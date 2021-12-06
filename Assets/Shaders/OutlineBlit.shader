Shader "OutlineBlit"
{
    Properties
    {
        //        _MainTex ("MainTex", 2D) = "clear" {}
        //        _SourceTex ("SourceTex", 2D) = "clear" {}
        //        _OuterThreshold ("Outer Threshold", float) = 1.0
        //        _InnerThreshold ("Inner Threshold", float) = 1.0
        //        _Rotations ("Rotations", int) = 6
        //        _DepthPush ("Depth Push", float) = 0.0
        //        _OuterLUT ("Outer LUT", 2D) = "white" {}
        //        _InnerLUT ("Inner Lut", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "OutlineBlit"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma exclude_renderers gles
            #pragma target 5.0
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            SamplerState sampler_LinearClamp;

            float _OuterThreshold;
            float _InnerThreshold;

            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);

            TEXTURE2D(_OutlineOpaque);
            TEXTURE2D(_OutlineDepth);

            TEXTURE2D(_BlurredUpsampleTex);
            TEXTURE2D(_OutlineTexture);
            
            // Combine textures in blit shader
            
            float4 Fragment(Varyings input) : SV_Target
            {
                
                float2 uv = input.uv;
            // #if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
            //     float4 outlineTex = SAMPLE_TEXTURE2D_ARRAY_LOD(_OutlineTexture, sampler_LinearClamp, uv, 0);
            //     float outlineMask = saturate(outlineTex); // _OutlineTex is normalized in the compute shader
            // #else
                
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 outlineTex = SAMPLE_TEXTURE2D_LOD(_OutlineTexture, sampler_LinearClamp, uv, 0);
                float outlineMask = saturate(outlineTex); // _OutlineTex is normalized in the compute shader

            // #endif
                
                //step(outlineTex.x, _OuterThreshold) + step(outlineTex.y, _InnerThreshold) + outlineTex.z);
                float4 camColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                float4 color = lerp(camColor, -outlineMask, outlineMask);
                return color;
            }
            
            
            // Combine textures in compute shader
            /*
            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                const float2 uv = input.uv;
                float4 color = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
                return color;
            }
            */
            ENDHLSL
        }
    }
}

/*
        // Relocated GaussianPyramid to RenderPass/Blur/GaussianPyramid.compute
        Tags
        {
            "LightMode" = "OutlinePost"
        }
        Pass
        {
            Name "OutlinePost"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0


            #include "OutlineShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
            Texture2D<float4> _OutlineOpaqueTexture;
            sampler2D sampler_OutlineOpaqueTexture;
            Texture2D<float> _OutlineDepthTexture;
            sampler2D sampler_OutlineDepthTexture;

            Texture2D<float4> _MainTex;
            sampler2D sampler_MainTex;
            // float4 _MainTex_ST;
            float2 _MainTex_TexelSize;
            float _gaussian_sigma;
            int _laplaceKernelType;
            CBUFFER_END

            float gaussian(float x, float y)
            {
                float sigma2 = _gaussian_sigma * _gaussian_sigma;
                return exp(-(((x * x) + (y * y)) / (2.0 * sigma2))) * (1 / sqrt(TWO_PI * _gaussian_sigma));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return OUT;
            }

            float4 frag(Varyings input) : SV_TARGET
            {
                float4 blurColor = (0., 0., 0., 1.);

                float sum = 0.0f;
                for (int x = -1; x <= 1; ++x)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        const float gaus_factor = gaussian(x, y);
                        sum += gaus_factor;
                        blurColor += gaus_factor * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex,
                                                                    input.uv + float2(_MainTex_TexelSize.x * x, _MainTex_TexelSize.y * y));
                    }
                }

                blurColor = float4(blurColor.xyz / sum, 1);


                return blurColor;
            }
            ENDHLSL
        }
*/