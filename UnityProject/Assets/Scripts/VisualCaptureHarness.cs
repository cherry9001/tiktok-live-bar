using System;
using System.Collections;
using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class VisualCaptureHarness : MonoBehaviour
    {
        public static void InstallIfRequested(GameObject root)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-capturePath");
            if (index < 0 || index + 1 >= arguments.Length) return;
            VisualCaptureHarness harness = root.AddComponent<VisualCaptureHarness>();
            harness.StartCoroutine(harness.Capture(arguments[index + 1]));
        }

        private IEnumerator Capture(string path)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            TikTokWebSocketClient client = FindFirstObjectByType<TikTokWebSocketClient>();
            client?.StartDemo(40);
            yield return new WaitForSecondsRealtime(5f);
            client?.StopDemo();
            client?.DemoGift(300, 7);
            client?.DemoGift(200, 8);
            client?.DemoGift(100, 9);
            yield return new WaitForSecondsRealtime(9f);
            FindFirstObjectByType<GiftEffectManager>()?.PartyBurst();
            yield return new WaitForSecondsRealtime(0.8f);
            ScreenCapture.CaptureScreenshot(path + ".stage.png");
            yield return new WaitForSecondsRealtime(0.5f);
            client?.DemoGift(100, 7);
            yield return new WaitForSecondsRealtime(4f);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"Visual capture saved to {path}");
            yield return new WaitForSecondsRealtime(2f);
            Application.Quit();
        }
    }
}
