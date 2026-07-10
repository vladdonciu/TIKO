Shader "Custom/CyberCollectibleGlow"
{
    Properties
    {
        _NeonColor ("Neon Color", Color) = (1, 0, 0.15, 1)
        _GlowStrength ("Glow Strength", Range(0, 100)) = 18

        _Fill ("Fill (0..1)", Range(0, 1)) = 1
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.05

        _ScanWidth ("Scan Width", Range(0.001, 0.5)) = 0.15
        _ScanStrength ("Scan Strength", Range(0, 10)) = 3
        _ScanSpeed ("Scan Speed", Range(0, 5)) = 1.0

        _RimPower ("Rim Power", Range(0.5, 10)) = 2.5
        _RimStrength ("Rim Strength", Range(0, 5)) = 2.2

        _AlphaPower ("Alpha Power", Range(0.5, 8)) = 1.5
        _AlphaMin ("Alpha Min (top clamp)", Range(0, 1)) = 0.15

        [Header(Hologram)]
        _HoloBaseAlpha ("Hologram Base Alpha", Range(0, 1)) = 0.35
        _HoloLineCount ("Hologram Line Count", Range(20, 400)) = 120
        _HoloLineSpeed ("Hologram Line Speed", Range(0, 5)) = 0.4
        _HoloLineStrength ("Hologram Line Strength", Range(0, 1)) = 0.2

        [Header(Glitch Effect)]
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.2
        _GlitchSpeed ("Glitch Speed", Range(0, 20)) = 5
        _GlitchBandCount ("Glitch Band Count", Range(2, 50)) = 10
        _GlitchColorShift ("Glitch Color Shift", Range(0, 1)) = 0.2
        [HDR] _GlitchColor ("Glitch Flash Color", Color) = (2.2, 0.35, 0.05, 1)

        [Header(Electric Arcs)]
        _BoltCount ("Bolt Count", Range(1, 6)) = 3
        _BoltFlickerSpeed ("Bolt Flicker Speed", Range(1, 20)) = 6
        _BoltThickness ("Bolt Thickness", Range(0.002, 0.08)) = 0.02
        _BoltJaggedness ("Bolt Jaggedness", Range(0, 20)) = 8
        _BoltStrength ("Bolt Strength", Range(0, 10)) = 4
        [HDR] _BoltColor ("Bolt Color", Color) = (2.5, 0.5, 0.1, 1)
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

                float _HoloBaseAlpha;
                float _HoloLineCount;
                float _HoloLineSpeed;
                float _HoloLineStrength;

                float _GlitchIntensity;
                float _GlitchSpeed;
                float _GlitchBandCount;
                float _GlitchColorShift;
                float4 _GlitchColor;

                float _BoltCount;
                float _BoltFlickerSpeed;
                float _BoltThickness;
                float _BoltJaggedness;
                float _BoltStrength;
                float4 _BoltColor;
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
                float3 posOS       : TEXCOORD3;
            };

            float band(float x, float center, float width)
            {
                float d = abs(x - center);
                float t = saturate(1.0 - d / max(width, 1e-5));
                return t * t;
            }

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float boltPathX(float y, float seed)
            {
                float path = 0.0;
                path += sin(y * _BoltJaggedness + seed * 6.283 + 0.0) * 0.4;
                path += sin(y * (_BoltJaggedness * 1.9) + seed * 6.283 + 2.1) * 0.2;
                path += sin(y * (_BoltJaggedness * 3.61) + seed * 6.283 + 4.2) * 0.1;
                return path * 0.15;
            }

            float lightningBolt(float2 uv, float boltIndex, float discreteTime)
            {
                float seed = hash11(boltIndex * 13.7 + discreteTime * 3.3);
                float visible = step(0.55, hash11(boltIndex * 7.1 + discreteTime));

                float centerX = 0.2 + boltIndex * 0.15 + (seed - 0.5) * 0.3;
                float pathX = centerX + boltPathX(uv.y, seed);

                float dist = abs(uv.x - pathX);
                float lineMask = smoothstep(_BoltThickness, 0.0, dist);

                return lineMask * visible;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                float y = IN.positionOS.y;
                float height01 = saturate(y + 0.5);

                float3 posOS = IN.positionOS.xyz;

                float3 posWS = TransformObjectToWorld(posOS);
                OUT.positionHCS = TransformWorldToHClip(posWS);

                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posWS);

                OUT.height01 = height01;
                OUT.posOS = posOS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                float glitchWave = sin(_Time.y * _GlitchSpeed) * 0.5 + 0.5;
                float glitchNoise = hash11(floor(_Time.y * _GlitchSpeed * 0.5));
                float glitchTrigger = smoothstep(0.6, 0.95, glitchNoise) * glitchWave;

                float bandID = floor(IN.height01 * _GlitchBandCount);
                float bandSeed = hash11(bandID + floor(_Time.y * _GlitchSpeed * 0.5) * 7.0);
                float horizontalShift = (bandSeed - 0.5) * _GlitchIntensity * glitchTrigger * 0.08;

                float shiftedHeight01 = saturate(IN.height01 + horizontalShift);

                float rim = pow(saturate(1.0 - dot(N, V)), _RimPower) * _RimStrength;

                float alphaEdgeA = _Fill - _EdgeSoftness;
                float alphaEdgeB = _Fill + _EdgeSoftness;
                float fillMask = 1.0 - smoothstep(alphaEdgeA, alphaEdgeB, shiftedHeight01);

                float scanCenter = frac(_Time.y * _ScanSpeed);
                float scan = band(shiftedHeight01, scanCenter, _ScanWidth) * _ScanStrength;

                float holoLines = sin((shiftedHeight01 - _Time.y * _HoloLineSpeed) * _HoloLineCount * 6.283);
                holoLines = (holoLines * 0.5 + 0.5) * _HoloLineStrength;

                float2 surfaceUV = float2(IN.posOS.x, IN.posOS.z * 0.5 + IN.posOS.y);
                float discreteTime = floor(_Time.y * _BoltFlickerSpeed);

                float boltMask = 0.0;
                boltMask += lightningBolt(surfaceUV, 0.0, discreteTime) * step(0.0, _BoltCount - 1.0);
                boltMask += lightningBolt(surfaceUV, 1.0, discreteTime) * step(1.0, _BoltCount - 1.0);
                boltMask += lightningBolt(surfaceUV, 2.0, discreteTime) * step(2.0, _BoltCount - 1.0);
                boltMask += lightningBolt(surfaceUV, 3.0, discreteTime) * step(3.0, _BoltCount - 1.0);
                boltMask += lightningBolt(surfaceUV, 4.0, discreteTime) * step(4.0, _BoltCount - 1.0);
                boltMask += lightningBolt(surfaceUV, 5.0, discreteTime) * step(5.0, _BoltCount - 1.0);
                boltMask = saturate(boltMask) * _BoltStrength;

                float intensity = (fillMask + scan + rim) * _GlowStrength + holoLines * _GlowStrength * 0.5;
                intensity *= (1.0 + glitchTrigger * 0.6);

                float alphaGrad = pow(saturate(1.0 - shiftedHeight01), _AlphaPower);
                alphaGrad = max(alphaGrad, _AlphaMin);

                float holoAlpha = saturate(alphaGrad * _HoloBaseAlpha + rim * 0.3 + boltMask * 0.5);

                float3 baseCol = _NeonColor.rgb * intensity;

                float colorShiftAmount = bandSeed * glitchTrigger * _GlitchColorShift;
                float3 col = lerp(baseCol, _GlitchColor.rgb * intensity, colorShiftAmount);

                col += _BoltColor.rgb * boltMask;

                col *= holoAlpha;

                return half4(col, holoAlpha);
            }
            ENDHLSL
        }
    }
}