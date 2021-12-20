Shader "OutlineBlit"
{
    Properties
    {
        _SourceTex ("Source Texture", Any) = "white" { }
        _OuterLines("Outer Lines", Color) = (0, 0, 0, 1)
        _InnerLines("Inner Lines", Color) = (1, 1, 0, 1)
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
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
            // #pragma multi_compile_fragment _ DEBUG_DISPLAY

            // #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #if _USE_DRAW_PROCEDURAL
            void GetProceduralQuad(in uint vertexID, out float4 positionCS, out float2 uv)
            {
                positionCS = GetQuadVertexPosition(vertexID);
                positionCS.xy = positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
                uv = GetQuadTexCoord(vertexID) * _ScaleBias.xy + _ScaleBias.zw;
            }
            #endif

            struct Attributes
            {
                #if _USE_DRAW_PROCEDURAL
                uint vertexID     : SV_VertexID;
                #else
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings FullscreenVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                #if _USE_DRAW_PROCEDURAL
                output.positionCS = GetQuadVertexPosition(input.vertexID);
                output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
                output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
                #else
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                #endif

                return output;
            }

            Varyings Vert(Attributes input)
            {
                return FullscreenVert(input);
            }


            
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            
            // TEXTURE2D(_CameraColorAttachmentA);
            // TEXTURE2D(_CameraColorAttachmentB);
            // TEXTURE2D(_CameraColorAttachmentC);
            // TEXTURE2D(_CameraColorAttachmentD);
            SAMPLER(sampler_LinearClamp);

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

            // TEXTURE2D(_FinalBlur);
            
            TEXTURE2D(_OutlineTexture);
            SAMPLER(sampler_OutlineTexture);

            // Combine textures in blit shader

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float4 outlineTex = SAMPLE_TEXTURE2D_X(_OutlineTexture, sampler_OutlineTexture, uv);

                float4 outlineMask;
                outlineMask.x = step(_OuterThreshold, outlineTex.x);
                outlineMask.y = step(_InnerThreshold, outlineTex.y);
                outlineMask.z = step(_OuterThreshold, outlineTex.z);
                outlineMask.w = saturate(outlineMask.x + outlineMask.y + outlineMask.z);

                float4 opaque = SAMPLE_TEXTURE2D_X(_OutlineOpaque, sampler_LinearClamp, uv);
                
                const float4 outerColor = outlineMask.x * _OuterLines;
                const float4 innerColor = outlineMask.y * _InnerLines;
                const float4 bColor = outlineMask.z * _OuterLines;
                
                const float4 compositeOutline = CompositeOver(CompositeOver(innerColor, outerColor), bColor);
                const float4 outlineColor = compositeOutline;

                float4 cameraColor = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                // float4 cameraColor = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, uv);
                float4 finalColor = lerp(cameraColor, outlineColor, outlineMask.w);
                return finalColor;
            }
            ENDHLSL
        }
    }
}