Shader "Custom/CyberCylinderGlowUpTransparent"
{
    Properties
    {
        _NeonColor ("Neon Color", Color) = (0, 1, 1, 1)
        _GlowStrength ("Glow Strength", Range(0, 100)) = 25

        // Fill from bottom to top
        _Fill ("Fill (0..1)", Range(0, 1)) = 0
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.05

        // Moving scan band
        _ScanWidth ("Scan Width", Range(0.001, 0.5)) = 0.12
        _ScanStrength ("Scan Strength", Range(0, 10)) = 3
        _ScanSpeed ("Scan Speed", Range(0, 5)) = 0.6

        // Rim glow
        _RimPower ("Rim Power", Range(0.5, 10)) = 3.5
        _RimStrength ("Rim Strength", Range(0, 5)) = 1.0

        // Transparency gradient: 1 at bottom -> 0 at top
        _AlphaPower ("Alpha Power", Range(0.5, 8)) = 1.5
        _AlphaMin ("Alpha Min (top clamp)", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            // Alpha blending so the top can fade to fully transparent
           Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _NeonColor;
                float _GlowStrength;

                float _Fill;
                float _EdgeSoftness;

                float _ScanWidth;
                float _ScanStrength;
                float _ScanSpeed;

                float _RimPower;
                float _RimStrength;

                float _AlphaPower;
                float _AlphaMin;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float  height01    : TEXCOORD2;
            };

            // A soft band around a moving center (0..1)
            float band(float x, float center, float width)
            {
                float d = abs(x - center);
                float t = saturate(1.0 - d / max(width, 1e-5));
                // Sharpen slightly for a nicer scan look
                return t * t;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // Unity cylinder is typically y in [-0.5, 0.5] in object space.
                float y = IN.positionOS.y;
                float height01 = saturate(y + 0.5); // maps [-0.5..0.5] -> [0..1]

                float3 posOS = IN.positionOS.xyz; // NO cone deformation (pure cylinder)

                float3 posWS = TransformObjectToWorld(posOS);
                OUT.positionHCS = TransformWorldToHClip(posWS);

                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posWS);

                OUT.height01 = height01;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // Rim glow (Fresnel-ish)
                float rim = pow(saturate(1.0 - dot(N, V)), _RimPower) * _RimStrength;

                // Fill mask: 1 below Fill, 0 above (with soft edge)
                float a = _Fill - _EdgeSoftness;
                float b = _Fill + _EdgeSoftness;
                float fillMask = 1.0 - smoothstep(a, b, IN.height01);

                // Animated scan band that moves up (loops)
                float scanCenter = frac(_Time.y * _ScanSpeed);
                float scan = band(IN.height01, scanCenter, _ScanWidth) * _ScanStrength;

                // Total intensity (emission-like)
                float intensity = (fillMask + scan + rim) * _GlowStrength;

                // Alpha gradient: bottom opaque -> top transparent
                float alphaGrad = pow(saturate(1.0 - IN.height01), _AlphaPower);
                alphaGrad = max(alphaGrad, _AlphaMin);

                // You can optionally make alpha depend on fill so nothing shows above the fill:
                // alphaGrad *= saturate(fillMask + scan);

                float3 col = _NeonColor.rgb * intensity;

                // Premultiply to avoid harsh edges with alpha blending
                col *= alphaGrad;

                return half4(col, alphaGrad);
            }
            ENDHLSL
        }
    }
}