Shader "TIKO/UI/HologramTriangle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.22, 1.0, 0.08, 1.0)

        [Header(Shape)]
        _BottomWidth ("Bottom Width", Range(0.0, 1.0)) = 0.02
        _TopWidth ("Top Width", Range(0.0, 1.0)) = 0.95
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.025

        [Header(Transparency)]
        _BottomAlpha ("Bottom Alpha", Range(0.0, 1.0)) = 0.04
        _TopAlpha ("Top Alpha", Range(0.0, 1.0)) = 0.34

        [Header(Projection Glow)]
        _GlowColor ("Glow Color", Color) = (0.35, 1.0, 0.12, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0.0, 5.0)) = 2.0
        _GlowRadius ("Glow Radius", Range(0.01, 1.0)) = 0.26
        _GlowHeight ("Glow Height", Range(0.01, 1.0)) = 0.18
        _PulseSpeed ("Pulse Speed", Range(0.0, 10.0)) = 2.0

        [Header(Scanlines)]
        _ScanlineStrength ("Scanline Strength", Range(0.0, 1.0)) = 0.22
        _ScanlineDensity ("Scanline Density", Range(1.0, 200.0)) = 45.0
        _ScanlineSpeed ("Scanline Speed", Range(-10.0, 10.0)) = 1.2

        [Header(Glitch)]
        _GlitchAmount ("Glitch Amount", Range(0.0, 1.0)) = 0.35
        _GlitchSpeed ("Glitch Speed", Range(0.0, 20.0)) = 6.0
        _GlitchBlockCount ("Glitch Block Count", Range(1.0, 60.0)) = 18.0
        _GlitchFrequency ("Glitch Frequency", Range(0.0, 1.0)) = 0.12
        _ChromaticAberration ("Chromatic Aberration", Range(0.0, 0.05)) = 0.012
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _BottomWidth;
            float _TopWidth;
            float _EdgeSoftness;

            float _BottomAlpha;
            float _TopAlpha;

            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowRadius;
            float _GlowHeight;
            float _PulseSpeed;

            float _ScanlineStrength;
            float _ScanlineDensity;
            float _ScanlineSpeed;

            float _GlitchAmount;
            float _GlitchSpeed;
            float _GlitchBlockCount;
            float _GlitchFrequency;
            float _ChromaticAberration;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            // Hash simplu, suficient de bun pentru glitch pe UI.
            float hash11(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453123);
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // ---- Glitch: rupem UV-ul în benzi orizontale random ----
                float glitchTimeStep = floor(_Time.y * _GlitchSpeed);
                float blockIndex = floor(uv.y * _GlitchBlockCount);

                float blockSeed = hash11(blockIndex + glitchTimeStep * 13.37);

                // Un glitch "trigger" random, ca să nu fie constant, ci intermitent.
                float glitchActive = step(1.0 - _GlitchFrequency, hash11(glitchTimeStep));

                float glitchOffset =
                    (blockSeed - 0.5) * 2.0 *
                    _GlitchAmount * 0.08 *
                    glitchActive;

                float2 glitchedUv = uv;
                glitchedUv.x += glitchOffset;

                // ---- Forma triunghiulară, calculată cu UV-ul glitch-uit ----
                float halfWidth = lerp(_BottomWidth, _TopWidth, glitchedUv.y) * 0.5;
                float distanceFromCenter = abs(glitchedUv.x - 0.5);

                float shapeMask = 1.0 - smoothstep(
                    halfWidth,
                    halfWidth + _EdgeSoftness,
                    distanceFromCenter
                );

                float verticalAlpha = lerp(_BottomAlpha, _TopAlpha, uv.y);

                // ---- Glow de proiecție, la baza formei ----
                float2 glowCenter = float2(0.5, 0.0);
                float horizontalDistance = (uv.x - glowCenter.x) / _GlowRadius;
                float verticalDistance = (uv.y - glowCenter.y) / _GlowHeight;

                float glowDistance =
                    horizontalDistance * horizontalDistance +
                    verticalDistance * verticalDistance;

                float pulse = 0.75 + 0.25 * sin(_Time.y * _PulseSpeed);

                float projectionGlow =
                    exp(-glowDistance * 3.5) *
                    _GlowIntensity *
                    pulse;

                // ---- Scanlines în mișcare verticală ----
                float movingScanUv = uv.y - (_Time.y * _ScanlineSpeed);

                float scanline =
                    sin(movingScanUv * _ScanlineDensity * 6.283185) * 0.5 + 0.5;

                scanline = smoothstep(0.55, 0.9, scanline);

                float scanlineModulation = lerp(
                    1.0 - _ScanlineStrength,
                    1.0 + (_ScanlineStrength * 0.35),
                    scanline
                );

                // ---- Flicker global de intensitate, tipic sci-fi hologram ----
                float flicker =
                    0.9 + 0.1 * hash11(floor(_Time.y * 18.0));

                flicker = lerp(1.0, flicker, glitchActive * 0.6 + 0.15);

                // ---- Sample cu aberație cromatică ușoară pe glitch ----
                float chroma = _ChromaticAberration * (0.3 + glitchActive * 1.4);

                fixed4 spriteR = tex2D(_MainTex, glitchedUv + float2(chroma, 0.0));
                fixed4 spriteG = tex2D(_MainTex, glitchedUv);
                fixed4 spriteB = tex2D(_MainTex, glitchedUv - float2(chroma, 0.0));

                fixed4 sprite = fixed4(
                    spriteR.r,
                    spriteG.g,
                    spriteB.b,
                    spriteG.a
                ) * i.color;

                fixed3 panelColor =
                    _Color.rgb *
                    scanlineModulation *
                    flicker;

                fixed3 finalColor =
                    panelColor +
                    (_GlowColor.rgb * projectionGlow);

                float finalAlpha =
                    shapeMask *
                    verticalAlpha *
                    sprite.a *
                    flicker;

                finalAlpha +=
                    shapeMask *
                    projectionGlow *
                    0.16;

                return fixed4(finalColor, saturate(finalAlpha));
            }
            ENDCG
        }
    }
}