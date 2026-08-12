using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TikTokLiveGame
{
    public sealed class AvatarService : MonoBehaviour
    {
        private readonly Dictionary<string, Sprite> cache = new();
        private readonly Dictionary<string, List<Action<Sprite>>> waiting = new();
        private readonly Queue<string> queue = new();
        private int activeDownloads;
        private const int MaximumConcurrent = 4;
        public static AvatarService Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Load(string url, Action<Sprite> callback)
        {
            if (string.IsNullOrWhiteSpace(url) || (!url.StartsWith("https://") && !url.StartsWith("http://"))) return;
            if (cache.TryGetValue(url, out Sprite sprite))
            {
                callback?.Invoke(sprite);
                return;
            }
            if (waiting.TryGetValue(url, out List<Action<Sprite>> callbacks))
            {
                callbacks.Add(callback);
                return;
            }
            waiting[url] = new List<Action<Sprite>> { callback };
            queue.Enqueue(url);
            Pump();
        }

        private void Pump()
        {
            while (activeDownloads < MaximumConcurrent && queue.Count > 0)
            {
                activeDownloads++;
                StartCoroutine(Download(queue.Dequeue()));
            }
        }

        private IEnumerator Download(string url)
        {
            Sprite result = null;
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url, true);
            request.timeout = 10;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                int side = Mathf.Min(texture.width, texture.height);
                Rect rect = new((texture.width - side) * 0.5f, (texture.height - side) * 0.5f, side, side);
                result = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), side);
                cache[url] = result;
            }
            if (waiting.Remove(url, out List<Action<Sprite>> callbacks))
                foreach (Action<Sprite> callback in callbacks) callback?.Invoke(result);
            activeDownloads--;
            Pump();
        }
    }
}
