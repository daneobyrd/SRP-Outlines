// Source: https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/master/Assets/_CompletedDemos/3DCharacterUI/KawaseBlur.shader

Shader "Custom/RenderFeature/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        //   _offset ("Offset", float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "Universal"
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

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            // sampler2D _CameraOpaqueTexture;
            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;
            CBUFFER_END

            float _offset;

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.vertex = TransformObjectToHClip(v.vertex), 1;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings input) : SV_Target
            {
                const float2 res = _MainTex_TexelSize.xy;
                float i = _offset;

                float4 col;
                col.rgb = tex2D(_MainTex, input.uv).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(i, i) * res).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(i, -i) * res).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(-i, i) * res).rgb;
                col.rgb += tex2D(_MainTex, input.uv + float2(-i, -i) * res).rgb;
                col.rgb /= 5.0f;

                return col;
            }
            ENDHLSL
        }
    }
}