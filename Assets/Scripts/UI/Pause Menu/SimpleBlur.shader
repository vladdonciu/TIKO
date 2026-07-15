Shader "Custom/SimpleBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 15)) = 4
        _TintColor ("Tint Color", Color) = (0.04, 0.04, 0.04, 0.35)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "HorizontalBlur"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            static const float weights[7] = { 0.071, 0.131, 0.191, 0.214, 0.191, 0.131, 0.071 };
            static const float offsets[7] = { -3, -2, -1, 0, 1, 2, 3 };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = float4(0,0,0,0);
                float texel = _MainTex_TexelSize.x * _BlurSize;

                for (int k = 0; k < 7; k++)
                {
                    float2 uv = i.uv + float2(offsets[k] * texel, 0);
                    col += tex2D(_MainTex, uv) * weights[k];
                }
                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "VerticalBlurAndTint"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            float4 _TintColor;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            static const float weights[7] = { 0.071, 0.131, 0.191, 0.214, 0.191, 0.131, 0.071 };
            static const float offsets[7] = { -3, -2, -1, 0, 1, 2, 3 };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = float4(0,0,0,0);
                float texel = _MainTex_TexelSize.y * _BlurSize;

                for (int k = 0; k < 7; k++)
                {
                    float2 uv = i.uv + float2(0, offsets[k] * texel);
                    col += tex2D(_MainTex, uv) * weights[k];
                }

                col.rgb = lerp(col.rgb, _TintColor.rgb, _TintColor.a);
                return col;
            }
            ENDCG
        }
    }
}