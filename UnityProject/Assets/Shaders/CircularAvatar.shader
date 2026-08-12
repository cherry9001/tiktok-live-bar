Shader "TikTokLiveGame/CircularAvatar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Avatar", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [_ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 avatar = tex2D(_MainTex, i.uv);
                float radius = length(i.uv - float2(0.5, 0.5));
                float edgeWidth = max(fwidth(radius), 0.002);
                float outer = 1.0 - smoothstep(0.49 - edgeWidth, 0.49 + edgeWidth, radius);
                float imageArea = 1.0 - smoothstep(0.415 - edgeWidth, 0.435 + edgeWidth, radius);
                fixed3 color = lerp(fixed3(1.0, 1.0, 1.0), avatar.rgb, imageArea);
                float alpha = outer * lerp(1.0, avatar.a, imageArea) * i.color.a;
                return fixed4(color * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
