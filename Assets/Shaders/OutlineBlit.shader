Shader "OutlineBlit"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "clear" {}
        _SourceTex ("SourceTex", 2D) = "clear" {}
        _OuterThreshold ("Outer Threshold", float) = 1.0
        _InnerThreshold ("Inner Threshold", float) = 1.0
        //        _Rotations ("Rotations", int) = 6
        //        _DepthPush ("Depth Push", float) = 0.0
        _OuterLUT ("Outer LUT", 2D) = "white" {}
        _InnerLUT ("Inner Lut", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Shader Model" = "5.0"
        }
        Pass
        {
            Name "OutlineBlit"
            Blend One One
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma exclude_renderers gles
            #pragma target 5.0
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebuggingFullscreen.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            /*struct outlined_obj
            {
                float3 positionWS;
                uint unity_InstanceID;
            };
            
            StructuredBuffer<outlined_obj> outline_objBuffer;*/

            CBUFFER_START(UnityPerMaterial)
            float OuterThreshold;
            float InnerThreshold;
            CBUFFER_END

            TEXTURE2D_X(_OutlineOpaque);
            TEXTURE2D(_OutlineDepth);
            TEXTURE2D(_BlurUpsampleTex);
            TEXTURE2D(_OutlineTexture);

            SAMPLER(sampler_LinearClamp);
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float4 outlineTex = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_LinearClamp, uv);
                float outlineMask = saturate(step(outlineTex.x, OuterThreshold) + step(outlineTex.y, InnerThreshold) + outlineTex.z);
                float4 cameraColorCopy = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, uv);
                float3 col = lerp(cameraColorCopy.xyz, -outlineMask, outlineMask);
                return float4(col, 1);
            }
            ENDHLSL
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

    }
}