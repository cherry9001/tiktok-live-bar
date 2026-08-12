using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TikTokLiveBar
{
    public sealed class GifPlayer : MonoBehaviour
    {
        public string FrameDirectory;
        public float FrameRate = 24f;

        private Texture2D[] frames;
        private Renderer targetRenderer;
        private bool isReady = false;
        private int currentFrame = 0;
        private float frameTimer = 0f;

        private void Start()
        {
            targetRenderer = GetComponent<Renderer>();
            
            // Cài đặt material hỗ trợ trong suốt (Transparent Unlit)
            Shader transparentShader = Shader.Find("Unlit/Transparent");
            if (transparentShader == null) transparentShader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(transparentShader);
            targetRenderer.sharedMaterial = mat;

            StartCoroutine(LoadFramesAsync());
        }

        private IEnumerator LoadFramesAsync()
        {
            if (!Directory.Exists(FrameDirectory))
            {
                Debug.LogWarning($"GifPlayer: Directory not found - {FrameDirectory}");
                yield break;
            }

            string[] files = Directory.GetFiles(FrameDirectory, "*.png");
            Array.Sort(files); // Ensure correct order

            if (files.Length == 0)
            {
                Debug.LogWarning("GifPlayer: No PNG frames found in " + FrameDirectory);
                yield break;
            }

            frames = new Texture2D[files.Length];
            
            for (int i = 0; i < files.Length; i++)
            {
                byte[] fileData = File.ReadAllBytes(files[i]);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.LoadImage(fileData);
                tex.Apply(false, true); // Update CPU side, make it non-readable to save memory
                frames[i] = tex;

                // Yield to prevent freezing the main thread if there are many frames
                if (i % 20 == 0) yield return null;
            }

            isReady = true;
            Debug.Log($"GifPlayer: Loaded {frames.Length} frames successfully!");
        }

        private void Update()
        {
            if (!isReady || frames == null || frames.Length == 0) return;

            frameTimer += Time.deltaTime;
            float timePerFrame = 1f / FrameRate;

            while (frameTimer >= timePerFrame)
            {
                frameTimer -= timePerFrame;
                currentFrame = (currentFrame + 1) % frames.Length;
                
                targetRenderer.sharedMaterial.mainTexture = frames[currentFrame];
            }
        }
        
        private void OnDestroy()
        {
            if (frames != null)
            {
                foreach (var tex in frames)
                {
                    if (tex != null) Destroy(tex);
                }
            }
        }
    }
}
