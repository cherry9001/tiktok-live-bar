Shader "TikTokLiveGame/ClubLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.35
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _DetailStrength ("Surface Detail", Range(0,0.2)) = 0
        _DetailScale ("Detail Scale", Range(0.1,30)) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _Color;
        half _Metallic;
        half _Glossiness;
        fixed4 _EmissionColor;
        half _DetailStrength;
        half _DetailScale;

        struct Input
        {
            float3 worldPos;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            float grain = sin(input.worldPos.x * _DetailScale) * sin(input.worldPos.z * (_DetailScale * 1.13));
            float detail = grain * _DetailStrength;
            output.Albedo = _Color.rgb * (1.0 + detail);
            output.Metallic = _Metallic;
            output.Smoothness = saturate(_Glossiness - detail * 0.4);
            output.Emission = _EmissionColor.rgb;
            output.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
