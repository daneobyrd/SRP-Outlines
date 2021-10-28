Shader "OutlineBlit"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        [HideInInspector] _OutlineOpaque ("Outline Opaque", 2D) = "white" {}
        [HideInInspector] _OutlineDepth ("Outline Depth", 2D) = "white" {}
        [HideInInspector] _BlurResults ("Blur Results", 2D) = "white" {}
        [HideInInspector] _OutlineTexture ("Outline Texture", 2D) = "white" {}
//        _OuterThreshold ("Outer Threshold", float) = 0.0
//        _InnerThreshold ("Inner Threshold", float) = 0.0
//        _Rotations ("Rotations", int) = 6
//        _DepthPush ("Depth Push", float) = 0.0
//        _OuterLUT ("Outer LUT", 2D) = "white" {}
//        _InnerLUT ("Inner Lut", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "OutlineBlit"
            HLSLPROGRAM
            // Reference:
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            struct outlined_obj
            {
                float3 positionWS;
                uint unity_InstanceID;
            };

            StructuredBuffer<outlined_obj> outline_objBuffer;

            // float _OuterThreshold;
            // float _InnerThreshold;
            // CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_OutlineOpaque);
            SAMPLER(sampler_OutlineOpaque);
            TEXTURE2D(_OutlineDepth);
            SAMPLER(sampler_OutlineDepth);
            TEXTURE2D(_OutlineTexture);
            SAMPLER(sampler_OutlineTexture);
            
            // CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                return FullscreenVert(input);
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 outlineColor = SAMPLE_TEXTURE2D(_OutlineTexture, sampler_OutlineTexture, input.uv);
                float4 cameraColorCopy = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 col = CompositeOver(outlineColor, cameraColorCopy);
                return col;
            }
            ENDHLSL
        }


        /*
                // Relocated GaussianPyramid to gaussian_pyramid.compute
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