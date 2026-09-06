Shader "hToon/SH_hToon"
{
    Properties
    {
        [Header(Stencil)]
        [Enum(UnityEngine.Rendering.CullMode)]_CullMode("Cull Mode", Float) = 0.0
        _StencilRef ("Stencil Reference (EyeBrow: 1)", Float) = 0
        _ViewThreshold ("Stencil View Threshold", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPassOp ("Stencil Pass Operation", Float) = 0

        [Space(20)]
        [Header(Diffuse)]
        _DiffuseTex ("Diffuse Texture", 2D) = "white" {}
        _DiffuseColor ("Diffuse Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Header(Normal)]
        _NormalTex ("Normal Texture", 2D) = "bump" {}
        _NormalIntensity ("Normal Intensity", Range(0,1)) = 0.5
        [Toggle] _UseSDF("Use SDF Shadow", Float) = 0
        _ShadowTex ("Shadow Texture", 2D) = "white" {}
        _ShadowMask ("Shadow Mask", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
        _ShadowRange ("Shadow Range", Range(0, 1)) = 0.5
        _ShadowSmooth ("Shadow Smooth", Range(0.001, 1)) = 0.5
        _FakeShadowSmooth ("Fake Shadow Smooth", Float) = 0
        [Header(AO)]
        _AOTex ("AO Texture", 2D) = "white" {}
        _AOIntensity ("AO Intensity", Range(0, 1)) = 1
        [Header(Specular)]
        _MatcapTex ("Matcap Texture", 2D) = "white" {}
        _nonMetalSmooth ("Non Metal Smooth", Range(0.001, 1)) = 0.5
        _nonMetalIntensity ("Non Metal Intensity", Range(0, 1)) = 0.5
        _MetalIntensity ("Metal Intensity", Range(0, 1)) = 0.5
        _SpecularMask ("Specular Mask", 2D) = "black" {}
        _SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.5
        [Header(Emission)]
        _EmissionMask ("Emission Mask", 2D) = "black" {}
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1
        [Header(RimLight)]
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0, 10)) = 2
        [Header(Outline)]
        _OutlineMask ("OutlineMask", 2D) = "white" {}
        _OutlineWidth ("OutlineWidth", Range(0,1)) = 0
        _OutlineColor ("OutlineColor", color) = (0,0,0,1)
        _OutlineFadeStart ("OutlineFadeStartDist", float) = 0
        _OutlineFadeEnd ("OutlineFadeEndDist", float) = 0
    }
    SubShader
    {
        Tags {"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}

        Pass
        {
            Name "Forward"
            Tags {"LightMode" = "UniversalForward"}

            Cull [_CullMode]

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPassOp]
            }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #include "./Library/pass.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha 
            ZWrite Off
            Cull [_CullMode]

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "./Library/outline.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode" = "ShadowCaster"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode" = "DepthOnly"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
