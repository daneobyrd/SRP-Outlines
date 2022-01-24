using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CustomColorTarget : CustomPassTarget
{
    // public bool overrideColorFormat;
    // private RenderTextureFormat _rtFormat;
    public CustomColorTarget(bool enabled, string texName, List<string> lightModeTags, RenderTextureFormat rtFormat) : base(enabled, texName, lightModeTags, rtFormat)
    {
        renderTextureFormat = rtFormat is not (RenderTextureFormat.Depth or RenderTextureFormat.Shadowmap) ? rtFormat : RenderTextureFormat.ARGBFloat;
    }
}