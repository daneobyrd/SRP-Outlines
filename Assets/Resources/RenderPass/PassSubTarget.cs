using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderPass.OutlineBuffers
{
    public enum SubTargetType
    {
        Color,
        Depth,
        Shadowmap
    }
    
    [System.Serializable]
    public class PassSubTarget
    {
        public List<string> lightModeTags;
        public string textureName;
        [HideInInspector] public int renderTargetInt;
        public RenderTargetIdentifier targetIdentifier;
        public bool createTexture;
        public RenderTextureFormat renderTextureFormat;

        public PassSubTarget(List<string> lightModeTags, string texName, SubTargetType type, bool createTexture, RenderTextureFormat rtFormat)
        {
            this.lightModeTags = lightModeTags;
            textureName = texName;
            renderTargetInt = Shader.PropertyToID(textureName);
            targetIdentifier = new RenderTargetIdentifier(renderTargetInt);
            this.createTexture = createTexture;
            renderTextureFormat = type switch
            {
                SubTargetType.Color => rtFormat,
                SubTargetType.Depth => RenderTextureFormat.Depth,
                SubTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
                _ => rtFormat
            };
        }

        public PassSubTarget(List<string> lightModeTags, string texName, SubTargetType type, bool createTexture)
        {
            this.lightModeTags = lightModeTags;
            textureName = texName;
            renderTargetInt = Shader.PropertyToID(textureName);
            targetIdentifier = new RenderTargetIdentifier(renderTargetInt);
            this.createTexture = createTexture;
            renderTextureFormat = type switch
            {
                SubTargetType.Color => RenderTextureFormat.Default,
                SubTargetType.Depth => RenderTextureFormat.Depth,
                SubTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}