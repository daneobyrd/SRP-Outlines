using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public enum SubTargetType
{
    Color,
    Depth,
    Shadowmap
}

[Serializable]
public class PassSubTarget
{
    public SubTargetType subTargetType;
    public List<string> lightModeTags;
    public string textureName;
    [NonSerialized] public int renderTargetInt;
    [NonSerialized] public RenderTargetIdentifier targetIdentifier;
    public bool createTexture;
    public RenderTextureFormat renderTextureFormat;

    public PassSubTarget(List<string> lightModeTags, string texName, SubTargetType type, bool createTexture,
                         RenderTextureFormat rtFormat)
    {
        subTargetType      = type;
        this.lightModeTags = lightModeTags;
        textureName        = texName;
        renderTargetInt    = Shader.PropertyToID(textureName);
        targetIdentifier   = new RenderTargetIdentifier(renderTargetInt);
        this.createTexture = createTexture;
        renderTextureFormat = subTargetType switch
        {
            SubTargetType.Color     => rtFormat,
            SubTargetType.Depth     => RenderTextureFormat.Depth,
            SubTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
            _                       => rtFormat
        };
    }

    public PassSubTarget(List<string> lightModeTags, string texName, SubTargetType type, bool createTexture)
    {
        subTargetType      = type;
        this.lightModeTags = lightModeTags;
        textureName        = texName;
        renderTargetInt    = Shader.PropertyToID(textureName);
        targetIdentifier   = new RenderTargetIdentifier(renderTargetInt);
        this.createTexture = createTexture;
        renderTextureFormat = subTargetType switch
        {
            SubTargetType.Color     => RenderTextureFormat.Default,
            SubTargetType.Depth     => RenderTextureFormat.Depth,
            SubTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
            _                       => RenderTextureFormat.Default
        };
    }
}