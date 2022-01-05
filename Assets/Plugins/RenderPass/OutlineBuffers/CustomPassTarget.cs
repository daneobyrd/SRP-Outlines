using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public abstract class CustomPassTarget
{
    public bool enabled;
    public string textureName;
    [NonSerialized] public int NameID;
    [NonSerialized] public RenderTargetIdentifier RTID;
    public List<string> lightModeTags;
    public RenderTextureFormat renderTextureFormat;
    
    // Custom Color Target
    protected CustomPassTarget(bool enabled, string texName, List<string> lightModeTags, RenderTextureFormat rtFormat)
    {
        this.enabled        = enabled;
        textureName         = texName;
        NameID              = Shader.PropertyToID(textureName);
        RTID                = new RenderTargetIdentifier(NameID);
        this.lightModeTags  = lightModeTags;
        renderTextureFormat = rtFormat;
    }

    // Custom Depth/Shadowmap Target 
    protected CustomPassTarget(bool enabled, string texName, List<string> lightModeTags)
    {
        this.enabled       = enabled;
        textureName        = texName;
        NameID             = Shader.PropertyToID(textureName);
        RTID               = new RenderTargetIdentifier(NameID);
        this.lightModeTags = lightModeTags;
    }
}