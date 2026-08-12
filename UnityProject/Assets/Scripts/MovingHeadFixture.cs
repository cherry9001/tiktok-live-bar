using UnityEngine;

namespace TikTokLiveGame
{
    public enum MovingHeadStyle
    {
        Spot,
        Wash,
        Beam,
        GoboDots,
        GoboStar
    }

    public sealed class MovingHeadFixture : MonoBehaviour
    {
        private const int FloorLightingLayer = 8;
        private static Texture2D softSpotCookie;
        private static Texture2D dotGoboCookie;
        private static Texture2D starGoboCookie;

        private Vector3 origin;
        private Color baseColor;
        private float phase;
        private MovingHeadStyle style;
        private Light spot;
        private LineRenderer beam;
        private float baseStartWidth;
        private float baseEndWidth;

        public void Initialize(Vector3 fixtureOrigin, Color color, float fixturePhase, MovingHeadStyle fixtureStyle, bool castsLight)
        {
            origin = fixtureOrigin;
            baseColor = color;
            phase = fixturePhase;
            style = fixtureStyle;

            bool wash = style == MovingHeadStyle.Wash;
            bool beamStyle = style == MovingHeadStyle.Beam;
            bool gobo = style == MovingHeadStyle.GoboDots || style == MovingHeadStyle.GoboStar;

            spot = gameObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = wash ? 19f : 24f;
            spot.spotAngle = wash ? 24f : beamStyle ? 7f : gobo ? 17f : 13f;
            spot.innerSpotAngle = spot.spotAngle * 0.62f;
            spot.intensity = wash ? 0.85f : beamStyle ? 3.4f : gobo ? 2.8f : 2.5f;
            spot.color = color;
            spot.cullingMask = -1; // Chiếu sáng mọi layer, bao gồm cả backdrop 2D
            spot.cookie = CookieForStyle(style);
            spot.renderMode = LightRenderMode.ForcePixel;
            spot.enabled = castsLight;

            beam = gameObject.AddComponent<LineRenderer>();
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.numCapVertices = 4;
            baseStartWidth = wash ? 0.28f : beamStyle ? 0.035f : gobo ? 0.06f : 0.075f;
            baseEndWidth = wash ? 0.92f : beamStyle ? 0.095f : gobo ? 0.22f : 0.24f;
            beam.startWidth = baseStartWidth;
            beam.endWidth = baseEndWidth;
            beam.sharedMaterial = GameMaterials.BackgroundSprite();
            beam.startColor = new Color(color.r, color.g, color.b, wash ? 0.045f : beamStyle ? 0.17f : gobo ? 0.1f : 0.12f);
            beam.endColor = new Color(color.r, color.g, color.b, 0.008f);
            beam.enabled = true;
        }

        private void Update()
        {
            if (spot == null || beam == null) return;
            float time = Time.time;
            float beat = ClubBeatClock.Beat;
            float beatPulse = Mathf.Exp(-(beat - Mathf.Floor(beat)) * 6.5f);
            bool wash = style == MovingHeadStyle.Wash;
            bool beamStyle = style == MovingHeadStyle.Beam;
            bool gobo = style == MovingHeadStyle.GoboDots || style == MovingHeadStyle.GoboStar;
            float speed = wash ? 0.29f : beamStyle ? 0.71f : gobo ? 0.41f : 0.58f;
            Vector3 target = new(
                Mathf.Sin(time * speed + phase) * (wash ? 4.4f : 5.2f),
                0.08f,
                Mathf.Lerp(-3.5f, 4.2f, Mathf.Sin(time * (speed * 0.81f) + phase * 1.23f) * 0.5f + 0.5f)
            );

            if (wash)
            {
                beam.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.055f);
                beam.endColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.008f);
            }
            spot.color = baseColor;
            float baseIntensity = wash ? 0.62f : beamStyle ? 2.7f : gobo ? 2.15f : 1.9f;
            float pulseIntensity = wash ? 0.42f : beamStyle ? 1.35f : gobo ? 1.05f : 0.9f;
            spot.intensity = baseIntensity + beatPulse * pulseIntensity;
            transform.position = origin;
            transform.LookAt(target);
            if (gobo) transform.Rotate(Vector3.forward, time * (style == MovingHeadStyle.GoboStar ? 21f : -15f) + phase * 18f, Space.Self);
            beam.startWidth = baseStartWidth * (0.82f + beatPulse * 0.45f);
            beam.endWidth = baseEndWidth * (0.82f + beatPulse * 0.45f);
            beam.SetPosition(0, origin);
            beam.SetPosition(1, target);
        }

        private static Texture2D CookieForStyle(MovingHeadStyle fixtureStyle)
        {
            return fixtureStyle switch
            {
                MovingHeadStyle.GoboDots => DotGoboCookie(),
                MovingHeadStyle.GoboStar => StarGoboCookie(),
                _ => SoftSpotCookie()
            };
        }

        private static Texture2D SoftSpotCookie()
        {
            if (softSpotCookie != null) return softSpotCookie;
            const int size = 64;
            softSpotCookie = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Soft Moving Head Cookie",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(u * u + v * v);
                float value = 1f - Mathf.SmoothStep(0.28f, 1f, radius);
                pixels[y * size + x] = new Color(value, value, value, value);
            }
            softSpotCookie.SetPixels(pixels);
            softSpotCookie.Apply(false, true);
            return softSpotCookie;
        }

        private static Texture2D DotGoboCookie()
        {
            if (dotGoboCookie != null) return dotGoboCookie;
            dotGoboCookie = CreateCookie("Seven Dot Gobo", (u, v) =>
            {
                float value = Disc(u, v, 0f, 0f, 0.19f);
                for (int index = 0; index < 6; index++)
                {
                    float angle = index * Mathf.PI / 3f;
                    value = Mathf.Max(value, Disc(u, v, Mathf.Cos(angle) * 0.55f, Mathf.Sin(angle) * 0.55f, 0.16f));
                }
                return value;
            });
            return dotGoboCookie;
        }

        private static Texture2D StarGoboCookie()
        {
            if (starGoboCookie != null) return starGoboCookie;
            starGoboCookie = CreateCookie("Rotating Star Gobo", (u, v) =>
            {
                float radius = Mathf.Sqrt(u * u + v * v);
                if (radius > 0.92f) return 0f;
                float angle = Mathf.Atan2(v, u);
                float starEdge = Mathf.Lerp(0.25f, 0.82f, Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 5f)), 0.7f));
                float star = 1f - Mathf.SmoothStep(starEdge - 0.08f, starEdge + 0.03f, radius);
                float ring = 1f - Mathf.SmoothStep(0.035f, 0.09f, Mathf.Abs(radius - 0.78f));
                return Mathf.Max(star, ring * 0.72f);
            });
            return starGoboCookie;
        }

        private static Texture2D CreateCookie(string name, System.Func<float, float, float> sampler)
        {
            const int size = 96;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float value = Mathf.Clamp01(sampler(u, v));
                pixels[y * size + x] = new Color(value, value, value, value);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float Disc(float u, float v, float centerX, float centerY, float radius)
        {
            float distance = Mathf.Sqrt((u - centerX) * (u - centerX) + (v - centerY) * (v - centerY));
            return 1f - Mathf.SmoothStep(radius * 0.72f, radius, distance);
        }
    }
}
