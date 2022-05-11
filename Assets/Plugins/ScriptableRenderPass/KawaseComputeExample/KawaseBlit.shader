Shader "YourFinalBlit"
{
    HLSLINCLUDE
    #pragma target 5.0
    #pragma editor_sync_compilation

    #include "kBlit.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
    ENDHLSL

    Properties 
    {
        _FinalBlur ("_FinalBlur", 2D) = "clear" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "Kawase Blit Color"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            // #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma exclude_renderers gles
            #pragma vertex Vert
            #pragma fragment FragmentColorOnly

            ENDHLSL
        }
    }
}