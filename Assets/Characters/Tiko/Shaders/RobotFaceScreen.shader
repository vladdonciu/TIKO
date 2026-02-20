Shader "Tiko/RobotFaceScreen"
{
    Properties
    {
        [Header(Eye Shape)]
        _EyeWidth ("Eye Width", Range(0.05, 0.4)) = 0.12
        _EyeHeight ("Eye Height", Range(0.1, 0.6)) = 0.35
        _CornerRadius ("Corner Radius", Range(0, 1)) = 0.3
        _EyeSpacing ("Eye Spacing", Range(0.05, 0.3)) = 0.15
        _EyeVerticalOffset ("Eye Vertical Offset", Range(-0.2, 0.2)) = 0.05

        [Header(Eye Transform)]
        _LookOffset ("Look Offset", Vector) = (0, 0, 0, 0)
        _LeftEyeRotation ("Left Eye Rotation", Range(-45, 45)) = 0
        _RightEyeRotation ("Right Eye Rotation", Range(-45, 45)) = 0
        _LeftEyeSquash ("Left Eye Squash", Vector) = (1, 1, 0, 0)
        _RightEyeSquash ("Right Eye Squash", Vector) = (1, 1, 0, 0)
        _BlinkAmount ("Blink Amount", Range(0, 1)) = 0

        [Header(Emission and Glow)]
        [HDR] _EyeEmissionColor ("Eye Emission HDR", Color) = (0, 2, 3, 1)
        _ScreenColor ("Screen Base Color", Color) = (0.02, 0.02, 0.04, 1)
        [HDR] _ScreenEmissionColor ("Screen Emission HDR", Color) = (0.05, 0.08, 0.12, 1)
        _GlowSoftness ("Glow Softness", Range(0, 0.15)) = 0.04
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.2

        [Header(Screen Effects)]
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.08
        _ScanlineCount ("Scanline Count", Range(20, 500)) = 180
        _ScanlineSpeed ("Scanline Scroll Speed", Range(0, 5)) = 0.8
        _ScreenFlicker ("Screen Flicker", Range(0, 0.1)) = 0.02
        _ScreenNoise ("Screen Noise", Range(0, 0.1)) = 0.015
        _VignetteIntensity ("Vignette", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float  _EyeWidth;
                float  _EyeHeight;
                float  _CornerRadius;
                float  _EyeSpacing;
                float  _EyeVerticalOffset;
                float4 _LookOffset;
                float  _LeftEyeRotation;
                float  _RightEyeRotation;
                float4 _LeftEyeSquash;
                float4 _RightEyeSquash;
                float  _BlinkAmount;
                float4 _EyeEmissionColor;
                float4 _ScreenColor;
                float4 _ScreenEmissionColor;
                float  _GlowSoftness;
                float  _GlowIntensity;
                float  _ScanlineIntensity;
                float  _ScanlineCount;
                float  _ScanlineSpeed;
                float  _ScreenFlicker;
                float  _ScreenNoise;
                float  _VignetteIntensity;
            CBUFFER_END

            // ── Utilities ──

            float roundedRectSDF(float2 p, float2 halfSize, float radius)
            {
                float2 d = abs(p) - halfSize + radius;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
            }

            float2 rotate2D(float2 p, float angleDeg)
            {
                float rad = radians(angleDeg);
                float s = sin(rad);
                float c = cos(rad);
                return float2(p.x * c - p.y * s, p.x * s + p.y * c);
            }

            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // ── Eye ──
            // Returns: x = mask, y = raw SDF dist
            float2 drawEye(float2 uv, float2 center, float2 squash,
                           float rotation, float width, float height,
                           float radius, float blinkAmt)
            {
                float2 p = uv - center;
                p = rotate2D(p, rotation);
                p /= max(squash, 0.01);

                float effH = height * (1.0 - blinkAmt * 0.95);
                float2 half2 = float2(width, effH) * 0.5;
                float cr = radius * min(half2.x, half2.y);
                float dist = roundedRectSDF(p, half2, cr);

                float aa = fwidth(dist) * 1.5;
                float mask = 1.0 - smoothstep(-aa, aa, dist);
                return float2(mask, dist);
            }

            // ── Screen FX ──

            float scanlines(float2 uv)
            {
                float scanWave = sin((uv.y + _Time.y * _ScanlineSpeed)
                                * _ScanlineCount * 3.14159 * 2.0);
                return 1.0 - _ScanlineIntensity * 0.5 * (scanWave * 0.5 + 0.5);
            }

            float screenFlicker()
            {
                float f = sin(_Time.y * 60.0) * sin(_Time.y * 13.7) * sin(_Time.y * 7.3);
                return 1.0 - _ScreenFlicker * (f * 0.5 + 0.5);
            }

            float screenNoise(float2 uv)
            {
                float2 nUV = uv * 100.0 + _Time.y * 50.0;
                return 1.0 - _ScreenNoise * hash(floor(nUV));
            }

            float vignette(float2 uv)
            {
                float2 c = uv - 0.5;
                return 1.0 - _VignetteIntensity * dot(c, c) * 2.0;
            }

            // ── Vertex / Fragment ──

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                float2 look = _LookOffset.xy;
                float2 lc = float2(0.5 - _EyeSpacing, 0.5 + _EyeVerticalOffset) + look;
                float2 rc = float2(0.5 + _EyeSpacing, 0.5 + _EyeVerticalOffset) + look;

                float2 L = drawEye(uv, lc, _LeftEyeSquash.xy,
                    _LeftEyeRotation, _EyeWidth, _EyeHeight,
                    _CornerRadius, _BlinkAmount);

                float2 R = drawEye(uv, rc, _RightEyeSquash.xy,
                    _RightEyeRotation, _EyeWidth, _EyeHeight,
                    _CornerRadius, _BlinkAmount);

                float eyeMask = saturate(L.x + R.x);
                float minDist = min(L.y, R.y);

                // Glow
                float gf = 1.0 - saturate(minDist / max(_GlowSoftness, 0.001));
                gf = pow(gf, 2.0) * _GlowIntensity;
                float glowMask = gf * (1.0 - eyeMask);

                // Screen FX
                float fx = scanlines(uv) * screenFlicker()
                         * screenNoise(uv) * vignette(uv);

                // Compose
                half3 screenCol = _ScreenColor.rgb + _ScreenEmissionColor.rgb * fx;
                half3 eyeCol    = _EyeEmissionColor.rgb * fx;
                half3 glowCol   = _EyeEmissionColor.rgb * glowMask * fx * 0.5;

                half3 final3 = lerp(screenCol, eyeCol, eyeMask) + glowCol;

                return half4(final3, 1.0);
            }
            ENDHLSL
        }
    }
}