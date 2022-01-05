using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CustomDepthTarget: CustomPassTarget
{
    public CustomDepthTarget(bool enabled,  string texName, List<string> lightModeTags) : base(enabled, texName, lightModeTags)
    {
        renderTextureFormat = RenderTextureFormat.Depth;
    }
}