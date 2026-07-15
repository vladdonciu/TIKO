Shader "Custom/GlitchOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlitchAmount ("Glitch Amount", Range(0,1)) = 0
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
            float _GlitchAmount;
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

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float sliceY = floor(uv.y * 24) / 24;
                float noise = rand(float2(sliceY, _Time01));
                float shift = (noise - 0.5) * 0.05 * _GlitchAmount;
                uv.x += shift;

                float r = tex2D(_MainTex, uv + float2(0.004 * _GlitchAmount, 0)).r;
                float g = tex2D(_MainTex, uv).g;
                float b = tex2D(_MainTex, uv - float2(0.004 * _GlitchAmount, 0)).b;

                return fixed4(r, g, b, 1);
            }
            ENDCG
        }
    }
}