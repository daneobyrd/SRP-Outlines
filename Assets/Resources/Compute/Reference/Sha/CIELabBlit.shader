Shader "Unlit/CIELabBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "LabColorSpace.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Checking the range of the function and making sure it is within the expected range
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                col.xyz = rgb2lab(col.xyz);
                //float4 out_ = float4(0., 0., 0., 1.);
                //if (col.x < 0.0 || col.y < 0.0 || col.z < 0.0) out_.x = 1.;
                //if (col.x > 1.0 || col.y > 1.0 || col.z > 1.0) out_.y = 1.;

                return col;
            }
            ENDHLSL
        }
    }
}