#ifndef PASS_INCLUDED
#define PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#include "./Input.hlsl"

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
    float3 positionVS : TEXCOORD1;
    float4 positionNDC : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float3 tangentWS : TEXCOORD4;
    float3 bitangentWS : TEXCOORD5;
    float4 color : COLOR;
    float2 uv : TEXCOORD6;

    float4 shadowCoord : TEXCOORD7;
};

Varyings vert(Attributes input)
{
    Varyings output;

    VertexPositionInputs positionInput = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInput.positionCS;
    output.positionWS = positionInput.positionWS;
    output.positionVS = positionInput.positionVS;
    output.positionNDC = positionInput.positionNDC;
    output.normalWS = normalInput.normalWS;
    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
    output.color = input.color;
    output.uv = input.uv;

    output.shadowCoord = GetShadowCoord(positionInput);

    return output;
}

float4 frag(Varyings input) : SV_TARGET
{
    // eyebrow
    float3 characterForward = TransformObjectToWorldDir(float3(0, 0, 1));
    float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
    
    float viewDot = dot(characterForward, viewDir);

    if (_StencilRef == 1.0)
    {
        clip(viewDot - _ViewThreshold); 
    }

    // Diffuse
    float4 diffuse = SAMPLE_TEXTURE2D(_DiffuseTex, sampler_DiffuseTex, input.uv) * _DiffuseColor;

    // Normal
    float3 bump = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv), _NormalIntensity);
    float3x3 tangent = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
    float3 normalWS = TransformTangentToWorld(bump, tangent, true);

    // Shadow
    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    float shadowMask = SAMPLE_TEXTURE2D(_ShadowMask, sampler_ShadowMask, input.uv).r;

    // AO
    float ao = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, input.uv).a;
    ao = lerp(1.0, ao, _AOIntensity);

    // Main Light
    Light mainLight = GetMainLight(input.shadowCoord);
    float rawAttenuation = MainLightRealtimeShadow(input.shadowCoord);
    float smoothAttenuation = smoothstep(0.5 - _FakeShadowSmooth, 0.5 + _FakeShadowSmooth, rawAttenuation);
    float mainLightHalfLambert = (0.5 * dot(normalWS, normalize(mainLight.direction)) + 0.5);
    float lightIntensity = mainLightHalfLambert * smoothAttenuation;
    float shadowStep = smoothstep(_ShadowRange - _ShadowSmooth, _ShadowRange + _ShadowSmooth, lightIntensity);
    float mainLightShadow = 1.0 - shadowStep;

    // Additional Light
    float3 additionalLightColor = float3(0, 0, 0);
    float additionalLightAttenuation = 0.0;
    InputData inputData = (InputData) 0;
    inputData.positionWS = input.positionWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS.xy);
    #ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
    Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
    additionalLightColor += additionalLight.color * additionalLight.distanceAttenuation;
    additionalLightAttenuation += additionalLight.shadowAttenuation * additionalLight.distanceAttenuation;
    LIGHT_LOOP_END
    #endif

    // Mix Light
    float mixAttenuation = saturate((length(mainLight.color) * mainLightShadow) + (length(additionalLightColor.rgb) * additionalLightAttenuation) + (1 - shadowMask));
    float3 mixAttenuationColor = (1 - mixAttenuation) * _ShadowColor;
    float3 mixLight = mainLight.color + clamp(additionalLightColor, 0.0, 0.5);
    float3 mixLightLength = length(mixLight.rgb);
    float3 mixLightColor = mixLight * saturate(mixAttenuation + mixAttenuationColor);

    // SDF
    float3 worldForward = TransformObjectToWorldDir(float3(0, 0, -1));
    float3 worldRight = TransformObjectToWorldDir(float3(1, 0, 0));
    float2 lightVector = normalize(mainLight.direction.xz);
    float FoL = dot(normalize(worldForward.xz), lightVector);
    float RoL = dot(normalize(worldRight.xz), lightVector);
    float2 reUV = float2(1.0 - input.uv.x, input.uv.y);
    float sdfData = (RoL >= 0) ? SAMPLE_TEXTURE2D(_ShadowMask, sampler_ShadowMask, input.uv).r : SAMPLE_TEXTURE2D(_ShadowMask, sampler_ShadowMask, reUV).r;
    float sdfGradient = smoothstep(FoL + _ShadowSmooth, FoL - _ShadowSmooth, sdfData);
    float4 sdfShadow = lerp(diffuse * _ShadowColor, diffuse, sdfGradient);
    float3 finalShadow = lerp(saturate(mainLightShadow + (1.0 - ao)), sdfShadow, _UseSDF);

    // BlinnPhong
    float3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    float3 halfVector = normalize(mainLight.direction + viewWS);
    float blinnPhong = saturate(dot(normalWS, halfVector));

    // Specular
    float3 normalVS = TransformWorldToViewNormal(normalWS, true);
    float matCap = SAMPLE_TEXTURE2D(_MatcapTex, sampler_MatcapTex, 0.5 * normalVS.xy + 0.5).r;
    float specularMask = SAMPLE_TEXTURE2D(_SpecularMask, sampler_SpecularMask, input.uv).g;
    float metalRange = step(0.9, specularMask);
    float nonMetalRange = (step(0.1, specularMask) - step(0.9, specularMask));
    float metal = blinnPhong * matCap * metalRange * _MetalIntensity;
    float nonMetal = pow(blinnPhong, _nonMetalSmooth) * nonMetalRange * _nonMetalIntensity;
    float lightMask = saturate(mainLightHalfLambert * smoothAttenuation);
    float specular = lerp(nonMetal, metal, metalRange) * _SpecularIntensity * lightMask;
    
    // Emission
    float emissionMask = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, input.uv).r;
    float emission = emissionMask * _EmissionIntensity;

    // Rimlight
    float3 lightDir = normalize(mainLight.direction);
    float rimDot = 1.0 - saturate(dot(normalWS, viewWS));
    float rimMask = saturate(dot(normalWS, lightDir));
    float rim = pow(rimDot, _RimPower) * rimMask;
    float3 rimLight = rim * _RimColor.rgb;

    // Final
    float3 diffuseColor = lerp(diffuse, diffuse * _ShadowColor, finalShadow) * mixLightColor;
    float3 combinedLight = specular + rimLight;
    float3 final = saturate(diffuseColor + combinedLight);
    final += (emission * _EmissionColor);
    return float4(final, diffuse.a);
}

#endif