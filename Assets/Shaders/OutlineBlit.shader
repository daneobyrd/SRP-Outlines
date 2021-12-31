Shader "OutlineBlit"
{
    Properties
    {
        //        _SourceTex ("Source Texture", Any) = "white" { }
        [HDR] _OuterLines("Outer Lines", Color) = (0.1960784, 0.3098039, 0.3098039, 1)
        [HDR] _InnerLines("Inner Lines", Color) = (0.8705883, 0.9254903, 0.937255, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "OutlineBlit"
            Cull Back
            ZTest LEqual
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            // #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma exclude_renderers gles
            #pragma target 5.0
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            // #include tree
            /*
            PostProcessing/Common.hlsl
                core/ShaderLibrary/Color.hlsl
                universal/Shaders/Utils/Fullscreen.hlsl
                    universal/ShaderLibrary/Core.hlsl
                        core/Common.hlsl
                        core/ShaderLibrary/Packing.hlsl
                        core/ShaderLibrary/Version.hlsl
                        universal/ShaderLibrary/Input.hlsl
                            universal/ShaderTypes.cs.hlsl
                            universal/ShaderLibrary/Deprecated.hlsl
                            universal/ShaderLibrary/UnityInput.hlsl"
                            core/ShaderLibrary/UnityInstancing.hlsl"
                            universal/ShaderLibrary/UniversalDOTSInstancing.hlsl"
                        #ifdef HAVE_VFX_MODIFICATION
                            visualeffectgraph/Shaders/VFXMatricesOverride.hlsl"
                        #endif
                            core/ShaderLibrary/SpaceTransforms.hlsl
            */
            
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            // TEXTURE2D(_CameraColorAttachmentA);
            // TEXTURE2D(_CameraColorAttachmentB);
            // TEXTURE2D(_CameraColorAttachmentC);
            // TEXTURE2D(_CameraColorAttachmentD);

            float _OuterThreshold;
            float _InnerThreshold;
            float _DepthPush;

            CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);
            float4 _SourceTex_ST;

            float4 _OuterLines;
            float4 _InnerLines;
            CBUFFER_END

            TEXTURE2D(_OutlineOpaqueColor);
            TEXTURE2D(_OutlineOpaqueDepth);

            TEXTURE2D(_FinalBlur);

            TEXTURE2D(_OutlineTexture);
            SAMPLER(sampler_OutlineTexture);

            // Combine textures in blit shader
            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float4 outlineTex = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, uv);

                float4 outlineMask;
                outlineMask.x = step(outlineTex.x, _OuterThreshold);
                outlineMask.y = step(outlineTex.y, _InnerThreshold);
                outlineMask.z = step(outlineTex.z, _InnerThreshold);
                outlineMask.w = step(outlineTex.w, _OuterThreshold);
                
                // outlineMask.x = Smootherstep01(outlineTex.x);
                // outlineMask.y = Smootherstep01(outlineTex.y);
                // outlineMask.z = Smootherstep01(outlineTex.z);
                // outlineMask.w = Smootherstep01(outlineTex.w);
                
                // outlineMask.x = smoothAA(outlineTex.x);
                // outlineMask.y = smoothAA(outlineTex.y);
                // outlineMask.z = smoothAA(outlineTex.z);
                // outlineMask.w = smoothAA(outlineTex.w);

                /*
                outlineMask.x = smoothAAsub(outlineTex.x, _OuterThreshold);
                outlineMask.y = smoothAAsub(outlineTex.y, _InnerThreshold);
                outlineMask.z = smoothAAsub(outlineTex.z, _DepthPush);
                outlineMask.w = smoothAAsub(outlineTex.w, _DepthPush);
                */

                /*
                outlineMask.x = smoothAAfWidth(outlineTex.x, _OuterThreshold);
                outlineMask.y = smoothAAfWidth(outlineTex.y, _InnerThreshold);
                outlineMask.z = smoothAAfWidth(outlineTex.z, _OuterThreshold);
                outlineMask.w = smoothAAfWidth(outlineTex.w, _OuterThreshold);
                */

                /*
                outlineMask.x = aaStep(_OuterThreshold,outlineTex.x);
                outlineMask.y = aaStep(_InnerThreshold,outlineTex.y);
                outlineMask.z = aaStep(_DepthPush,outlineTex.z);
                outlineMask.w = aaStep(_OuterThreshold, outlineTex.w);
                */

                // float test = SafeNormalize(outlineTex.r + outlineTex.g + outlineTex.b);
                const float cameraMask = SafeNormalize(outlineMask.x + outlineMask.y + outlineMask.z);

                const float4 outerColor = lerp( 0, _OuterLines, outlineMask.x);
                const float4 innerColor = lerp(0, _InnerLines, outlineMask.y);
                const float4 bColor = lerp(0, _OuterLines, outlineMask.z);
                
                const float4 outlineColor = lerp(lerp(bColor, innerColor, cameraMask), outerColor, cameraMask);

                const float4 cameraColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                // float4 cameraColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv);
                float4 finalColor = lerp(cameraColor, outlineColor, cameraMask);
                return finalColor;
            }
            ENDHLSL
        }
    }
}