#ifndef KAWASE_BLIT
#define KAWASE_BLIT

// Based on excerpt from Core.BlitColorAndDepth as of URP 12.1

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

TEXTURE2D(_FinalBlur);
SamplerState sampler_PointClamp;
SamplerState sampler_LinearClamp;

#if SHADER_API_GLES
    struct Attributes
    {
        float4 positionOS       : POSITION;
        float2 uv               : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
#else
struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};
                        
Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if SHADER_API_GLES
    float4 pos = input.positionOS;
    float2 uv  = input.uv;
    #else
    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
    #endif

    output.positionCS = pos;
    output.texcoord   = uv;
    return output;
}

float4 FragmentColorOnly(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;

    const float4 final_blur = SAMPLE_TEXTURE2D_LOD(_FinalBlur, sampler_LinearClamp, uv, 0);

    return final_blur;
}

#endif 