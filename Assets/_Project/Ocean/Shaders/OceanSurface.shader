// PBR ocean surface shader for URP.
// Height-only displacement (no choppy = no triangle folding).
// Smooth normals from heightmap finite differences.
// Fresnel, sky reflection, sun specular, subsurface scattering.
Shader "PirateSeas/OceanSurface"
{
    Properties
    {
        [Header(Water Color)]
        _WaterColorNear ("Water color (near)", Color) = (0.04, 0.16, 0.2, 1)
        _WaterColorFar ("Water color (horizon)", Color) = (0.02, 0.07, 0.12, 1)
        _HorizonDistance ("Horizon blend distance", Range(10, 500)) = 150

        [Header(Reflection)]
        _FresnelPower ("Fresnel power", Range(1, 10)) = 4
        _FresnelBias ("Min reflection", Range(0, 0.15)) = 0.02
        _ReflectionStrength ("Reflection strength", Range(0, 1)) = 0.7

        [Header(Specular)]
        _SpecularPower ("Sun tightness", Range(64, 2048)) = 512
        _SpecularStrength ("Sun strength", Range(0, 5)) = 2.0

        [Header(Subsurface Scattering)]
        _SSSColor ("SSS tint", Color) = (0.05, 0.3, 0.2, 1)
        _SSSStrength ("SSS strength", Range(0, 3)) = 1.0
        _SSSPower ("SSS falloff", Range(1, 8)) = 3

        [Header(FFT Displacement)]
        [HideInInspector] _HeightMap ("", 2D) = "black" {}
        [HideInInspector] _MeshSize ("", Float) = 500
        _DisplacementStrength ("Wave height", Range(0, 30)) = 8
        _NormalStrength ("Normal strength", Range(0.1, 5)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite On
        Cull Back

        Pass
        {
            Name "OceanForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColorNear;
                half4 _WaterColorFar;
                float _HorizonDistance;

                half _FresnelPower;
                half _FresnelBias;
                half _ReflectionStrength;

                half _SpecularPower;
                half _SpecularStrength;

                half4 _SSSColor;
                half _SSSStrength;
                half _SSSPower;

                float _MeshSize;
                float _DisplacementStrength;
                float _NormalStrength;
            CBUFFER_END

            TEXTURE2D(_HeightMap); SAMPLER(sampler_HeightMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float3 normalWS      : TEXCOORD0;
                float3 viewDirWS     : TEXCOORD1;
                float3 positionWS    : TEXCOORD2;
                float  waveHeight    : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float2 uv = input.uv;

                // Height-only displacement — no horizontal, no folding
                float height = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv, 0).x;

                float3 displaced = input.positionOS.xyz;
                displaced.y += height * _DisplacementStrength;

                // Smooth normals from heightmap finite differences
                float texelSize = 1.0 / 512.0;
                float hL = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(-texelSize, 0), 0).x;
                float hR = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(texelSize, 0), 0).x;
                float hD = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(0, -texelSize), 0).x;
                float hU = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv + float2(0, texelSize), 0).x;

                float3 computedNormal = normalize(float3(
                    (hL - hR) * _DisplacementStrength * _NormalStrength,
                    1.0,
                    (hD - hU) * _DisplacementStrength * _NormalStrength
                ));

                // Use FFT normal if available, fallback to mesh normal
                float3 normalOS = (abs(height) > 0.0001) ? computedNormal : input.normalOS;

                VertexPositionInputs posInputs = GetVertexPositionInputs(displaced);
                VertexNormalInputs normInputs = GetVertexNormalInputs(normalOS);

                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                output.positionWS = posInputs.positionWS;
                output.waveHeight = height;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;

                // ── Water base color ──
                float dist = distance(input.positionWS, _WorldSpaceCameraPos);
                float horizonFactor = saturate(dist / _HorizonDistance);
                half3 waterColor = lerp(_WaterColorNear.rgb, _WaterColorFar.rgb, horizonFactor);

                // Soft wrap lighting
                float NdotL = saturate(dot(normal, lightDir) * 0.5 + 0.5);
                waterColor *= lerp(0.6, 1.0, NdotL);

                // ── Fresnel (Schlick) ──
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = _FresnelBias + (1.0 - _FresnelBias) * pow(1.0 - NdotV, _FresnelPower);

                // ── Sky reflection ──
                float3 reflectDir = reflect(-viewDir, normal);
                half3 skyColor = lerp(
                    half3(0.15, 0.25, 0.35),   // horizon color
                    half3(0.4, 0.6, 0.8),      // zenith color
                    saturate(reflectDir.y)
                );
                half3 reflection = skyColor * _ReflectionStrength;

                // ── Specular (sun glint) ──
                float3 halfVec = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfVec));
                half3 specular = lightColor * pow(NdotH, _SpecularPower) * _SpecularStrength;

                // ── Subsurface scattering ──
                float3 sssDir = lightDir + normal * 0.5;
                float sssDot = saturate(dot(viewDir, -sssDir));
                float sssAmount = pow(sssDot, _SSSPower) * _SSSStrength;
                float heightBoost = saturate(input.waveHeight * _DisplacementStrength * 0.3);
                half3 sss = _SSSColor.rgb * sssAmount * heightBoost * lightColor;

                // ── Final composite ──
                half3 finalColor = lerp(waterColor, reflection, fresnel);
                finalColor += specular;
                finalColor += sss;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
