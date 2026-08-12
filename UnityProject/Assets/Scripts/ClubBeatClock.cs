using System.Globalization;
using System.IO;
using UnityEngine;

namespace TikTokLiveGame
{
    public static class ClubBeatClock
    {
        private static float bpm;
        private static bool loaded;

        public static float Bpm
        {
            get
            {
                Load();
                return bpm;
            }
        }

        public static float Beat => Time.time * Bpm / 60f;

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            bpm = 128f;
            string buildRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            string projectRoot = Directory.GetParent(buildRoot)?.FullName ?? buildRoot;
            string workspaceRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            string[] paths =
            {
                Path.Combine(buildRoot, "DJ_VIDEO", "BPM.txt"),
                Path.Combine(workspaceRoot, "DJ_VIDEO", "BPM.txt")
            };
            foreach (string path in paths)
            {
                if (!File.Exists(path)) continue;
                if (float.TryParse(File.ReadAllText(path).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float configured))
                    bpm = Mathf.Clamp(configured, 60f, 200f);
                break;
            }
            Debug.Log($"Club lighting BPM: {bpm:0.##}");
        }
    }
}
