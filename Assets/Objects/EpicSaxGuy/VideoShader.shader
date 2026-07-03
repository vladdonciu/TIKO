Shader "Custom/VideoNeonURP"
{
    Properties
    {
        _BaseMap ("Video Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.6, 1.0, 1.0, 1)
        _Intensity ("Intensity", Range(0, 10)) = 1.5

        _NeonColor ("Neon Color", Color) = (0,1,1,1)
        _NeonStrength ("Neon Strength", Range(0, 50)) = 8
        _RimPower ("Rim Power", Range(0.5, 10)) = 3.0

        _ScanTiling ("Scan Tiling", Range(1, 300)) = 80
        _ScanSpeed ("Scan Speed", Range(0, 10)) = 2
        _ScanStrength ("Scan Strength", Range(0, 10)) = 0.7
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

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
                float4 _Tint;
                float _Intensity;

                float4 _NeonColor;
                float _NeonStrength;
                float _RimPower;

                float _ScanTiling;
                float _ScanSpeed;
                float _ScanStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 posWS : TEXCOORD3;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posWS);
                OUT.posWS = posWS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                // video
                float3 videoCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;
                videoCol *= _Tint.rgb * _Intensity;

                // rim neon
                float rim = pow(saturate(1.0 - dot(N, V)), _RimPower);

                // scanlines (world-space Y)
                float scan = sin((IN.posWS.y * _ScanTiling) + (_Time.y * _ScanSpeed));
                scan = abs(scan);
                scan = smoothstep(0.85, 1.0, scan) * _ScanStrength;

                float3 neon = _NeonColor.rgb * (rim + scan) * _NeonStrength;

                float3 col = videoCol + neon;

                // alpha: keep visible
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}