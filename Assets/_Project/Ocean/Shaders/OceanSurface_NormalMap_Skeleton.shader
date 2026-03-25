Shader "PirateSeas/OceanSurface"
{
    Properties
    {
        [Header(Water Color)]
        _WaterColorNear ("Near", Color) = (0.04, 0.16, 0.2, 1)
        _WaterColorFar ("Horizon", Color) = (0.02, 0.07, 0.12, 1)
        _HorizonDistance ("Blend distance", Range(10, 500)) = 150

        [Header(Reflection)]
        _FresnelPower ("Fresnel power", Range(1, 10)) = 4
        _FresnelBias ("Min reflection", Range(0, 0.15)) = 0.02
        _ReflectionStrength ("Strength", Range(0, 1)) = 0.7
        _SkyHorizonColor ("Sky horizon", Color) = (0.15, 0.25, 0.35, 1)
        _SkyZenithColor ("Sky zenith", Color) = (0.4, 0.6, 0.8, 1)

        [Header(Specular)]
        _SpecularPower ("Tightness", Range(64, 2048)) = 512
        _SpecularStrength ("Strength", Range(0, 5)) = 2.0

        [Header(Subsurface Scattering)]
        _SSSColor ("Tint", Color) = (0.05, 0.3, 0.2, 1)
        _SSSStrength ("Strength", Range(0, 3)) = 1.0
        _SSSPower ("Falloff", Range(1, 8)) = 3

        [Header(Detail Normals)]
        _DetailNormalMap ("Normal Map", 2D) = "bump" {}
        _DetailTiling1 ("Layer 1 Tiling", Float) = 8
        _DetailTiling2 ("Layer 2 Tiling", Float) = 4
        _DetailSpeed1 ("Layer 1 Speed", Vector) = (0.03, 0.02, 0, 0)
        _DetailSpeed2 ("Layer 2 Speed", Vector) = (-0.02, 0.03, 0, 0)
        _DetailNormalStrength ("Detail Strength", Range(0, 1)) = 0.3

        [Header(Displacement)]
        [HideInInspector] _HeightMap ("", 2D) = "black" {}
        [HideInInspector] _DisplaceXMap ("", 2D) = "black" {}
        [HideInInspector] _DisplaceZMap ("", 2D) = "black" {}
        [HideInInspector] _MeshSize ("", Float) = 500
        _DisplacementStrength ("Wave height", Range(0, 30)) = 8
        _ChoppyStrength ("Choppy", Range(0, 2)) = 0.6
        _NormalStrength ("Normal detail", Range(0.1, 5)) = 1.5
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
                half4 _SkyHorizonColor;
                half4 _SkyZenithColor;

                half _SpecularPower;
                half _SpecularStrength;

                half4 _SSSColor;
                half _SSSStrength;
                half _SSSPower;

                float _DetailTiling1;
                float _DetailTiling2;
                float4 _DetailSpeed1;
                float4 _DetailSpeed2;
                half _DetailNormalStrength;

                float _MeshSize;
                float _DisplacementStrength;
                float _ChoppyStrength;
                float _NormalStrength;
            CBUFFER_END

            TEXTURE2D(_HeightMap);    SAMPLER(sampler_HeightMap);
            TEXTURE2D(_DisplaceXMap); SAMPLER(sampler_DisplaceXMap);
            TEXTURE2D(_DisplaceZMap); SAMPLER(sampler_DisplaceZMap);
            TEXTURE2D(_DetailNormalMap); SAMPLER(sampler_DetailNormalMap);

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

                float height = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, uv, 0).x;
                float displaceX = SAMPLE_TEXTURE2D_LOD(_DisplaceXMap, sampler_DisplaceXMap, uv, 0).x;
                float displaceZ = SAMPLE_TEXTURE2D_LOD(_DisplaceZMap, sampler_DisplaceZMap, uv, 0).x;

                float3 displaced = input.positionOS.xyz;
                displaced.y += height * _DisplacementStrength;
                displaced.x += displaceX * _ChoppyStrength;
                displaced.z += displaceZ * _ChoppyStrength;

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

            // Blend UDN : ajoute la perturbation detail (tangent space xy)
            // sur la normale de base (world space, Y-up).
            float3 BlendNormalUDN(float3 base, float3 detail)
            {
                return normalize(float3(
                    base.x + detail.x,
                    base.y,
                    base.z + detail.y
                ));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // ─── TODO : Perturber la normale FFT avec du micro-détail ─
                //
                // Objectif : la variable `normal` ci-dessus contient la
                // normale FFT (grandes vagues). Avant qu'elle soit utilisée
                // par le lighting en dessous, tu dois y ajouter du détail
                // haute fréquence issu de _DetailNormalMap.
                //
                // Ce que tu dois produire :
                //   - Deux samplings animés de la même normal map
                //     (échelles et vitesses différentes, UVs world-space)
                //   - Un blend des deux layers
                //   - La normale FFT perturbée par ce détail
                //
                // Tu as à ta disposition :
                //   - input.positionWS, _DetailTiling1/2, _DetailSpeed1/2
                //   - _DetailNormalMap / sampler_DetailNormalMap
                //   - UnpackNormal(), _DetailNormalStrength
                //   - BlendNormalUDN() définie juste au-dessus
                //   - _Time.y (temps en secondes)
                //
                // Écris tout ton code ici, puis écrase `normal` avec
                // le résultat final.
                // ──────────────────────────────────────────────────────────



                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 lightColor = mainLight.color;

                float dist = distance(input.positionWS, _WorldSpaceCameraPos);
                float horizonFactor = saturate(dist / _HorizonDistance);
                half3 waterColor = lerp(_WaterColorNear.rgb, _WaterColorFar.rgb, horizonFactor);
                float NdotL = saturate(dot(normal, lightDir) * 0.5 + 0.5);
                waterColor *= lerp(0.6, 1.0, NdotL);

                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = _FresnelBias + (1.0 - _FresnelBias) * pow(1.0 - NdotV, _FresnelPower);

                float3 reflectDir = reflect(-viewDir, normal);
                half3 skyColor = lerp(_SkyHorizonColor.rgb, _SkyZenithColor.rgb, saturate(reflectDir.y));
                half3 reflection = skyColor * _ReflectionStrength;

                float3 halfVec = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normal, halfVec));
                half3 specular = lightColor * pow(NdotH, _SpecularPower) * _SpecularStrength;

                float3 sssDir = lightDir + normal * 0.5;
                float sssDot = saturate(dot(viewDir, -sssDir));
                float sssAmount = pow(sssDot, _SSSPower) * _SSSStrength;
                float heightBoost = saturate(input.waveHeight * _DisplacementStrength * 0.3);
                half3 sss = _SSSColor.rgb * sssAmount * heightBoost * lightColor;

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
