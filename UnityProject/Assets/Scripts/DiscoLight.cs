using UnityEngine;

namespace TikTokLiveGame
{
    /// <summary>
    /// Disco spotlight that sweeps the dance floor with colour-cycling beams.
    /// Created procedurally by ClubSceneBuilder — one instance per light fixture.
    /// </summary>
    public sealed class DiscoLight : MonoBehaviour
    {
        /// <summary>Index (0-3) used to offset the sweep phase so each light moves independently.</summary>
        [HideInInspector] public int index;

        private Light spot;
        private Color baseColor;
        private float phase;

        private static readonly Color[] palette =
        {
            new(1f, 0.15f, 0.35f),   // hot pink
            new(0.1f, 0.55f, 1f),     // electric blue
            new(0.18f, 1f, 0.45f),    // neon green
            new(0.92f, 0.3f, 1f),     // violet
            new(1f, 0.72f, 0.08f),    // amber
            new(0f, 0.95f, 0.92f),    // cyan
        };

        private void Awake()
        {
            spot = GetComponent<Light>();
            phase = index * Mathf.PI * 0.5f;
            baseColor = palette[index % palette.Length];
        }

        private void Update()
        {
            if (spot == null) return;

            float time = Time.time;
            float beat = ClubBeatClock.Beat;
            float beatPulse = Mathf.Exp(-(beat - Mathf.Floor(beat)) * 5.5f);

            // Colour cycling — slowly rotate through the palette
            int colourA = (int)(time * 0.18f + index) % palette.Length;
            int colourB = (colourA + 1) % palette.Length;
            float blend = Mathf.PingPong(time * 0.22f + phase, 1f);
            Color currentColour = Color.Lerp(palette[colourA], palette[colourB], blend);

            spot.color = currentColour;
            spot.intensity = 1.4f + beatPulse * 2.2f;

            // Sweep target across the dance floor
            float sweepX = Mathf.Sin(time * 0.45f + phase) * 5.5f;
            float sweepZ = Mathf.Cos(time * 0.32f + phase * 1.3f) * 3.5f;
            Vector3 target = new(sweepX, 0.1f, sweepZ);

            transform.LookAt(target);
        }
    }
}
