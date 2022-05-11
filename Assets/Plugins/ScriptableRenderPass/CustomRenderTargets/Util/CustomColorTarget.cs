using System;
using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins.ScriptableRenderPass
{
    [Serializable]
    public sealed class CustomColorTarget : CustomPassTarget
    {
        [SearchableEnum]
        public RenderTextureFormat renderTextureFormat;
    
        public CustomColorTarget(bool enabled, string texName, List<string> lightModeTags, RenderTextureFormat rtFormat, DepthBits depthBits)
            : base(enabled, texName, lightModeTags, depthBits)
        {
            renderTextureFormat = rtFormat is not (RenderTextureFormat.Depth or RenderTextureFormat.Shadowmap) ? rtFormat : RenderTextureFormat.Default;
        }

        public CustomColorTarget() : base(false, default, default, default)
        {
            renderTextureFormat = RenderTextureFormat.Default;
        }
    }
}