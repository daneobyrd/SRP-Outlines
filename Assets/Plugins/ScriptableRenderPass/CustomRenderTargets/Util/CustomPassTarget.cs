using System;
using System.Collections.Generic;
using MyBox;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins.ScriptableRenderPass
{
    /// <summary>
    /// Class of relevant properties useful for creating custom render targets.
    /// </summary>
    [Serializable]
    public abstract class CustomPassTarget
    {
        [SerializeField] public bool enabled = false;
        [SearchableEnum] public BuiltinRenderTextureType renderTextureType;

        [SerializeField] public string textureName;
        [SerializeField] public Texture texture;
        protected RenderTexture RenderTexture => (RenderTexture) texture;

        // private int NameID => Shader.PropertyToID(textureName);
        // private RenderTargetIdentifier RTID   => new(NameID);

        [SerializeField] public List<string> lightModeTags;
        [SerializeField] public DepthBits depthBits;

        protected CustomPassTarget(bool enabled, string texName, List<string> lightModeTags, DepthBits depthBits)
        {
            this.enabled = enabled;
            textureName  = texName;
            // NameID             = Shader.PropertyToID(textureName);
            // RTID               = new RenderTargetIdentifier(NameID);
            this.lightModeTags = lightModeTags;
            this.depthBits     = depthBits;
        }

        // Temp getter/setter variables until I make an editor and handle serialization
        public int GetNameID()
        {
            return Shader.PropertyToID(textureName);
        }

        public RenderTargetIdentifier GetRTID()
        {
            return (renderTextureType == BuiltinRenderTextureType.PropertyName)
                ? new RenderTargetIdentifier(GetNameID())
                : GetBuiltInRTID(renderTextureType);
        }

        public RenderTargetIdentifier GetBuiltInRTID(BuiltinRenderTextureType rtType)
        {
            return rtType switch
            {
                BuiltinRenderTextureType.BufferPtr => new RenderTargetIdentifier(
                    rtType.GetType().DeclaringType == typeof(CustomDepthTarget)
                        ? RenderTexture.depthBuffer
                        : RenderTexture.colorBuffer),
                BuiltinRenderTextureType.RenderTexture   => new RenderTargetIdentifier(RenderTexture),
                BuiltinRenderTextureType.BindableTexture => new RenderTargetIdentifier(texture),
                _                                        => new RenderTargetIdentifier(renderTextureType)
            };
        }
    }
}