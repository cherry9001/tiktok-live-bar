using System.Collections.Generic;
using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class ClubPulseController : MonoBehaviour
    {
        [SerializeField] private float bpm = 128f;
        private readonly List<LightState> lights = new();
        private readonly List<MaterialState> materials = new();
        private Transform discoBall;

        private void Start()
        {
            bpm = ClubBeatClock.Bpm;
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.name.StartsWith("Moving Head") || light.name.StartsWith("Strobe Light")) continue;
                lights.Add(new LightState(light, light.intensity));
            }
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!renderer.name.StartsWith("LED") && !renderer.name.StartsWith("Floor Strip")) continue;
                Material material = renderer.material;
                if (material.HasProperty("_EmissionColor"))
                    materials.Add(new MaterialState(material, material.GetColor("_EmissionColor"), materials.Count * 0.47f));
            }
            GameObject disco = GameObject.Find("Disco Ball");
            if (disco != null) discoBall = disco.transform;
        }

        private void Update()
        {
            float beat = Time.time * bpm / 60f;
            float phase = beat - Mathf.Floor(beat);
            float pulse = Mathf.Exp(-phase * 7f);
            foreach (LightState state in lights)
                if (state.Light != null) state.Light.intensity = state.BaseIntensity * (0.72f + pulse * 0.55f);
            foreach (MaterialState state in materials)
            {
                float chase = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(beat * Mathf.PI * 2f - state.Phase)), 8f);
                float wave = 0.42f + Mathf.Sin(Time.time * 1.7f + state.Phase) * 0.15f + pulse * 0.28f + chase * 1.1f;
                state.Material.SetColor("_EmissionColor", state.BaseEmission * wave);
            }
            if (discoBall != null) discoBall.Rotate(0f, 38f * Time.deltaTime, 0f, Space.World);
        }

        private readonly struct LightState
        {
            public readonly Light Light;
            public readonly float BaseIntensity;
            public LightState(Light light, float intensity) { Light = light; BaseIntensity = intensity; }
        }

        private readonly struct MaterialState
        {
            public readonly Material Material;
            public readonly Color BaseEmission;
            public readonly float Phase;
            public MaterialState(Material material, Color emission, float phase) { Material = material; BaseEmission = emission; Phase = phase; }
        }
    }
}
