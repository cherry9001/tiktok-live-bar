using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class ClubStrobeLight : MonoBehaviour
    {
        private Light strobe;
        private float phase;

        public void Initialize(Light target, float offset)
        {
            strobe = target;
            phase = offset;
        }

        private void Update()
        {
            if (strobe == null) return;
            float beat = ClubBeatClock.Beat;
            int beatIndex = Mathf.FloorToInt(beat);
            float beatPhase = beat - beatIndex;
            bool dropWindow = beatIndex % 32 >= 28;
            bool dropFlash = Mathf.Repeat(Time.time * 11f + phase, 1f) < 0.2f;
            bool barAccent = beatIndex % 4 == 0 && beatPhase < 0.12f;
            strobe.intensity = dropWindow && dropFlash ? 34f : barAccent ? 18f : 0f;
        }
    }
}
