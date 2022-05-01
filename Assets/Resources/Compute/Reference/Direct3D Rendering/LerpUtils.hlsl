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

float4 smoothAA(float4 col)
{
    return saturate(1 - abs(col));
}

float4 smoothAAsub(const in float4 col, const in float edge)
{
    return saturate(1 - abs(col - edge));
}

float4 smoothAAfWidth(const in float4 col, const in float edge)
{
    return saturate(1 - (abs((col - edge) / fwidth(col))));
}

float aaStep(float compValue, float gradient)
{
    float halfChange = fwidth(gradient) / 2;
    // base the range of the inverse lerp on the change over one pixel
    float lowerEdge = compValue - halfChange;
    float upperEdge = compValue + halfChange;
    //do the inverse interpolation
    float stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
    stepped = saturate(stepped);
    return stepped;
}

float4 aaStep(const float compValue, const float4 gradient)
{
    float4 halfChange = fwidth(gradient) / 2;
    // base the range of the inverse lerp on the change over one pixel
    float4 lowerEdge = compValue - halfChange;
    float4 upperEdge = compValue + halfChange;
    //do the inverse interpolation
    float4 stepped = (gradient - lowerEdge) / (upperEdge - lowerEdge);
    stepped = saturate(stepped);
    return stepped;
}


#endif
