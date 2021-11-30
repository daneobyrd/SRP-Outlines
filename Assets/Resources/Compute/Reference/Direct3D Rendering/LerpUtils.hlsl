#ifndef LERP_UTILS
#define LERP_UTILS

// Compute constant buffer
cbuffer ComputeConstants : register(b0)
{
    float LerpT;
}

float4 lerpKeepAlpha(float4 source, float3 target, float T)
{
    return float4(lerp(source.rgb, target, T), source.a);
}

float4 lerpKeepAlpha(float3 source, float4 target, float T)
{
    return float4(lerp(source, target.rgb, T), target.a);
}
#endif
