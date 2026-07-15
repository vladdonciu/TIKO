Shader "Custom/CRTWobble"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WobbleStrength ("Wobble Strength", Range(0, 0.01)) = 0.002
        _WobbleSpeed ("Wobble Speed", Range(0, 5)) = 1
        _ScanlineOpacity ("Scanline Opacity", Range(0, 0.3)) = 0.08
        _ScanlineSpeed ("Scanline Speed", Range(0, 20)) = 8
        _Time01 ("Time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _WobbleStrength;
            float _WobbleSpeed;
            float _ScanlineOpacity;
            float _ScanlineSpeed;
            float _Time01;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float wave = sin(uv.y * 40 + _Time01 * _WobbleSpeed) * _WobbleStrength;
                uv.x += wave;

                fixed4 col = tex2D(_MainTex, uv);

                float scanline = sin((uv.y * 300) - _Time01 * _ScanlineSpeed) * _ScanlineOpacity;
                col.rgb -= scanline;

                return col;
            }
            ENDCG
        }
    }
}