using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class ClubLaserBeam : MonoBehaviour
    {
        private LineRenderer line;
        private Vector3 origin;
        private float phase;
        private float baseStartWidth;
        private float baseEndWidth;

        public void Initialize(LineRenderer targetLine, Vector3 beamOrigin, float beamPhase)
        {
            line = targetLine;
            origin = beamOrigin;
            phase = beamPhase;
            baseStartWidth = line.startWidth;
            baseEndWidth = line.endWidth;
            UpdateBeam();
        }

        private void Update() => UpdateBeam();

        private void UpdateBeam()
        {
            if (line == null) return;
            float time = Time.time;
            float beat = ClubBeatClock.Beat;
            float pulse = 0.68f + Mathf.Exp(-(beat - Mathf.Floor(beat)) * 7f) * 0.72f;
            Vector3 target = new(
                Mathf.Sin(time * 1.15f + phase) * 6.7f,
                0.12f + Mathf.Abs(Mathf.Sin(time * 0.61f + phase * 0.7f)) * 2.2f,
                Mathf.Lerp(-3.8f, 5.2f, Mathf.Sin(time * 0.75f + phase * 1.31f) * 0.5f + 0.5f)
            );
            line.startWidth = baseStartWidth * pulse;
            line.endWidth = baseEndWidth * pulse;
            line.SetPosition(0, origin);
            line.SetPosition(1, target);
        }
    }
}
