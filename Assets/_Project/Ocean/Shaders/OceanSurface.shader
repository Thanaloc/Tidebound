// Ocean surface shader for URP — supports both Gerstner (CPU) and FFT (GPU) displacement.
//
// In Gerstner mode: vertices are displaced by C# code, shader just does coloring.
// In FFT mode: vertex shader samples displacement textures and moves vertices on the GPU.
// This avoids the CPU readback bottleneck entirely.
Shader "PirateSeas/OceanSurface"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.1, 0.6, 0.7, 0.9)
        _DeepColor ("Deep Color", Color) = (0.02, 0.1, 0.2, 1)
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 3
        _DepthDistance ("Depth Fade Distance", Float) = 10

        // FFT displacement maps (set from C# — not visible in inspector)
        [HideInInspector] _HeightMap ("Height Map", 2D) = "black" {}
        [HideInInspector] _DisplaceXMap ("Displace X Map", 2D) = "black" {}
        [HideInInspector] _DisplaceZMap ("Displace Z Map", 2D) = "black" {}
        [HideInInspector] _MeshSize ("Mesh Size", Float) = 200

        _DisplacementStrength ("Displacement Strength", Range(0, 50)) = 20
        _ChoppyStrength ("Choppy Strength", Range(0, 5)) = 1.5
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
                float _MeshSize;
                float _DisplacementStrength;
                float _ChoppyStrength;
            CBUFFER_END

            TEXTURE2D(_HeightMap);      SAMPLER(sampler_HeightMap);
            TEXTURE2D(_DisplaceXMap);    SAMPLER(sampler_DisplaceXMap);
            TEXTURE2D(_DisplaceZMap);    SAMPLER(sampler_DisplaceZMap);

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

                // Sample FFT displacement textures using the UV
                float2 uv = input.uv;

                // The IFFT output is a complex number — the .x (real part) is what we want.
                float height = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv, 0).x;
                float displaceX = SAMPLE_TEXTURE2D_LOD(_DisplaceXMap, sampler_DisplaceXMap, uv, 0).x;
                float displaceZ = SAMPLE_TEXTURE2D_LOD(_DisplaceZMap, sampler_DisplaceZMap, uv, 0).x;

                // Apply displacement in object space
                float3 displaced = input.positionOS.xyz;
                displaced.y += height * _DisplacementStrength;
                displaced.x += displaceX * _ChoppyStrength;
                displaced.z += displaceZ * _ChoppyStrength;

                // Compute normal from height map neighbors (finite differences)
                float texelSize = 1.0 / 256.0;
                float hL = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(-texelSize, 0), 0).x;
                float hR = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(texelSize, 0), 0).x;
                float hD = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(0, -texelSize), 0).x;
                float hU = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(0, texelSize), 0).x;

                float3 computedNormal = normalize(float3(
                    (hL - hR) * _DisplacementStrength,
                    2.0,
                    (hD - hU) * _DisplacementStrength
                ));

                // Use computed normal if FFT is active (height != 0), otherwise use mesh normal
                float3 normalOS = (abs(height) > 0.0001) ? computedNormal : input.normalOS;

                VertexPositionInputs posInputs = GetVertexPositionInputs(displaced);
                VertexNormalInputs normInputs = GetVertexNormalInputs(normalOS);

                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.screenPos = ComputeScreenPos(posInputs.positionCS);
                output.uv = uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepthRaw = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float waterDepth = LinearEyeDepth(input.positionCS.z / input.positionCS.w, _ZBufferParams);
                float depthDiff = saturate((sceneDepth - waterDepth) / _DepthDistance);

                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthDiff);
                waterColor.rgb = lerp(waterColor.rgb, half3(0.8, 0.9, 1.0), fresnel * 0.3);

                return waterColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
