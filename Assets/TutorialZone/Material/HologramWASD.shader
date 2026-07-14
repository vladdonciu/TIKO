Shader "TIKO/UI/HologramWASD"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color   ("Tint Color", Color) = (0.24, 1.0, 0.08, 0.5) // aproximativ Phosphor Green [file:1]
        _EmissionStrength ("Emission Strength", Float) = 1.5

        _ScanlineSpeed   ("Scanline Speed", Float) = 1.0
        _ScanlineDensity ("Scanline Density", Float) = 60.0

        _GlitchIntensity ("Glitch Intensity", Float) = 0.02
        _GlitchScale     ("Glitch Noise Scale", Float) = 10.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "HologramPass"
            Tags { "LightMode" = "UniversalForward" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;

            fixed4 _Color;
            float  _EmissionStrength;

            float _ScanlineSpeed;
            float _ScanlineDensity;

            float _GlitchIntensity;
            float _GlitchScale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            // noise simplu bazat pe UV (pseudo-noise)
            float hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p.x * 43758.5453 + p.y * 12345.6789));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // --- GLITCH UV ---
                float2 glitchUV = uv * _GlitchScale;
                float glitchNoiseX = hash(glitchUV + _Time.y);
                float glitchNoiseY = hash(glitchUV + _Time.y * 1.37);
                float2 glitchOffset = float2(glitchNoiseX, glitchNoiseY) - 0.5;
                glitchOffset *= _GlitchIntensity;

                uv += glitchOffset;

                // --- TEXTURA DE BAZĂ ---
                fixed4 texColor = tex2D(_MainTex, uv);

                // combinăm cu culoarea de tint + vertex color
                fixed4 baseColor = texColor * _Color * i.color;

                // --- SCANLINES ---
                // folosim UV.y + Time pentru dungi care se mișcă
                float scanT = uv.y + _Time.y * _ScanlineSpeed;
                float scanVal = sin(scanT * _ScanlineDensity); // -1..1
                scanVal = abs(scanVal);                        // 0..1

                // amplificăm doar luminozitatea, nu alpha
                float emissionFactor = lerp(1.0, 1.5, scanVal);

                // --- EMISSION ---
                fixed3 emissive = baseColor.rgb * _EmissionStrength * emissionFactor;

                // alpha vine din textură * tint
                fixed alpha = baseColor.a;

                fixed4 finalColor;
                finalColor.rgb = emissive;
                finalColor.a   = alpha;

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}