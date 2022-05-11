Shader "Custom/RenderFeature/OutlineBlit"
{
    HLSLINCLUDE
    #pragma target 5.0
    #pragma editor_sync_compilation
    // #pragma multi_compile _ _USE_DRAW_PROCEDURAL
    // #pragma multi_compile_fragment _ DEBUG_DISPLAY

    #include "BlitColorAndDepth_VertOnly.hlsl"
    #include "OutlineShaderProperties.hlsl"

    ENDHLSL

    Properties
    {
        [HDR] _RLines("Red Channel Lines", Color) = (0.1960784, 0.3098039, 0.3098039, 1)
        [HDR] _GLines("Green Channel Lines", Color) = (0.8705883, 0.9254903, 0.937255, 1)
        [HDR] _BLines("Blue Channel Lines", Color) = (0, 0.9254903, 0.937255, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        // 0: Color Only
        Pass
        {
            Name "Outline Color"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            // #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma exclude_renderers gles
            #pragma vertex Vert
            #pragma fragment FragmentColorOnly

            float4 FragmentColorOnly(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 outlineTex = SAMPLE_TEXTURE2D_LOD(_OutlineTexture, sampler_OutlineTexture, uv, 0);

                float4 outlineMask;
                outlineMask.x = SharpenAlpha(outlineTex.x, _OuterThreshold);
                outlineMask.y = SharpenAlpha(outlineTex.y, _InnerThreshold);
                outlineMask.z = SharpenAlpha(outlineTex.z, _InnerThreshold);
                outlineMask.w = SharpenAlpha(outlineTex.w, _OuterThreshold);

                const float camera_mask = normalize(outlineMask.x + outlineMask.y + outlineMask.z);

                const float4 outerColor = lerp(0, _RLines, outlineMask.x);
                const float4 innerColor = lerp(0, _GLines, outlineMask.y);
                const float4 bColor = lerp(0, _BLines, outlineMask.z);

                const float4 outlineColor = lerp(lerp(bColor, innerColor, camera_mask), outerColor, camera_mask);
                const float4 cameraColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);

                float4 final_color = lerp(cameraColor, outlineColor, camera_mask);
                return final_color;
            }
            ENDHLSL
        }
        // Color and Depth
        Pass
        {
            Name "Outline Color And Depth"
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentColorAndDepth

            struct PixelData
            {
                float4 color : SV_Target;
                real depth : SV_Depth;
            };

            PixelData FragmentColorAndDepth(Varyings input)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 outline_tex = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, uv);
                
                real scene_depth;
                #if UNITY_REVERSED_Z
                scene_depth = SampleSceneDepth(input.texcoord.xy);
                #else
                    // Adjust z to match NDC for OpenGL
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                const float outline_depth = SAMPLE_DEPTH_TEXTURE(_OutlineOpaqueDepth, sampler_PointClamp, uv).r;
                const float4 outline_opaque = SAMPLE_TEXTURE2D(_OutlineOpaqueColor, sampler_LinearClamp, uv);

                const float silhouette = ceil(saturate(outline_depth));

                float4 channel_mask;
                channel_mask.r = SharpenAlpha(outline_tex.r, _OuterThreshold);
                channel_mask.g = SharpenAlpha(outline_tex.g, _InnerThreshold);
                channel_mask.b = SharpenAlpha(outline_tex.b, _DepthPush);
                channel_mask.a = SharpenAlpha(outline_tex.a, _OuterThreshold);

                const float4 outline_mask = (saturate(channel_mask) * silhouette);
                const float camera_mask = silhouette - sign(length(outline_mask));

                const float4 final_mask = min(outline_mask, silhouette);
                
                const float4 r_color = lerp(0,_RLines, outline_mask.r);
                const float4 g_color = _GLines * channel_mask.g;
                const float4 b_color = _BLines * channel_mask.b;
                const float4 a_value = 1 * channel_mask.a;
                
                const float4 cameraColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv.xy);
                const float4 combined_outline = lerp(lerp(lerp(0, b_color, outline_mask.b), g_color, outline_mask.g), r_color, outline_mask.r);
                
                PixelData pd;
                // Out = ((1 - edge) * original) + (edge * lerp(original, OutlineColor,  OutlineColor.a));
                // pd.color = ((1 - outline_mask) * cameraColor) + (outline_mask * lerp(cameraColor, combined_outline, final_mask));
                // pd.color = outline_opaque;
                pd.color = SAMPLE_TEXTURE2D_LOD(_OutlineOpaqueColor, sampler_LinearClamp, input.texcoord.xy, 0);
                pd.depth = outline_depth;
                return pd;
            }
            ENDHLSL
        }
    }
}