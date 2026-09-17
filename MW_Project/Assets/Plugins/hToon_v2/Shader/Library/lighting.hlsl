#ifndef HTOON_V2_LIGHTING_INCLUDED
#define HTOON_V2_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "./input.hlsl"

struct MingPart
{
    float face;
    float skin;
    float hair;
    float cloth;
    float metal;
    float eye;
};

MingPart ResolvePart(float specMaskG)
{
    MingPart part;
    part.face = 0.0;
    part.skin = 0.0;
    part.hair = 0.0;
    part.cloth = 0.0;
    part.metal = 0.0;
    part.eye = 0.0;

    float id = _ShadingPart;
    if (id > 5.5)
    {
        part.eye = 1.0;
    }
    else if (id > 4.5)
    {
        part.metal = 1.0;
    }
    else if (id > 3.5)
    {
        part.cloth = 1.0;
    }
    else if (id > 2.5)
    {
        part.hair = 1.0;
    }
    else if (id > 1.5)
    {
        part.skin = 1.0;
    }
    else if (id > 0.5)
    {
        part.face = 1.0;
        part.skin = 1.0;
    }
    else
    {
        part.metal = step(0.9, specMaskG);
        part.cloth = saturate(step(0.1, specMaskG) - part.metal);
        part.face = _UseSDF;
        part.skin = part.face;
    }

    return part;
}

float SampleRealtimeShadow(float4 shadowCoord)
{
    #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        float raw = MainLightRealtimeShadow(shadowCoord);
        float shaped = SoftStep(raw, 0.42, max(_FakeShadowSmooth, 0.02));
        return lerp(1.0, shaped, saturate(_ReceiveRealtimeShadow));
    #else
        return 1.0;
    #endif
}

float WrapLambert(float3 normalWS, float3 lightDirWS, float wrap)
{
    float ndotl = dot(normalWS, lightDirWS);
    return saturate(ndotl * (1.0 - wrap) + wrap);
}

float EvaluateFaceSDF(float2 uv, float3 lightDirWS)
{
    float3 upWS = normalize(TransformObjectToWorldDir(float3(0.0, 1.0, 0.0)));
    float3 frontWS = TransformObjectToWorldDir(float3(0.0, 0.0, 1.0));
    frontWS = normalize(frontWS - upWS * dot(frontWS, upWS));
    float3 rightWS = normalize(cross(upWS, frontWS));

    float3 toLightH = lightDirWS - upWS * dot(lightDirWS, upWS);
    float toLightLen = length(toLightH);
    if (toLightLen < 1e-4)
    {
        return 1.0;
    }
    toLightH /= toLightLen;

    float foL = clamp(dot(frontWS, toLightH), -1.0, 1.0);
    float roL = dot(rightWS, toLightH);
    float2 sdfUV = (roL >= 0.0) ? uv : float2(1.0 - uv.x, uv.y);
    float sdf = SAMPLE_TEXTURE2D(_ShadowMask, sampler_ShadowMask, sdfUV).r;
    float threshold = acos(foL) / 3.14159265;
    return SoftStep(sdf, threshold, max(_ShadowSmooth, 0.02));
}

void EvaluateCelBands(float lightValue, out float litMask, out float midMask, out float deepMask)
{
    litMask = SoftStep(lightValue, _ShadowRange, _ShadowSmooth);
    float mid = SoftStep(lightValue, _MidRange, _MidSmooth);
    midMask = saturate(mid - litMask);
    deepMask = saturate(1.0 - mid);
}

float3 ComposeMingShadow(float3 albedo, float lightValue, float litMask, float midMask, float deepMask)
{
    float band = saturate(lightValue);
    float inner = lerp(1.0, 0.82 + band * 0.18, _BandSoftness);

    float3 litColor = albedo * inner;
    float3 midColor = albedo * _MidtoneColor.rgb * (0.88 + band * 0.12);
    midColor += _WarmBounceColor.rgb * albedo * 0.16;
    float3 deepColor = albedo * _InkShadowColor.rgb * _ShadowColor.rgb;
    deepColor = lerp(dot(deepColor, float3(0.2126, 0.7152, 0.0722)).xxx, deepColor, 0.78);

    float3 color = litColor * litMask + midColor * midMask + deepColor * deepMask;
    color += _WarmBounceColor.rgb * midMask * 0.10;
    return color;
}

float3 EvaluateHemisphere(float3 normalWS, float3 albedo)
{
    float ground = saturate(normalWS.y * 0.5 + 0.5);
    float3 ambient = lerp(_GroundColor.rgb, _SkyColor.rgb, ground);
    return albedo * ambient * _AmbientIntensity;
}

float3 EvaluateSkinScatter(float lightValue, float litMask, float midMask, float skinMask)
{
    float terminator = saturate(1.0 - abs(lightValue - _ShadowRange) * 4.0);
    float scatter = terminator * (midMask + (1.0 - litMask) * 0.25);
    return _ScatterColor.rgb * scatter * _ScatterIntensity * skinMask;
}

float3 EvaluatePattern(float2 uv, float3 baseColor, float litMask)
{
    float4 pattern = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, uv * _PatternTex_ST.xy + _PatternTex_ST.zw);
    float mask = pattern.a * _PatternIntensity;
    float keep = lerp(1.0, litMask, _PatternInShadow);
    float3 embroider = pattern.rgb * _PatternColor.rgb;
    return lerp(baseColor, saturate(baseColor + embroider * 0.7), mask * keep);
}

float KajiyaKay(float3 tangentWS, float3 lightDirWS, float3 viewWS, float power)
{
    float tdl = dot(tangentWS, lightDirWS);
    float tdv = dot(tangentWS, viewWS);
    float dir = saturate(sqrt(saturate(1.0 - tdl * tdl)) * sqrt(saturate(1.0 - tdv * tdv)) - tdl * tdv);
    return pow(dir, max(power, 1.0));
}

float3 EvaluateHairSpecular(float3 normalWS, float3 bitangentWS, float3 lightDirWS, float3 viewWS, float hairMask, float litMask)
{
    float3 flowBase = bitangentWS;
    if (length(flowBase) < 0.05)
    {
        flowBase = cross(normalWS, float3(0.0, 1.0, 0.0));
    }
    float3 flow = normalize(flowBase + normalWS * _HairShift);
    float3 secondaryFlow = normalize(flowBase - normalWS * _HairShift * 0.6);
    float3 primary = KajiyaKay(flow, lightDirWS, viewWS, _HairPrimaryPower) * _HairPrimaryIntensity;
    float3 secondary = KajiyaKay(secondaryFlow, lightDirWS, viewWS, _HairSecondaryPower) * _HairSecondaryIntensity;
    float shadowKeep = lerp(1.0, litMask, _SpecInShadow);
    return (primary + secondary) * _HairSpecColor.rgb * hairMask * shadowKeep;
}

float3 EvaluateSilkSpecular(float3 normalWS, float3 tangentWS, float3 lightDirWS, float3 viewWS, float mask)
{
    float3 t = normalize(tangentWS + normalWS * _SilkAnisotropy);
    float3 halfVector = normalize(lightDirWS + viewWS);
    float tDotH = dot(t, halfVector);
    float sinTH = sqrt(saturate(1.0 - tDotH * tDotH));
    float dirAtten = saturate(4.0 * abs(tDotH) + 0.05);
    float spec = pow(sinTH, lerp(18.0, 56.0, saturate(_SilkAnisotropy))) * dirAtten;
    return spec * mask * _SilkIntensity * _GoldColor.rgb;
}

float3 EvaluateMetalMatcap(float3 normalWS, float ndh, float metalMask)
{
    float3 normalVS = TransformWorldToViewNormal(normalWS, true);
    float2 matcapUV = normalVS.xy * 0.5 + 0.5;
    float matcap = SAMPLE_TEXTURE2D(_MatcapTex, sampler_MatcapTex, matcapUV).r;
    float lacquer = pow(ndh, 22.0) * 0.38;
    return (matcap * _MetalIntensity + lacquer) * metalMask * _GoldColor.rgb * _GoldIntensity;
}

float3 EvaluateClothSpec(float ndh, float mask)
{
    return pow(ndh, max(_NonMetalPower, 0.01)) * mask * _NonMetalIntensity;
}

float3 EvaluateRim(float3 normalWS, float3 viewWS, float3 lightDirWS, float litMask)
{
    float fresnel = 1.0 - saturate(dot(normalWS, viewWS));
    float rim = pow(saturate(fresnel), _RimPower);
    float lightFacing = saturate(dot(normalWS, lightDirWS) * 0.5 + 0.5);
    float rimMask = SoftStep(rim, _RimThreshold, 0.07);
    float3 brightRim = _RimColor.rgb * rimMask * lightFacing * _RimIntensity * lerp(1.0, litMask, _RimLightMask);
    float3 inkRim = _DarkRimColor.rgb * pow(fresnel, _RimPower + 1.4) * (1.0 - lightFacing) * _DarkRimIntensity;
    return brightRim + inkRim;
}

float3 EvaluateInnerLine(float2 uv, float3 albedo)
{
    float lineSample = SAMPLE_TEXTURE2D(_InnerLineTex, sampler_InnerLineTex, uv).r;
    float lineMask = 1.0 - SoftStep(lineSample, _InnerLineThreshold, 0.04);
    return lerp(float3(1.0, 1.0, 1.0), _InnerLineColor.rgb * albedo, lineMask * _InnerLineIntensity);
}

#endif
