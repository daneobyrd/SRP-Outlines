Shader "OutlineSource"
{
    Properties
    {
        [BaseMap] _BaseColor("TestTexture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "Universal"
            "LightMode" = "Outline"
        }
        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            // Reference:
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitForwardPass.hlsl"

            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0
            // #pragma vertex vert
            // #pragma fragment frag

            struct outlined_obj
            {
                float3 positionWS;
                uint unity_InstanceID;
            };
            
            StructuredBuffer<outlined_obj> outline_objBuffer;            
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