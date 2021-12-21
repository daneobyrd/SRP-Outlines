Shader "OutlineBlit"
{
    Properties
    {
//        _SourceTex ("Source Texture", Any) = "white" { }
        _OuterLines("Outer Lines", Color) = (0.1960784, 0.3098039, 0.3098039, 1)
        _InnerLines("Inner Lines", Color) = (0.8705883, 0.9254903, 0.937255, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        Pass
        {
            Name "OutlineBlit"
            ZWrite Off Cull Off

            HLSLPROGRAM
            // #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma exclude_renderers gles
            #pragma target 5.0
            // #pragma editor_sync_compilation
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
            
            // #include tree
            // PostProcessing/Common.hlsl
                // core/ShaderLibrary/Color.hlsl
                // universal/Shaders/Utils/Fullscreen.hlsl
                    // universal/ShaderLibrary/Core.hlsl
                        // core/Common.hlsl
                        // core/ShaderLibrary/Packing.hlsl
                        // core/ShaderLibrary/Version.hlsl
                        // universal/ShaderLibrary/Input.hlsl
                            // universal/ShaderTypes.cs.hlsl
                            // universal/ShaderLibrary/Deprecated.hlsl
                            // universal/ShaderLibrary/UnityInput.hlsl"
                            // core/ShaderLibrary/UnityInstancing.hlsl"
                            // universal/ShaderLibrary/UniversalDOTSInstancing.hlsl"
                        // #ifdef HAVE_VFX_MODIFICATION
                            // visualeffectgraph/Shaders/VFXMatricesOverride.hlsl"
                        // #endif
                            // core/ShaderLibrary/SpaceTransforms.hlsl
            
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            
            // TEXTURE2D(_CameraColorAttachmentA);
            // TEXTURE2D(_CameraColorAttachmentB);
            // TEXTURE2D(_CameraColorAttachmentC);
            // TEXTURE2D(_CameraColorAttachmentD);

            float _OuterThreshold;
            float _InnerThreshold;
            float depthPush;

            CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);
            float4 _SourceTex_ST;

            float4 _OuterLines;
            float4 _InnerLines;
            CBUFFER_END

            TEXTURE2D(_OutlineOpaque);
            TEXTURE2D(_OutlineDepth);

            TEXTURE2D(_FinalBlur);

            TEXTURE2D(_OutlineTexture);
            // SAMPLER(sampler_OutlineTexture);

            float4 smoothAA(in float4 col)
            {
                return saturate(1 - abs(col));
            }

            float4 smoothAAsub(in float4 col, in float edge)
            {
                return saturate(1 - abs(col - edge));
            }

            float4 smoothAAfWidth(in float4 col, in float edge)
            {
                return saturate(1 - abs((col - edge) / fwidth(col)));
            }

            float aaStep(float compValue, float gradient)
            {
                float halfChange = fwidth(gradient) / 2;
                // base the range of the inverse lerp on the change over one pixel
                float lowerEdge = compValue - halfChange;
                float upperEdge = compValue + halfChange;
                //do the inverse interpolation
                float stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
                stepped = saturate(stepped);
                return stepped;
            }

            // Combine textures in blit shader
            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float4 outlineTex = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_PointClamp, uv);

                float4 outlineMask;
                outlineMask.x = step(_OuterThreshold, outlineTex.x);
                outlineMask.y = step(_InnerThreshold, outlineTex.y);
                outlineMask.z = step(depthPush, outlineTex.z);
                
                /*
                outlineMask.x = smoothAA(outlineTex.x);
                outlineMask.y = smoothAA(outlineTex.y);
                outlineMask.z = smoothAA(outlineTex.z);
                */

                /*
                outlineMask.x = smoothAAsub(outlineTex.x, _OuterThreshold);
                outlineMask.y = smoothAAsub(outlineTex.y, _InnerThreshold);
                outlineMask.z = smoothAAsub(outlineTex.z, _OuterThreshold);
                */

                /*
                outlineMask.x = smoothAAfWidth(outlineTex.x, _OuterThreshold);
                outlineMask.y = smoothAAfWidth(outlineTex.y, _InnerThreshold);
                outlineMask.z = smoothAAfWidth(outlineTex.z, _OuterThreshold);
                */

                outlineMask.w = saturate(outlineMask.x * outlineMask.y * outlineMask.z);

                float4 opaque = SAMPLE_TEXTURE2D(_OutlineOpaque, sampler_LinearClamp, uv);
                float4 blur = SAMPLE_TEXTURE2D(_FinalBlur, sampler_PointClamp, uv);

                const float4 outerColor = outlineMask.x * _OuterLines;
                const float4 innerColor = outlineMask.y * _InnerLines;
                const float4 bColor = outlineMask.z * _OuterLines;

                const float4 compositeOutline = CompositeOver(CompositeOver(innerColor, outerColor), bColor);
                const float4 outlineColor = compositeOutline;

                float4 cameraColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                // float4 cameraColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv);
                float4 finalColor = lerp(cameraColor, outlineColor, outlineMask.w);
                return outlineTex;
            }
            ENDHLSL
        }
    }
}