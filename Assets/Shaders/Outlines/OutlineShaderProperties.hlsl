#ifndef OUTLINE_SHADER_PROPERTIES_INCLUDED
#define OUTLINE_SHADER_PROPERTIES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
CBUFFER_START(UnityPerMaterial)
float4 _RLines;
float4 _GLines;
float4 _BLines;
CBUFFER_END

float _OuterThreshold;
float _InnerThreshold;
float _DepthPush;
TEXTURE2D(_OutlineOpaqueColor);
TEXTURE2D(_OutlineOpaqueDepth);

TEXTURE2D(_FinalBlur);

TEXTURE2D(_OutlineTexture);
SAMPLER(sampler_OutlineTexture);

#endif // OUTLINE_SHADER_PROPERTIES_INCLUDED