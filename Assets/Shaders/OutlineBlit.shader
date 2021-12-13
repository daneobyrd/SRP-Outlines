Shader "OutlineBlit"
{
    Properties
    {
        _SourceTex ("Source Texture", Any) = "white" { }
        _OuterLines("Outer Lines", Color) = (.25, .5, .5, 1)
        _InnerLines("Inner Lines", Color) = (.5, .25, .25, 1)
    }
    SubShader
    {
        Tags {"RenderPipeline" = "UniversalPipeline"}
        LOD 100
        Pass
        {
            Name "OutlineBlit"
            ZWrite Off Cull Off
            
            HLSLPROGRAM
            // #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma exclude_renderers gles
            #pragma target 5.0
            #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Assets/Resources/Compute/Reference/Direct3D Rendering/LerpUtils.hlsl"
            
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            // float4 _CameraOpaqueTexture_ST;

            TEXTURE2D_X(_CameraColorAttachmentA);
            TEXTURE2D_X(_CameraColorAttachmentB);
            TEXTURE2D_X(_CameraColorAttachmentC);
            TEXTURE2D_X(_CameraColorAttachmentD);

            float _OuterThreshold;
            float _InnerThreshold;
            float depthPush;

            CBUFFER_START(UnityPerMaterial)
            TEXTURE2D_X(_SourceTex);
            SAMPLER(sampler_SourceTex);
            float4 _SourceTex_ST;
            
            float4 _OuterLines;
            float4 _InnerLines;
            CBUFFER_END
            
            TEXTURE2D_X(_OutlineOpaque);
            TEXTURE2D_X(_OutlineDepth);

            TEXTURE2D_X(_BlurredUpsampleTex);
            TEXTURE2D_X(_OutlineTexture);
            SAMPLER(sampler_OutlineTexture);
            
            // Combine textures in blit shader
            
            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.uv;
                float4 outlineTex = SAMPLE_TEXTURE2D_X(_OutlineTexture, sampler_OutlineTexture, uv);

                float4 outlineMask;
                outlineMask.x = step( _OuterThreshold, outlineTex.x );
                outlineMask.y = step( _InnerThreshold, outlineTex.y );
                outlineMask.z = step( _OuterThreshold, outlineTex.z );
                outlineMask.w = saturate(outlineMask.x + outlineMask.y + outlineMask.z);
                
                float4 outerColor = outlineMask.x * _OuterLines;
                float4 innerColor = outlineMask.y * _InnerLines;
                float4 bColor = outlineMask.z * _OuterLines;
                float4 outlineColor = lerp(0, CompositeOver(CompositeOver(innerColor, outerColor), bColor), outlineMask.w);
                
                float4 cameraColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                float4 finalColor = lerpKeepAlpha(cameraColor.rgb, outlineColor, outlineMask.w);
                return cameraColor;
            }
            
            ENDHLSL
        }
    }
}