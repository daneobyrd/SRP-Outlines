Shader "Hidden/ColorPyramidPS"
{
//    Properties
//    {
//        _Source ("Texture2DArray Source", 2dArray) = "" {}
//        _SrcScaleBias ("Source Scale Bias", Vector) = 
//        _SrcUvLimits ("Source UV Limits", Vector) =  // {xy: max uv, zw: offset of blur for 1 texel }
//        uniform half _SourceMip;
//
//    }
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalRenderPipeline" "LightMode" = "Outline" }

        // 0: Bilinear tri
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma editor_sync_compilation
                #pragma target 4.5
                #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
                #pragma vertex vert
                #pragma fragment Frag
                #define DISABLE_TEXTURE2D_X_ARRAY 1
                #include "ColorPyramidPS.hlsl"
            ENDHLSL
        }

        // 1: no tex array
        Pass
        {
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma editor_sync_compilation
                #pragma target 4.5
                #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
                #pragma vertex vert
                #pragma fragment Frag
                #include "ColorPyramidPS.hlsl"
        ENDHLSL
        }

    }
        Fallback Off
}
