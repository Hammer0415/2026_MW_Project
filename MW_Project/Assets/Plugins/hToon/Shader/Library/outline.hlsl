#ifndef PASS_INCLUDED
#define PASS_INCLUDED

#include "./Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

Varyings vert(Attributes input)
{
    Varyings output;

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float4 positionCS = TransformWorldToHClip(positionWS);

    float3 normalCS = TransformWorldToHClipDir(normalWS);
    
    float2 offset = normalize(normalCS.xy) * _OutlineWidth * positionCS.w * 0.01;
    
    float dist = length(GetCameraPositionWS() - positionWS);
    float fade = 1.0 - smoothstep(_OutlineFadeStart, _OutlineFadeEnd, dist);
    
    output.positionCS = positionCS;
    output.positionCS.xy += offset * fade;
    output.uv = input.uv;

    return output;
}

float4 frag(Varyings input) : SV_Target
{
    half outlineMask = SAMPLE_TEXTURE2D(_OutlineMask, sampler_OutlineMask, input.uv).a;
    if (outlineMask < 0.5) discard;
    return _OutlineColor;
}

#endif