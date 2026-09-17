#ifndef HTOON_V2_PASS_INCLUDED
#define HTOON_V2_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "./input.hlsl"
#include "./lighting.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 tangentWS : TEXCOORD2;
    float3 bitangentWS : TEXCOORD3;
    float4 color : COLOR;
    float2 uv : TEXCOORD4;
};

Varyings vert(Attributes input)
{
    Varyings output;
    VertexPositionInputs positionInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.positionCS = positionInput.positionCS;
    output.positionWS = positionInput.positionWS;
    output.normalWS = normalInput.normalWS;
    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
    output.color = input.color;
    output.uv = TRANSFORM_TEX(input.uv, _DiffuseTex);
    return output;
}

float4 frag(Varyings input) : SV_Target
{
    float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    if (_UseViewClip > 0.5)
    {
        float3 characterForward = TransformObjectToWorldDir(float3(0.0, 0.0, 1.0));
        clip(dot(characterForward, viewWS) - _ViewThreshold);
    }

    float4 diffuseSample = SAMPLE_TEXTURE2D(_DiffuseTex, sampler_DiffuseTex, input.uv) * _DiffuseColor;
    #if defined(_ALPHATEST_ON)
    clip(diffuseSample.a - _Cutoff);
    #endif

    float3 albedo = AdjustAlbedo(diffuseSample.rgb);
    float3 normalWS = normalize(input.normalWS);

    if (_NormalIntensity > 0.001 && length(input.tangentWS) > 0.05)
    {
        float3 bump = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv), _NormalIntensity);
        float3x3 tbn = float3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalWS);
        normalWS = TransformTangentToWorld(bump, tbn, true);
        normalWS = NormalizeNormalPerPixel(normalWS);
    }

    float specMaskG = SAMPLE_TEXTURE2D(_SpecularMask, sampler_SpecularMask, input.uv).g;
    MingPart part = ResolvePart(specMaskG);

    if (part.eye > 0.5)
    {
        float3 color = MixFog(diffuseSample.rgb, ComputeFogFactor(input.positionCS.z));
        return float4(saturate(color), diffuseSample.a);
    }

    float aoTex = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, input.uv).r;
    float ao = lerp(1.0, aoTex, _AOIntensity);
    ao *= VertexChannelOrOne(input.color, input.color.g, _UseVertexColorAO);

    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    Light mainLight = GetMainLight(shadowCoord);
    float3 lightDir = normalize(mainLight.direction);
    float realtimeShadow = SampleRealtimeShadow(shadowCoord);

    float wrap = lerp(0.0, _WrapLighting, saturate(part.skin + part.face));
    float lightValue = WrapLambert(normalWS, lightDir, wrap) * ao;
    lightValue = lerp(lightValue, lightValue * lerp(0.62, 1.0, realtimeShadow), saturate(_ReceiveRealtimeShadow));

    float sdfLit = EvaluateFaceSDF(input.uv, lightDir);
    float sdfShadow = lerp(1.0, lerp(0.62, 1.0, realtimeShadow), saturate(_ReceiveRealtimeShadow));
    float useFaceSDF = saturate(part.face * _UseSDF);
    lightValue = lerp(lightValue, sdfLit * sdfShadow * ao, useFaceSDF);

    float3 additionalLightColor = 0.0;
    float additionalAttenuation = 0.0;

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = viewWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light extra = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
        float extraNdotL = saturate(dot(normalWS, extra.direction));
        additionalLightColor += extra.color * extra.distanceAttenuation * extraNdotL;
        additionalAttenuation += extra.distanceAttenuation * extra.shadowAttenuation;
    LIGHT_LOOP_END
    #endif
    lightValue = saturate(lightValue + additionalAttenuation * 0.08);

    float litMask;
    float midMask;
    float deepMask;
    EvaluateCelBands(lightValue, litMask, midMask, deepMask);

    float3 shadowedAlbedo = ComposeMingShadow(albedo, lightValue, litMask, midMask, deepMask);
    shadowedAlbedo = EvaluatePattern(input.uv, shadowedAlbedo, litMask);
    shadowedAlbedo += EvaluateSkinScatter(lightValue, litMask, midMask, saturate(part.skin + part.face));
    shadowedAlbedo += EvaluateHemisphere(normalWS, albedo);

    float3 mixLight = CompressLightColor(mainLight.color) + CompressLightColor(saturate(additionalLightColor)) * 0.4;
    float3 color = shadowedAlbedo * mixLight;

    float3 halfVector = normalize(lightDir + viewWS);
    float ndh = saturate(dot(normalWS, halfVector));
    float specKeep = lerp(1.0, litMask, _SpecInShadow);

    float3 spec = EvaluateClothSpec(ndh, part.cloth);
    spec += EvaluateSilkSpecular(normalWS, input.tangentWS, lightDir, viewWS, part.cloth);
    spec += EvaluateMetalMatcap(normalWS, ndh, part.metal);
    spec += EvaluateHairSpecular(normalWS, input.bitangentWS, lightDir, viewWS, part.hair, litMask);
    spec *= _SpecularIntensity * specKeep;

    float3 rim = EvaluateRim(normalWS, viewWS, lightDir, litMask);
    float3 innerLine = EvaluateInnerLine(input.uv, albedo);
    float3 emission = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, input.uv).r * _EmissionColor.rgb * _EmissionIntensity;

    color = color * innerLine + spec + rim + emission;
    color = MixFog(color, ComputeFogFactor(input.positionCS.z));
    return float4(saturate(color), diffuseSample.a);
}

#endif
