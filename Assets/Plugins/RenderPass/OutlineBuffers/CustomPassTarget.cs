using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum CustomPassTargetType
{
    Color,
    Depth,
    Shadowmap
}

[Serializable]
public class CustomPassTarget
{
    public CustomPassTargetType customPassTargetType;
    public List<string> lightModeTags;
    public string textureName;
    [NonSerialized] public int RTIntId;
    [NonSerialized] public RenderTargetIdentifier RTIdentifier;
    public bool enabled;
    public RenderTextureFormat renderTextureFormat;

    public CustomPassTarget()
    {
        customPassTargetType = CustomPassTargetType.Color;
        lightModeTags        = new List<string> { "SRPDefaultUnlit" };
        textureName          = "_CustomColor";
        RTIntId              = Shader.PropertyToID(textureName);
        RTIdentifier         = new RenderTargetIdentifier(RTIntId);
        enabled              = false;
        renderTextureFormat  = RenderTextureFormat.ARGBFloat;
    }

    public CustomPassTarget(List<string> lightModeTags, string texName, CustomPassTargetType type, bool enabled, RenderTextureFormat rtFormat)
    {
        customPassTargetType      = type;
        this.lightModeTags = lightModeTags;
        textureName        = texName;
        RTIntId    = Shader.PropertyToID(textureName);
        RTIdentifier   = new RenderTargetIdentifier(RTIntId);
        this.enabled = enabled;
        renderTextureFormat = customPassTargetType switch
        {
            CustomPassTargetType.Color     => rtFormat,
            CustomPassTargetType.Depth     => RenderTextureFormat.Depth,
            CustomPassTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
            _                              => renderTextureFormat
        };
    }

    public CustomPassTarget(List<string> lightModeTags, string texName, CustomPassTargetType type, bool enabled)
    {
        customPassTargetType      = type;
        this.lightModeTags = lightModeTags;
        textureName        = texName;
        RTIntId    = Shader.PropertyToID(textureName);
        RTIdentifier   = new RenderTargetIdentifier(RTIntId);
        this.enabled = enabled;
        renderTextureFormat = customPassTargetType switch
        {
            CustomPassTargetType.Color     => RenderTextureFormat.Default,
            CustomPassTargetType.Depth     => RenderTextureFormat.Depth,
            CustomPassTargetType.Shadowmap => RenderTextureFormat.Shadowmap,
            _                       => RenderTextureFormat.Default
        };
    }
}