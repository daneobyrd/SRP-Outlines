using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public sealed class CustomColorTarget : CustomPassTarget
{
    public RenderTextureFormat renderTextureFormat;
    // public RenderTextureReadWrite textureColorSpace;
    
    public CustomColorTarget(bool enabled, string texName, List<string> lightModeTags, RenderTextureFormat rtFormat, DepthBits depthBits) :
        base(enabled, texName, lightModeTags, depthBits)
    {
        renderTextureFormat = rtFormat is not (RenderTextureFormat.Depth or RenderTextureFormat.Shadowmap)
            ? rtFormat
            : RenderTextureFormat.Default;
        // textureColorSpace = colorSpace;
    }

    public CustomColorTarget()
        : base(false, "customColorTarget", new List<string> {"YourCustomPass"}, 0)
    {
        renderTextureFormat = RenderTextureFormat.Default;
    }
}