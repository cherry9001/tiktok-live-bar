Shader "Custom/NeonGrid"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.02, 0.02, 0.04, 1)
        _LineColor ("Line Color", Color) = (0, 0.8, 1, 1) // Cyan
        _GridSize ("Grid Size", Float) = 2.0
        _LineThickness ("Line Thickness", Float) = 0.03
        _GlowIntensity ("Glow Intensity", Float) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

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
                float3 worldPos : TEXCOORD1;
            };

            float4 _BaseColor;
            float4 _LineColor;
            float _GridSize;
            float _LineThickness;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use world position (x and z) to draw the grid on the floor
                float2 gridUv = i.worldPos.xz * _GridSize;
                
                // Calculate distance to nearest grid line
                float2 distToLine = frac(gridUv);
                distToLine = min(distToLine, 1.0 - distToLine);
                
                // Anti-aliased line drawing using fwidth
                float2 fw = fwidth(gridUv);
                float lineMaskX = smoothstep(_LineThickness + fw.x, _LineThickness - fw.x, distToLine.x);
                float lineMaskY = smoothstep(_LineThickness + fw.y, _LineThickness - fw.y, distToLine.y);
                float lineMask = max(lineMaskX, lineMaskY);
                
                // Add scrolling light pulses along the grid
                float pulseX = sin(i.worldPos.x * 2.0 - _Time.y * 3.0) * 0.5 + 0.5;
                float pulseY = sin(i.worldPos.z * 2.0 - _Time.y * 2.5) * 0.5 + 0.5;
                float pulse = max(pulseX, pulseY) * 0.5 + 0.5;
                
                // Combine pulse with distance fade
                float fade = smoothstep(45.0, 15.0, length(i.worldPos.xz));

                float3 finalColor = lerp(_BaseColor.rgb, _LineColor.rgb * _GlowIntensity * pulse, lineMask * fade);
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
