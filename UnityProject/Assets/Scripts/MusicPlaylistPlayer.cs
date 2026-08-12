using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace TikTokLiveGame
{
    public sealed class MusicPlaylistPlayer : MonoBehaviour
    {
        private const float DefaultVolume = 0.35f;
        private const float FadeSeconds = 1.2f;
        private AudioSource output;
        private AudioClip currentClip;
        private string[] tracks = Array.Empty<string>();
        private int trackIndex;
        private float configuredVolume = DefaultVolume;
        private float trackStartedAt;
        private float watchdogAt;
        private float advanceAt;
        private bool loading;
        private bool advancePending;
        private bool wasPlaying;

        private void Awake()
        {
            tracks = FindTracks(out string selectedFolder);
            if (tracks.Length == 0)
            {
                Debug.Log("Music player is ready. Add MP3, WAV or OGG files to the DJ_MUSIC folder.");
                enabled = false;
                return;
            }

            configuredVolume = ReadVolume(selectedFolder);
            output = gameObject.AddComponent<AudioSource>();
            output.playOnAwake = false;
            output.loop = false;
            output.spatialBlend = 0f;
            output.volume = 0f;
            output.priority = 128;
            PlayTrack(0);
        }

        private void Update()
        {
            if (output == null) return;
            if (advancePending && Time.unscaledTime >= advanceAt)
            {
                advancePending = false;
                PlayTrack((trackIndex + 1) % tracks.Length);
                return;
            }

            if (output.isPlaying)
            {
                wasPlaying = true;
                float fadeIn = Mathf.Clamp01((Time.unscaledTime - trackStartedAt) / FadeSeconds);
                float fadeOut = currentClip == null
                    ? 1f
                    : Mathf.Clamp01((currentClip.length - output.time) / FadeSeconds);
                output.volume = configuredVolume * Mathf.Min(fadeIn, fadeOut);
                watchdogAt = Time.unscaledTime + 3f;
            }
            else if (wasPlaying && !loading)
            {
                wasPlaying = false;
                output.volume = 0f;
                ScheduleAdvance(0.05f);
            }
            else if (!loading && !advancePending && Time.unscaledTime >= watchdogAt)
            {
                Debug.LogWarning("Music watchdog detected a stopped track; advancing the playlist.");
                ScheduleAdvance(0.5f);
            }
        }

        private void PlayTrack(int index)
        {
            if (tracks.Length == 0) return;
            trackIndex = Mathf.Clamp(index, 0, tracks.Length - 1);
            StopCurrentClip();
            loading = true;
            advancePending = false;
            watchdogAt = Time.unscaledTime + 20f;
            StartCoroutine(LoadAndPlay(tracks[trackIndex]));
        }

        private IEnumerator LoadAndPlay(string path)
        {
            AudioType type = AudioTypeFor(path);
            string uri = new Uri(path).AbsoluteUri;
            Debug.Log($"Preparing music {trackIndex + 1}/{tracks.Length}: {path}");
            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, type);
            if (request.downloadHandler is DownloadHandlerAudioClip audioDownload)
                audioDownload.streamAudio = true;
            yield return request.SendWebRequest();

            loading = false;
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Music could not play '{path}': {request.error}");
                ScheduleAdvance(tracks.Length == 1 ? 2f : 0.75f);
                yield break;
            }

            currentClip = DownloadHandlerAudioClip.GetContent(request);
            if (currentClip == null)
            {
                Debug.LogWarning($"Music decoder returned no audio for '{path}'.");
                ScheduleAdvance(tracks.Length == 1 ? 2f : 0.75f);
                yield break;
            }

            currentClip.name = $"Music {Path.GetFileName(path)}";
            output.clip = currentClip;
            output.volume = 0f;
            trackStartedAt = Time.unscaledTime;
            watchdogAt = Time.unscaledTime + 5f;
            output.Play();
            Debug.Log($"Music playing {trackIndex + 1}/{tracks.Length}: {path}");
        }

        private void StopCurrentClip()
        {
            if (output != null)
            {
                output.Stop();
                output.clip = null;
                output.volume = 0f;
            }
            if (currentClip != null) Destroy(currentClip);
            currentClip = null;
            wasPlaying = false;
        }

        private void ScheduleAdvance(float delay)
        {
            if (advancePending) return;
            advancePending = true;
            advanceAt = Time.unscaledTime + Mathf.Max(0.05f, delay);
        }

        private static AudioType AudioTypeFor(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                _ => AudioType.MPEG
            };
        }

        private static string[] FindTracks(out string selectedFolder)
        {
            string buildRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string projectRoot = Directory.GetParent(buildRoot)?.FullName ?? buildRoot;
            string workspaceRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            string[] folders =
            {
                Path.Combine(buildRoot, "DJ_MUSIC"),
                Path.Combine(workspaceRoot, "DJ_MUSIC"),
                Path.Combine(workspaceRoot, "assets", "music"),
                Application.streamingAssetsPath
            };
            string[] extensions = { ".mp3", ".wav", ".ogg" };
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                string[] files = Directory.EnumerateFiles(folder)
                    .Where(candidate => extensions.Contains(Path.GetExtension(candidate), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(candidate => Path.GetFileName(candidate), StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (files.Length == 0) continue;
                selectedFolder = folder;
                return files;
            }
            selectedFolder = string.Empty;
            return Array.Empty<string>();
        }

        private static float ReadVolume(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return DefaultVolume;
            string path = Path.Combine(folder, "VOLUME.txt");
            if (!File.Exists(path)) return DefaultVolume;
            string value = File.ReadAllText(path).Trim().Replace(',', '.');
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return DefaultVolume;
            if (parsed > 1f) parsed /= 100f;
            return Mathf.Clamp01(parsed);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            StopCurrentClip();
        }
    }
}
