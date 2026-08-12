Shader "Custom/ProceduralSmoke"
{
    Properties
    {
        _Color1 ("Color 1", Color) = (1, 0, 0.4, 1) // Magenta/Red
        _Color2 ("Color 2", Color) = (0, 0.8, 1, 1) // Cyan/Blue
        _Speed ("Speed", Float) = 0.5
        _Scale ("Scale", Float) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100
        ZWrite Off
        Cull Off

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color1;
            float4 _Color2;
            float _Speed;
            float _Scale;

            // Simple 2D noise function
            float hash(float2 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * (p.x + p.y));
            }

            float noise(float2 x)
            {
                float2 i = floor(x);
                float2 f = frac(x);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i + float2(0.0, 0.0)), 
                                 hash(i + float2(1.0, 0.0)), u.x),
                            lerp(hash(i + float2(0.0, 1.0)), 
                                 hash(i + float2(1.0, 1.0)), u.x), u.y);
            }

            // Fractal Brownian Motion
            float fbm(float2 x)
            {
                float v = 0.0;
                float a = 0.5;
                float2 shift = float2(100.0, 100.0);
                float2x2 rot = float2x2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));
                for (int i = 0; i < 5; ++i)
                {
                    v += a * noise(x);
                    x = mul(rot, x) * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * _Scale;
                float time = _Time.y * _Speed;

                // Create warping effect by feeding fbm into fbm
                float2 q = float2(fbm(uv + float2(0.0, time * 0.1)),
                                  fbm(uv + float2(5.2, 1.3)));

                float2 r = float2(fbm(uv + 4.0 * q + float2(1.7, 9.2) + time * 0.15),
                                  fbm(uv + 4.0 * q + float2(8.3, 2.8) + time * 0.126));

                float f = fbm(uv + 4.0 * r);

                // Mix colors based on the noise value
                float3 col = lerp(_Color1.rgb, _Color2.rgb, clamp(f * f * 4.0, 0.0, 1.0));
                
                // Add some brightness/contrast
                col = col * col * (3.0 - 2.0 * col);
                
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
