#ifndef HTOON_V2_SHADOW_INCLUDED
#define HTOON_V2_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "./input.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

float4 GetShadowHClip(float3 positionOS, float3 normalOS)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
        float3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
        float3 lightDirectionWS = _LightDirection;
    #endif
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    #if UNITY_REVERSED_Z
        positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
        positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif
    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    output.uv = TRANSFORM_TEX(input.uv, _DiffuseTex);
    output.positionCS = GetShadowHClip(input.positionOS.xyz, input.normalOS);
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    #if defined(_ALPHATEST_ON)
        float alpha = SAMPLE_TEXTURE2D(_DiffuseTex, sampler_DiffuseTex, input.uv).a * _DiffuseColor.a;
        clip(alpha - _Cutoff);
    #endif
    return 0;
}

Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output;
    output.uv = TRANSFORM_TEX(input.uv, _DiffuseTex);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    #if defined(_ALPHATEST_ON)
        float alpha = SAMPLE_TEXTURE2D(_DiffuseTex, sampler_DiffuseTex, input.uv).a * _DiffuseColor.a;
        clip(alpha - _Cutoff);
    #endif
    return 0;
}

#endif
