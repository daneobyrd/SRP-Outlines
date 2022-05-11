using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Plugins.ScriptableRenderPass
{
[Serializable]
public sealed class CustomDepthTarget: CustomPassTarget
{
    // No RenderTextureFormat needed as RenderTextureFormat will always be Depth.
    // Use RenderTextureFormat.Depth when creating temporary render textures.
    
    public CustomDepthTarget(bool enabled, string texName, List<string> lightModeTags, DepthBits depthBits) : base(enabled, texName, lightModeTags, depthBits) { }

    public CustomDepthTarget() : base(false, default, default, default) { }
    
}
}