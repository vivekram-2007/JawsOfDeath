Shader "Custom/LavaFlow"
{
    Properties
    {
        _BaseMap ("Base Color", 2D) = "white" {}
        _EmissiveMap ("Emissive Map", 2D) = "white" {}
        _Tint ("Base Tint", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,0.55,0.1,1)
        _EmissionIntensity ("Emission Intensity", Range(0,10)) = 3
        _Tiling ("Texture Tiling (world units)", Float) = 0.1
        _FlowSpeed ("Flow Speed", Range(0,0.2)) = 0.02
        _FlowDirection ("Flow Direction (XY)", Vector) = (1,0.4,0,0)
        _PulseSpeed ("Glow Pulse Speed", Range(0,5)) = 0.8
        _PulseAmount ("Glow Pulse Amount", Range(0,1)) = 0.25
        _WaveHeight ("Wave Height", Range(0,1)) = 0.08
        _WaveSpeed ("Wave Speed", Range(0,5)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLava"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissiveMap); SAMPLER(sampler_EmissiveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _Tiling;
                float _FlowSpeed;
                float4 _FlowDirection;
                float _PulseSpeed;
                float _PulseAmount;
                float _WaveHeight;
                float _WaveSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                // gentle slow waves so the surface heaves like thick liquid
                float t = _Time.y * _WaveSpeed;
                posWS.y += sin(posWS.x * 0.35 + t) * _WaveHeight;
                posWS.y += sin(posWS.z * 0.50 + t * 0.8) * _WaveHeight;

                OUT.positionCS = TransformWorldToHClip(posWS);
                // world-space UVs: no UV unwrap needed, works on any plane or mesh
                OUT.uv = posWS.xz * _Tiling;
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _FlowSpeed;
                float2 dir = normalize(_FlowDirection.xy + 0.0001);

                // two layers drifting in different directions give a slow churning look
                float2 uvA = IN.uv + dir * t;
                float2 uvB = IN.uv * 0.73 + float2(-dir.y, dir.x) * t * 0.7 + 0.37;

                half3 baseA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA).rgb;
                half3 baseB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB).rgb;
                half3 emiA = SAMPLE_TEXTURE2D(_EmissiveMap, sampler_EmissiveMap, uvA).rgb;
                half3 emiB = SAMPLE_TEXTURE2D(_EmissiveMap, sampler_EmissiveMap, uvB).rgb;

                half3 baseCol = (baseA + baseB) * 0.5 * _Tint.rgb;
                half3 emi = (emiA + emiB) * 0.5;

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                half3 glow = emi * _EmissionColor.rgb * _EmissionIntensity * pulse;

                half3 col = baseCol * 0.5 + glow;
                col = MixFog(col, IN.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
