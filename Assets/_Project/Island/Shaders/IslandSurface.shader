Shader "Tidebound/IslandSurface"
{
    Properties
    {
        [Header(Zone Colors)]
        _SandColor ("Sand Color", Color) = (0.82, 0.72, 0.55, 1)
        _GrassColor ("Grass Color", Color) = (0.30, 0.50, 0.18, 1)
        _RockColor ("Rock Color", Color) = (0.40, 0.38, 0.35, 1)

        [Header(Height Thresholds)]
        _SandToGrass ("Sand to Grass Height", Float) = 2.0
        _GrassToRock ("Grass to Rock Height", Float) = 12.0
        _BlendRange ("Blend Range", Float) = 3.0

        _SlopeThreshold ("Slope Rock Threshold", Range(0, 1)) = 0.6
        _SlopeBlend ("Slope Rock Blend", Range(0, 0.5)) = 0.15

        [Header(Noise)]
        _NoiseScale ("Noise Scale", Float) = 0.15
        _HeightNoisePower ("Height Noise Perturbation", Float) = 2.5
        _ColorVariation ("Color Variation", Float) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SandColor;
                half4 _GrassColor;
                half4 _RockColor;
                float _SandToGrass;
                float _GrassToRock;
                float _BlendRange;
                float _NoiseScale;
                float _HeightNoisePower;
                float _ColorVariation;
                float _SlopeThreshold;
                float _SlopeBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            // Simple hash-based noise — no texture needed
            // Returns a value roughly in [-1, 1]
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z) * 2.0 - 1.0;
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep curve

                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 posWS = input.positionWS;
                float3 normalWS = normalize(input.normalWS);

                // --- Height-based coloring ---

                // Perturb the height with noise based on XZ
                // This makes sand/grass/rock borders jagged and natural
                float noise = valueNoise(posWS.xz * _NoiseScale);
                float perturbedHeight = posWS.y + noise * _HeightNoisePower;

                // Blend weights via smoothstep
                float halfBlend = _BlendRange * 0.5;
                float grassWeight = smoothstep(_SandToGrass - halfBlend, _SandToGrass + halfBlend, perturbedHeight);
                float rockWeight  = smoothstep(_GrassToRock - halfBlend, _GrassToRock + halfBlend, perturbedHeight);

                // Sand -> Grass -> Rock
                half3 albedo = lerp(_SandColor.rgb, _GrassColor.rgb, grassWeight);
                albedo = lerp(albedo, _RockColor.rgb, rockWeight);

                // Subtle color variation to break monotony
                float variation = valueNoise(posWS.xz * _NoiseScale * 3.7) * _ColorVariation;
                albedo += variation;


                float slope = 1.0 - saturate(normalWS.y);
                float slopeRock = smoothstep(_SlopeThreshold, _SlopeThreshold + _SlopeBlend, slope);
                albedo = lerp(albedo, _RockColor.rgb, slopeRock);

                // --- URP Lighting ---
                float4 shadowCoord = TransformWorldToShadowCoord(posWS);
                Light mainLight = GetMainLight(shadowCoord);

                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = albedo * mainLight.color * NdotL * mainLight.shadowAttenuation;

                half3 ambient = SampleSH(normalWS) * albedo;

                return half4(diffuse + ambient, 1.0);
            }
            ENDHLSL
        }

        Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }

    ZWrite On
    ZTest LEqual
    ColorMask 0

    HLSLPROGRAM
    #pragma vertex vert
    #pragma fragment frag

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

    float3 _LightDirection;

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
    };

    Varyings vert(Attributes input)
    {
        Varyings output;
        float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
        float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
        posWS = ApplyShadowBias(posWS, normalWS, _LightDirection);
        output.positionCS = TransformWorldToHClip(posWS);
        return output;
    }

    half4 frag(Varyings input) : SV_Target
    {
        return 0;
    }
    ENDHLSL
}
    }
}