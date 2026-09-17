Shader "hToon/hToon_v2"
{
    Properties
    {
        [Header(Part)]
        [Enum(Auto,0,Face,1,Skin,2,Hair,3,Cloth,4,Metal,5,Eye,6)] _ShadingPart("Shading Part", Float) = 0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 2
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth Test", Float) = 4
        [Toggle] _ZWrite("Depth Write", Float) = 1
        [Toggle] _UseViewClip("Use View Clip", Float) = 0
        _ViewThreshold("View Clip Threshold", Range(-1, 1)) = -0.2

        [Header(Albedo)]
        _DiffuseTex("Diffuse Texture", 2D) = "white" {}
        _DiffuseColor("Diffuse Color", Color) = (1, 1, 1, 1)
        _AlbedoSaturation("Saturation", Range(0, 2)) = 1.05
        _AlbedoContrast("Contrast", Range(0.5, 1.6)) = 1.02

        [Header(Normal)]
        _NormalTex("Normal Texture", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Range(0, 1)) = 0.25

        [Header(Cel)]
        [HDR] _ShadowColor("Shadow Tint", Color) = (0.70, 0.58, 0.72, 1)
        [HDR] _MidtoneColor("Midtone Tint", Color) = (0.96, 0.82, 0.70, 1)
        [HDR] _InkShadowColor("Ink Deep Shadow", Color) = (0.28, 0.22, 0.32, 1)
        [HDR] _WarmBounceColor("Warm Bounce", Color) = (0.72, 0.38, 0.22, 1)
        _ShadowRange("Lit Threshold", Range(0, 1)) = 0.54
        _ShadowSmooth("Lit Smooth", Range(0.001, 0.35)) = 0.028
        _MidRange("Mid Threshold", Range(0, 1)) = 0.34
        _MidSmooth("Mid Smooth", Range(0.001, 0.35)) = 0.05
        _BandSoftness("Inside Band Gradient", Range(0, 1)) = 0.22
        _ReceiveRealtimeShadow("Receive Realtime Shadow", Range(0, 1)) = 0.28
        _FakeShadowSmooth("Realtime Shadow Smooth", Range(0, 0.5)) = 0.14
        _WrapLighting("Skin Wrap", Range(0, 1)) = 0.35
        [HDR] _ScatterColor("Skin Scatter", Color) = (0.92, 0.32, 0.22, 1)
        _ScatterIntensity("Skin Scatter Intensity", Range(0, 1)) = 0.28

        [Header(Ambient)]
        [HDR] _SkyColor("Sky Ambient", Color) = (0.55, 0.62, 0.78, 1)
        [HDR] _GroundColor("Ground Ambient", Color) = (0.42, 0.28, 0.22, 1)
        _AmbientIntensity("Ambient Intensity", Range(0, 1)) = 0.18

        [Header(Face SDF)]
        [Toggle] _UseSDF("Use SDF Face Shadow", Float) = 0
        _ShadowMask("SDF / Shadow Mask", 2D) = "white" {}
        _ShadowTex("Shadow Texture", 2D) = "white" {}

        [Header(AO)]
        _AOTex("AO Texture", 2D) = "white" {}
        _AOIntensity("AO Intensity", Range(0, 1)) = 0.35
        [Toggle] _UseVertexColorAO("Vertex Color G as AO", Float) = 0

        [Header(Hair)]
        [HDR] _HairSpecColor("Hair Spec Color", Color) = (1.0, 0.90, 0.78, 1)
        _HairShift("Hair Shift", Range(-1, 1)) = 0.12
        _HairPrimaryPower("Hair Primary Power", Range(1, 256)) = 64
        _HairPrimaryIntensity("Hair Primary Intensity", Range(0, 4)) = 0.85
        _HairSecondaryPower("Hair Secondary Power", Range(1, 128)) = 12
        _HairSecondaryIntensity("Hair Secondary Intensity", Range(0, 2)) = 0.28

        [Header(Specular Silk Metal)]
        _MatcapTex("Matcap Texture", 2D) = "white" {}
        _SpecularMask("Specular Mask (G)", 2D) = "black" {}
        _SpecularIntensity("Specular Intensity", Range(0, 2)) = 0.65
        _NonMetalIntensity("Cloth Spec Intensity", Range(0, 1)) = 0.18
        _NonMetalPower("Cloth Spec Power", Range(0.5, 32)) = 10
        _SilkAnisotropy("Silk Anisotropy", Range(0, 1)) = 0.6
        _SilkIntensity("Silk Intensity", Range(0, 2)) = 0.4
        _MetalIntensity("Metal Matcap Intensity", Range(0, 2)) = 0.7
        _GoldIntensity("Gold Intensity", Range(0, 2)) = 0.85
        [HDR] _GoldColor("Gold / Lacquer Color", Color) = (0.93, 0.74, 0.36, 1)
        _SpecInShadow("Hide Spec In Shadow", Range(0, 1)) = 0.82

        [Header(Rim)]
        [HDR] _RimColor("Bright Rim Color", Color) = (1.0, 0.93, 0.82, 1)
        _RimPower("Rim Power", Range(0.5, 10)) = 3.4
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.38
        _RimIntensity("Bright Rim Intensity", Range(0, 2)) = 0.48
        _RimLightMask("Rim Follows Light", Range(0, 1)) = 1
        [HDR] _DarkRimColor("Ink Rim Color", Color) = (0.10, 0.06, 0.09, 1)
        _DarkRimIntensity("Ink Rim Intensity", Range(0, 2)) = 0.32

        [Header(Pattern Embroidery)]
        _PatternTex("Pattern Texture", 2D) = "black" {}
        [HDR] _PatternColor("Pattern Color", Color) = (0.78, 0.58, 0.24, 1)
        _PatternIntensity("Pattern Intensity", Range(0, 1)) = 0
        _PatternInShadow("Hide Pattern In Shadow", Range(0, 1)) = 0.45

        [Header(Inner Line)]
        _InnerLineTex("Inner Line Texture", 2D) = "white" {}
        _InnerLineColor("Inner Line Color", Color) = (0.16, 0.10, 0.10, 1)
        _InnerLineIntensity("Inner Line Intensity", Range(0, 1)) = 0
        _InnerLineThreshold("Inner Line Threshold", Range(0, 1)) = 0.45

        [Header(Emission)]
        _EmissionMask("Emission Mask", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity("Emission Intensity", Range(0, 10)) = 0

        [Header(Outline)]
        _OutlineMask("Outline Mask", 2D) = "white" {}
        _OutlineWidth("Outline Width", Range(0, 8)) = 1.05
        _OutlineColor("Outline Color", Color) = (0.08, 0.04, 0.05, 1)
        _OutlineAlbedoMix("Outline Albedo Mix", Range(0, 1)) = 0.32
        _OutlineFadeStart("Outline Fade Start", Float) = 0
        _OutlineFadeEnd("Outline Fade End", Float) = 80
        _OutlineZOffset("Outline Z Offset", Range(-10, 10)) = 0
        [Toggle] _UseVertexColorOutline("Vertex Color R as Width", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_CullMode]
            ZTest [_ZTest]
            ZWrite [_ZWrite]
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #include "./Library/pass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _ALPHATEST_ON
            #include "./Library/outline.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #define HTOON_V2_SHADOW_CASTER 1
            #include "./Library/shadow.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #define HTOON_V2_DEPTH_ONLY 1
            #include "./Library/shadow.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
