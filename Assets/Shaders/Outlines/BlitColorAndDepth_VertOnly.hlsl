#ifndef BLIT_COLOR_AND_DEPTH_VERT_ONLY
#define BLIT_COLOR_AND_DEPTH_VERT_ONLY

// Excerpt from Core.BlitColorAndDepth as of URP 12.1

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

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

#endif // MY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED