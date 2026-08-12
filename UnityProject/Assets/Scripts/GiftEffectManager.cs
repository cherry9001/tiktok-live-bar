using System.Collections;
using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class GiftEffectManager : MonoBehaviour
    {
        private readonly System.Collections.Generic.Dictionary<string, ComboState> combos = new();
        private AudioSource fireworkAudio;
        private AudioClip fireworkClip;
        public string BannerText { get; private set; } = string.Empty;
        public float BannerUntil { get; private set; }

        private void Awake()
        {
            fireworkAudio = gameObject.AddComponent<AudioSource>();
            fireworkAudio.playOnAwake = false;
            fireworkAudio.loop = false;
            fireworkAudio.spatialBlend = 0.08f;
            fireworkAudio.volume = 0.72f;
            fireworkAudio.priority = 80;
            fireworkClip = Resources.Load<AudioClip>("Audio/dragon-studio-fireworks-07-419025");
            fireworkAudio.clip = fireworkClip;
        }

        public void Play(TikTokEvent data, PlayerActor actor)
        {
            if (data.type != "gift") return;
            string gift = string.IsNullOrWhiteSpace(data.giftName) ? "Gift" : data.giftName;
            combos.TryGetValue(data.userId ?? string.Empty, out ComboState combo);
            int count = Time.unscaledTime - combo.LastTime <= 5f ? combo.Count + 1 : 1;
            combos[data.userId ?? string.Empty] = new ComboState(count, Time.unscaledTime);
            string comboText = count > 1 ? $"  (COMBO x{count})" : string.Empty;
            BannerText = $"Chủ tịch: {data.nickname} đã tặng {gift} cho anh em bar!{comboText}";
            BannerUntil = Time.unscaledTime + Mathf.Clamp(data.durationMs / 1000f, 3f, 10f);

            bool spotlightAction = data.action is "camera" or "vip" or "topdj" or "fireworks" or "medal";
            if (actor != null && spotlightAction && data.diamondCount >= 20) StartCoroutine(Spotlight(actor, data));
            if (data.action == "fireworks" || data.action == "topdj" || data.diamondCount >= 500)
                LaunchFireworks(Mathf.Clamp(Mathf.Max(2, data.fireworkBursts), 4, 7));
        }

        public void PartyBurst()
        {
            BannerText = "PARTY ENERGY MAX — CẢ SÀN CÙNG QUẨY";
            BannerUntil = Time.unscaledTime + 6f;
            LaunchFireworks(5);
        }

        private IEnumerator Spotlight(PlayerActor actor, TikTokEvent data)
        {
            GameObject lightObject = new($"Gift Spotlight — {actor.Nickname}");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = data.diamondCount >= 100 ? new Color(1f, 0.22f, 0.68f) : new Color(0.2f, 0.9f, 1f);
            light.intensity = data.diamondCount >= 100 ? 35f : 22f;
            light.range = 14f;
            light.spotAngle = 38f;

            float duration = Mathf.Clamp(data.durationMs / 1000f, 3f, 12f);
            float end = Time.time + duration;
            while (actor != null && Time.time < end)
            {
                lightObject.transform.position = actor.transform.position + new Vector3(0f, 7f, 2f);
                lightObject.transform.LookAt(actor.transform.position + Vector3.up);
                yield return null;
            }
            Destroy(lightObject);
        }

        private void LaunchFireworks(int burstCount)
        {
            PlayFireworkSound();
            StartCoroutine(FireworkSequence(burstCount));
            StartCoroutine(ColdFountainSequence());
        }

        private IEnumerator FireworkSequence(int waveCount)
        {
            for (int wave = 0; wave < waveCount; wave++)
            {
                int burstInWave = wave % 3 == 0 ? 2 : 1;
                for (int index = 0; index < burstInWave; index++)
                {
                    Vector3 position = new(Random.Range(-6.5f, 6.5f), Random.Range(4f, 8.2f), Random.Range(-7f, -0.5f));
                    CreateBurst(position, Color.HSVToRGB(Random.value, 0.78f, 1f));
                }
                yield return new WaitForSeconds(0.72f);
            }
        }

        private IEnumerator ColdFountainSequence()
        {
            float[] xPositions = { -5.35f, -3.45f, -1.55f, 1.55f, 3.45f, 5.35f };
            int[,] pairs = { { 0, 5 }, { 1, 4 }, { 2, 3 } };
            for (int pair = 0; pair < 3; pair++)
            {
                float duration = 5.35f - pair * 0.32f;
                CreateColdFountain(new Vector3(xPositions[pairs[pair, 0]], 0.08f, -4.75f), duration, false);
                CreateColdFountain(new Vector3(xPositions[pairs[pair, 1]], 0.08f, -4.75f), duration, false);
                yield return new WaitForSeconds(0.32f);
            }

            yield return new WaitForSeconds(2.15f);
            foreach (float x in xPositions)
                CreateColdFountain(new Vector3(x, 0.08f, -4.75f), 2.35f, true);
        }

        private void CreateColdFountain(Vector3 position, float duration, bool finale)
        {
            GameObject fountain = new(finale ? "Cold Spark Finale" : "Cold Spark Fountain");
            fountain.transform.position = position;
            fountain.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem particles = fountain.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.62f, 1.02f);
            main.startSpeed = finale
                ? new ParticleSystem.MinMaxCurve(6.2f, 9.1f)
                : new ParticleSystem.MinMaxCurve(4.8f, 7.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.043f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.94f, 0.66f, 1f),
                Color.white);
            main.gravityModifier = 0.38f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = finale ? 108f : 72f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = finale ? 8.5f : 6.2f;
            shape.radius = 0.055f;

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = finale ? 0.42f : 0.28f;
            noise.frequency = 1.35f;
            noise.scrollSpeed = 0.7f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.78f, 0.28f), 0.62f),
                    new GradientColorKey(new Color(1f, 0.38f, 0.08f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.06f),
                    new GradientAlphaKey(0.92f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GameMaterials.Sprite();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.085f;
            renderer.lengthScale = 1.35f;
            renderer.maxParticleSize = 0.055f;

            particles.Play();
            Destroy(fountain, duration + 1.5f);
        }

        private void CreateBurst(Vector3 position, Color color)
        {
            GameObject burst = new("Firework Burst");
            burst.transform.position = position;
            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.48f, 0.86f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.016f, 0.042f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = 0.28f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            ParticleSystem.LimitVelocityOverLifetimeModule limit = particles.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 0.22f;

            ParticleSystem.TrailModule trails = particles.trails;
            trails.enabled = true;
            trails.ratio = 0.06f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GameMaterials.Sprite();
            renderer.trailMaterial = GameMaterials.Sprite();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.035f;

            particles.Play();
            particles.Emit(new ParticleSystem.EmitParams(), 28);
            Destroy(burst, 1.8f);
        }

        private void PlayFireworkSound()
        {
            if (fireworkAudio == null || fireworkClip == null) return;
            fireworkAudio.Stop();
            fireworkAudio.Play();
        }

        private readonly struct ComboState
        {
            public readonly int Count;
            public readonly float LastTime;
            public ComboState(int count, float lastTime) { Count = count; LastTime = lastTime; }
        }
    }
}
