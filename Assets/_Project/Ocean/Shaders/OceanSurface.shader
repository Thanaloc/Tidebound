// Base ocean surface shader for URP.
// Phase 1A: depth-based color blend + fresnel. Simple but solid foundation.
// We'll layer on refraction, reflection, SSS, and foam in Phase 1D.
Shader "PirateSeas/OceanSurface"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.1, 0.6, 0.7, 0.9)
        _DeepColor ("Deep Color", Color) = (0.02, 0.1, 0.2, 1)
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 3
        _DepthDistance ("Depth Fade Distance", Float) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "OceanForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half _FresnelPower;
                float _DepthDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.screenPos = ComputeScreenPos(posInputs.positionCS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Fresnel: water gets more reflective at grazing angles.
                // This is what gives the ocean that sense of depth and shine.
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);

                // Depth fade: compare the water surface depth with whatever is behind it.
                // Deeper = darker color. This sells the shallow-to-deep transition near shores.
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepthRaw = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float waterDepth = LinearEyeDepth(input.positionCS.z / input.positionCS.w, _ZBufferParams);
                float depthDiff = saturate((sceneDepth - waterDepth) / _DepthDistance);

                // Blend shallow/deep based on depth, then add a subtle bright tint from fresnel
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthDiff);
                waterColor.rgb = lerp(waterColor.rgb, half3(0.8, 0.9, 1.0), fresnel * 0.3);

                return waterColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
