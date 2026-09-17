#ifndef HTOON_V2_OUTLINE_INCLUDED
#define HTOON_V2_OUTLINE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "./input.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 color : COLOR;
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
    float4 normalHCS = mul(UNITY_MATRIX_VP, float4(normalWS, 0.0));
    float2 offsetDir = normalize(normalHCS.xy + 1e-6);

    float vertexWidth = VertexChannelOrOne(input.color, input.color.r, _UseVertexColorOutline);
    float dist = length(GetCameraPositionWS() - positionWS);
    float fade = 1.0;
    if (_OutlineFadeEnd > _OutlineFadeStart)
    {
        fade = 1.0 - smoothstep(_OutlineFadeStart, _OutlineFadeEnd, dist);
    }

    float objectScale = max(length(TransformObjectToWorldDir(float3(1.0, 0.0, 0.0))), 0.0001);
    float width = _OutlineWidth * vertexWidth * fade / objectScale;

    output.positionCS = positionCS;
    output.positionCS.xy += offsetDir * width * positionCS.w * 0.0036;
    output.positionCS.z += _OutlineZOffset * 0.0001 * output.positionCS.w;
    output.uv = TRANSFORM_TEX(input.uv, _DiffuseTex);
    return output;
}

float4 frag(Varyings input) : SV_Target
{
    float4 diffuse = SAMPLE_TEXTURE2D(_DiffuseTex, sampler_DiffuseTex, input.uv) * _DiffuseColor;
    #if defined(_ALPHATEST_ON)
    clip(diffuse.a - _Cutoff);
    #endif

    float outlineMask = SAMPLE_TEXTURE2D(_OutlineMask, sampler_OutlineMask, input.uv).a;
    clip(outlineMask - 0.5);

    float3 outline = lerp(_OutlineColor.rgb, diffuse.rgb * _OutlineColor.rgb, saturate(_OutlineAlbedoMix));
    return float4(outline, _OutlineColor.a);
}

#endif
