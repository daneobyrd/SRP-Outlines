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
            "RenderPipeline" = "Universal"
            "LightMode" = "Outline"
        }
        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            // Reference:
            // #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            #if _USE_DRAW_PROCEDURAL
            void GetProceduralQuad(in uint vertexID, out float4 positionCS, out float2 uv)
            {
                positionCS = GetQuadVertexPosition(vertexID);
                positionCS.xy = positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
                uv = GetQuadTexCoord(vertexID) * _ScaleBias.xy + _ScaleBias.zw;
            }
            #endif

            struct outlined_obj
            {
                float3 positionWS;
                uint unity_InstanceID;
            };
            
            StructuredBuffer<outlined_obj> outline_objBuffer;

            CBUFFER_START(UnityPerMaterial)
            float _OuterThreshold;
            float _InnerThreshold;
            Texture2D<float> _LineNoise;
            sampler2D sampler_LineNoise;
            float perObjectFloat;
            CBUFFER_END

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

            float4 frag(Varyings input) : SV_Target
            {
                float4 col = float4(1.0, 0.0, 0.0, 1.0);
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