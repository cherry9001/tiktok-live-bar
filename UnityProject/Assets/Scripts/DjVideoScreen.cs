using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace TikTokLiveGame
{
    [RequireComponent(typeof(Renderer))]
    public sealed class DjVideoScreen : MonoBehaviour
    {
        private VideoPlayer player;
        private RenderTexture videoSourceTexture;
        private RenderTexture videoTexture;
        private Material screenMaterial;
        private Texture2D imageTexture;
        private string[] imagePaths = Array.Empty<string>();
        private int imageIndex;
        private float nextSlideAt;
        private const float SlideSeconds = 8f;

        private void Start()
        {
            string path = FindVideoPath();
            if (!string.IsNullOrEmpty(path))
            {
                StartVideo(path);
                return;
            }

            imagePaths = FindImagePaths();
            if (imagePaths.Length > 0)
            {
                ShowImage(0);
                return;
            }

            Debug.Log("DJ screen is ready. Add a video or PNG/JPG image to the DJ_VIDEO folder.");
        }

        private void Update()
        {
            if (videoSourceTexture != null && videoTexture != null)
            {
                Graphics.Blit(
                    videoSourceTexture,
                    videoTexture,
                    new Vector2(1f, -1f),
                    new Vector2(0f, 1f));
            }
            if (imagePaths.Length <= 1 || Time.unscaledTime < nextSlideAt) return;
            ShowImage((imageIndex + 1) % imagePaths.Length);
        }

        private void StartVideo(string path)
        {
            videoSourceTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
            {
                name = "DJ Video Source Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            videoSourceTexture.Create();
            videoTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
            {
                name = "DJ Video Render Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            videoTexture.Create();

            Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            screenMaterial = new Material(shader) { mainTexture = videoTexture };
            GetComponent<Renderer>().material = screenMaterial;

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.source = VideoSource.Url;
            player.url = new Uri(path).AbsoluteUri;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = videoSourceTexture;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.skipOnDrop = true;
            player.errorReceived += (_, message) => Debug.LogWarning($"DJ video could not play: {message}");
            player.prepareCompleted += source => source.Play();
            player.Prepare();
            Debug.Log($"DJ video loaded: {path}");
        }

        private void ShowImage(int index)
        {
            if (index < 0 || index >= imagePaths.Length) return;
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(imagePaths[index]);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"DJ image could not be read: {exception.Message}");
                return;
            }

            Texture2D nextTexture = new(2, 2, TextureFormat.RGBA32, false)
            {
                name = $"DJ Image {Path.GetFileName(imagePaths[index])}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!nextTexture.LoadImage(bytes, false))
            {
                Destroy(nextTexture);
                Debug.LogWarning($"DJ image format is not supported: {imagePaths[index]}");
                return;
            }
            FlipPixelsVertically(nextTexture);

            if (screenMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
                screenMaterial = new Material(shader);
                GetComponent<Renderer>().material = screenMaterial;
            }
            Texture2D previous = imageTexture;
            imageTexture = nextTexture;
            screenMaterial.mainTexture = imageTexture;
            FitImageToScreen(imageTexture);
            if (previous != null) Destroy(previous);
            imageIndex = index;
            nextSlideAt = Time.unscaledTime + SlideSeconds;
            Debug.Log($"DJ image loaded: {imagePaths[index]}");
        }

        private void FitImageToScreen(Texture texture)
        {
            if (screenMaterial == null || texture == null) return;
            Vector3 screenSize = GetComponent<Renderer>().bounds.size;
            float screenAspect = screenSize.y > 0.001f ? screenSize.x / screenSize.y : 16f / 9f;
            float textureAspect = texture.height > 0 ? texture.width / (float)texture.height : screenAspect;
            Vector2 scale = Vector2.one;
            Vector2 offset = Vector2.zero;
            if (textureAspect > screenAspect)
            {
                scale.x = screenAspect / textureAspect;
                offset.x = (1f - scale.x) * 0.5f;
            }
            else if (textureAspect < screenAspect)
            {
                scale.y = textureAspect / screenAspect;
                offset.y = (1f - scale.y) * 0.5f;
            }
            screenMaterial.mainTextureScale = scale;
            screenMaterial.mainTextureOffset = offset;
        }

        private static void FlipPixelsVertically(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            Color32[] source = texture.GetPixels32();
            Color32[] flipped = new Color32[source.Length];
            for (int row = 0; row < height; row++)
                Array.Copy(source, row * width, flipped, (height - 1 - row) * width, width);
            texture.SetPixels32(flipped);
            texture.Apply(false, false);
        }

        private static string FindVideoPath()
        {
            string buildRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string projectRoot = Directory.GetParent(buildRoot)?.FullName ?? buildRoot;
            string workspaceRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            string[] folders =
            {
                Path.Combine(buildRoot, "DJ_VIDEO"),
                Path.Combine(workspaceRoot, "DJ_VIDEO"),
                Application.streamingAssetsPath
            };
            string[] extensions = { ".mp4", ".mov", ".m4v", ".webm" };
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                string file = Directory.EnumerateFiles(folder)
                    .FirstOrDefault(candidate => extensions.Contains(Path.GetExtension(candidate), StringComparer.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(file)) return file;
            }
            return null;
        }

        private static string[] FindImagePaths()
        {
            string buildRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string projectRoot = Directory.GetParent(buildRoot)?.FullName ?? buildRoot;
            string workspaceRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            string[] folders =
            {
                Path.Combine(buildRoot, "DJ_VIDEO"),
                Path.Combine(workspaceRoot, "DJ_VIDEO"),
                Application.streamingAssetsPath
            };
            string[] extensions = { ".png", ".jpg", ".jpeg" };
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                string[] files = Directory.EnumerateFiles(folder)
                    .Where(candidate => extensions.Contains(Path.GetExtension(candidate), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(candidate => Path.GetFileName(candidate), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (files.Length > 0) return files;
            }
            return Array.Empty<string>();
        }

        private void OnDestroy()
        {
            if (player != null) player.Stop();
            if (videoSourceTexture != null)
            {
                videoSourceTexture.Release();
                Destroy(videoSourceTexture);
            }
            if (videoTexture != null)
            {
                videoTexture.Release();
                Destroy(videoTexture);
            }
            if (imageTexture != null) Destroy(imageTexture);
            if (screenMaterial != null) Destroy(screenMaterial);
        }
    }
}
