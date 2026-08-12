using UnityEngine;

namespace TikTokLiveGame
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartGame()
        {
            if (Object.FindFirstObjectByType<TikTokGameController>() != null) return;
            // TikTok LIVE Studio captures at 60 FPS. Disable display-rate VSync so
            // high-refresh monitors cannot force uneven 60 FPS capture cadence.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Screen.SetResolution(540, 960, false);
            ClubSceneBuilder.Build();

            GameObject root = new("TikTok Live Game");
            AvatarService avatars = root.AddComponent<AvatarService>();
            TikTokWebSocketClient client = root.AddComponent<TikTokWebSocketClient>();
            PlayerManager players = new GameObject("Players").AddComponent<PlayerManager>();
            players.transform.SetParent(root.transform);
            GiftEffectManager giftEffects = root.AddComponent<GiftEffectManager>();
            root.AddComponent<MusicPlaylistPlayer>();
            TikTokGameController game = root.AddComponent<TikTokGameController>();
            game.Initialize(client, players, giftEffects);
            VisualCaptureHarness.InstallIfRequested(root);
        }
    }
}
