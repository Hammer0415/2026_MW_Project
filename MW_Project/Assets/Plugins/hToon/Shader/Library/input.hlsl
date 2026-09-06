#ifndef INPUT_INCLUDED
#define INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


// Default
TEXTURE2D(_DiffuseTex);
TEXTURE2D(_NormalTex);
TEXTURE2D(_RampTex);
TEXTURE2D(_ShadowTex);
TEXTURE2D(_ShadowMask);
TEXTURE2D(_AOTex);
TEXTURE2D(_OutlineMask);
TEXTURE2D(_MatcapTex);
TEXTURE2D(_EmissionMask);
TEXTURE2D(_SpecularMask);
SAMPLER(sampler_DiffuseTex);
SAMPLER(sampler_NormalTex);
SAMPLER(sampler_RampTex);
SAMPLER(sampler_ShadowTex);
SAMPLER(sampler_ShadowMask);
SAMPLER(sampler_AOTex);
SAMPLER(sampler_OutlineMask);
SAMPLER(sampler_MatcapTex);
SAMPLER(sampler_EmissionMask);
SAMPLER(sampler_SpecularMask);

// Raindrop
TEXTURE2D(_RainTexture);
SAMPLER(sampler_RainTexture);

CBUFFER_START(UnityPerMaterial)

// Stencil
float _StencilRef;
float _ViewThreshold;

// Diffuse
float4 _DiffuseTex_ST;
float4 _DiffuseColor;

// Normal
float4 _NormalTex_ST;
float _NormalIntensity;

// Shadow
float _UseSDF;
float4 _ShadowTex_ST;
float4 _ShadowMask_ST;
float4 _ShadowColor;
float _ShadowRange;
float _ShadowSmooth;
float _FakeShadowSmooth;

// AO
float4 _AOTex_ST;
float _AOIntensity;

// Specular
float4 _MatcapTex_ST;
float _MetalIntensity;
float _nonMetalIntensity;
float _nonMetalSmooth;
float4 _SpecularMask_ST;
float _SpecularIntensity;

// Emission
float _EmissionMask_ST;
float4 _EmissionColor;
float _EmissionIntensity;

// Rimlight
float4 _RimColor;
float _RimPower;

// Outline
float4 _OutlineMask_ST;
float _OutlineWidth;
float4 _OutlineColor;
float _OutlineFadeStart;
float _OutlineFadeEnd;

CBUFFER_END

#endif