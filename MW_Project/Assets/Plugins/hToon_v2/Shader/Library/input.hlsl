#ifndef HTOON_V2_INPUT_INCLUDED
#define HTOON_V2_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_DiffuseTex);
TEXTURE2D(_NormalTex);
TEXTURE2D(_ShadowTex);
TEXTURE2D(_ShadowMask);
TEXTURE2D(_AOTex);
TEXTURE2D(_OutlineMask);
TEXTURE2D(_InnerLineTex);
TEXTURE2D(_MatcapTex);
TEXTURE2D(_EmissionMask);
TEXTURE2D(_SpecularMask);
TEXTURE2D(_PatternTex);

SAMPLER(sampler_DiffuseTex);
SAMPLER(sampler_NormalTex);
SAMPLER(sampler_ShadowTex);
SAMPLER(sampler_ShadowMask);
SAMPLER(sampler_AOTex);
SAMPLER(sampler_OutlineMask);
SAMPLER(sampler_InnerLineTex);
SAMPLER(sampler_MatcapTex);
SAMPLER(sampler_EmissionMask);
SAMPLER(sampler_SpecularMask);
SAMPLER(sampler_PatternTex);

CBUFFER_START(UnityPerMaterial)
float _ShadingPart;
float _Cutoff;
float _ZTest;
float _ZWrite;
float _UseViewClip;
float _ViewThreshold;

float4 _DiffuseTex_ST;
float4 _DiffuseColor;
float _AlbedoSaturation;
float _AlbedoContrast;

float4 _NormalTex_ST;
float _NormalIntensity;

float4 _ShadowColor;
float4 _MidtoneColor;
float4 _InkShadowColor;
float4 _WarmBounceColor;
float _ShadowRange;
float _ShadowSmooth;
float _MidRange;
float _MidSmooth;
float _BandSoftness;
float _FakeShadowSmooth;
float _ReceiveRealtimeShadow;
float _WrapLighting;
float4 _ScatterColor;
float _ScatterIntensity;

float4 _SkyColor;
float4 _GroundColor;
float _AmbientIntensity;

float _UseSDF;
float4 _ShadowTex_ST;
float4 _ShadowMask_ST;

float4 _AOTex_ST;
float _AOIntensity;
float _UseVertexColorAO;

float4 _HairSpecColor;
float _HairShift;
float _HairPrimaryPower;
float _HairPrimaryIntensity;
float _HairSecondaryPower;
float _HairSecondaryIntensity;

float4 _MatcapTex_ST;
float _MetalIntensity;
float _NonMetalIntensity;
float _NonMetalPower;
float4 _SpecularMask_ST;
float _SpecularIntensity;
float _SilkAnisotropy;
float _SilkIntensity;
float _GoldIntensity;
float4 _GoldColor;
float _SpecInShadow;

float4 _RimColor;
float _RimPower;
float _RimThreshold;
float _RimLightMask;
float _RimIntensity;
float _DarkRimIntensity;
float4 _DarkRimColor;

float4 _PatternTex_ST;
float4 _PatternColor;
float _PatternIntensity;
float _PatternInShadow;

float4 _InnerLineTex_ST;
float4 _InnerLineColor;
float _InnerLineIntensity;
float _InnerLineThreshold;

float4 _EmissionMask_ST;
float4 _EmissionColor;
float _EmissionIntensity;

float4 _OutlineMask_ST;
float _OutlineWidth;
float4 _OutlineColor;
float _OutlineAlbedoMix;
float _OutlineFadeStart;
float _OutlineFadeEnd;
float _OutlineZOffset;
float _UseVertexColorOutline;
CBUFFER_END

float VertexChannelOrOne(float4 color, float channel, float enabled)
{
    float painted = step(0.001, color.r + color.g + color.b);
    float value = lerp(1.0, saturate(channel), painted);
    return lerp(1.0, value, enabled);
}

float SoftStep(float value, float threshold, float smoothness)
{
    float width = max(smoothness, 0.0001);
    return smoothstep(threshold - width, threshold + width, value);
}

float3 AdjustAlbedo(float3 albedo)
{
    float luma = dot(albedo, float3(0.2126, 0.7152, 0.0722));
    float3 saturated = luma + (albedo - luma) * _AlbedoSaturation;
    float3 contrasted = (saturated - 0.5) * _AlbedoContrast + 0.5;
    return saturate(contrasted);
}

float3 CompressLightColor(float3 lightColor)
{
    float lum = max(dot(lightColor, float3(0.2126, 0.7152, 0.0722)), 1e-4);
    float3 tint = lightColor / lum;
    float gain = 0.48 + saturate(lum * 0.32);
    return tint * gain;
}

#endif
