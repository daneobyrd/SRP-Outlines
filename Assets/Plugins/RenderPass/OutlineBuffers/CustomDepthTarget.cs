using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public sealed class CustomDepthTarget: CustomPassTarget
{
    public CustomDepthTarget(bool enabled, string texName, List<string> lightModeTags, DepthBits depthBits)
        : base(enabled, texName, lightModeTags, depthBits)
    {
        name = "Depth";
    }

    public CustomDepthTarget() : base(false, default, default, default)
    {
        name = "Depth";
    }
}