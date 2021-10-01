Shader "Outline Post Process"
{
    Properties
    {
        _OuterThreshold ("Outer Threshold", float) = 0.0
        _InnerThreshold ("Inner Threshold", float) = 0.0
        _Rotations ("Rotations", int) = 6
        _DepthPush ("Depth Push", float) = 0.0
        _OuterLUT ("Outer LUT", 2D) = "white" {}
        _InnerLUT ("Inner Lut", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "LightMode" = "Outline"
        }
        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            // Includes Unity includes and shared Attributes and Varyings
            #include "OutlineShared.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _OuterThreshold;
            float _InnerThreshold;
            Texture2D<float> _LineNoise;
            sampler2D sampler_LineNoise;
            float perObjectFloat;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // float grayscale_opaque = Luminance();

                // float depth_value = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_OutlineDepthTexture, input.uv);
                // float b_noise = SAMPLE_TEXTURE2D_X(_LineNoise, sampler_LineNoise, input.uv);
                // float4 col = float4(perObjectFloat, grayscale_opaque, b_noise, depth_value);
                float4 col = float4(1.0, 0.0, 0.0, 1.0);
                return col;
            }
            ENDHLSL
        }

/*
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
            #include "SamplingKernels.hlsl"

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
                // OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
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