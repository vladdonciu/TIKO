Shader "Custom/HeatGaugeFill"
{
    Properties
    {
        _FillAmount ("Fill Amount", Range(0,1)) = 1
        _FillColor ("Fill Color", Color) = (0,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0.04,0.04,0.04,1)
        _EmissionStrength ("Emission Strength", Range(0,5)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            float _FillAmount;
            fixed4 _FillColor;
            fixed4 _BackgroundColor;
            float _EmissionStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 baseColor = (i.uv.x <= _FillAmount) ? _FillColor : _BackgroundColor;
                fixed4 emission = baseColor * _EmissionStrength;
                return baseColor + emission * 0.5; // combina culoarea de baza cu glow-ul
            }
            ENDCG
        }
    }
}