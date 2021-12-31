using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public enum CustomTargetType
{
    Color,
    Depth,
    Shadowmap
}

[Serializable]
public class PassSubTarget
{
    public CustomTargetType customTargetType;
    public List<string> lightModeTags;
    public string textureName;
    [NonSerialized] public int renderTargetInt;
    [NonSerialized] public RenderTargetIdentifier targetIdentifier;
    public bool createTexture;
    public RenderTextureFormat renderTextureFormat;

    public PassSubTarget(List<string> lightModeTags, string texName, CustomTargetType type, bool createTexture, RenderTextureFormat rtFormat)
    {
        customTargetType      = type;
        this.lightModeTags = lightModeTags;
        textureName        = texName;
        renderTargetInt    = Shader.PropertyToID(textureName);
        targetIdentifier   = new RenderTargetIdentifier(renderTargetInt);
        this.createTexture = createTexture;
        renderTextureFormat = customTargetType switch
        {
            CustomTargetType.Color     => rtFormat,
            CustomTargetType.Depth     => RenderTextureFormat.Depth,
            CustomTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
            _                       => rtFormat
        };
    }

    public PassSubTarget(List<string> lightModeTags, string texName, CustomTargetType type, bool createTexture)
    {
        customTargetType      = type;
        this.lightModeTags = lightModeTags;
        textureName        = texName;
        renderTargetInt    = Shader.PropertyToID(textureName);
        targetIdentifier   = new RenderTargetIdentifier(renderTargetInt);
        this.createTexture = createTexture;
        renderTextureFormat = customTargetType switch
        {
            CustomTargetType.Color     => RenderTextureFormat.Default,
            CustomTargetType.Depth     => RenderTextureFormat.Depth,
            CustomTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
            _                       => RenderTextureFormat.Default
        };
    }
}