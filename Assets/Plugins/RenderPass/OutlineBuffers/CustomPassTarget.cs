using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public abstract class CustomPassTarget
{
    [HideInInspector]
    [SerializeField] public string name;
    
    [SerializeField] public bool enabled;
    [SerializeField] public string textureName;
    // [NonSerialized] public int NameID;
    // [NonSerialized] public RenderTargetIdentifier RTID;
    [SerializeField] public List<string> lightModeTags;
    [SerializeField] public DepthBits depthBits;

    // public RenderTextureFormat renderTextureFormat;

    protected CustomPassTarget(bool enabled, string texName, List<string> lightModeTags, DepthBits depthBits)
    {
        this.enabled       = enabled;
        textureName        = texName;
        // NameID             = Shader.PropertyToID(textureName);
        // RTID               = new RenderTargetIdentifier(NameID);
        this.lightModeTags  = lightModeTags;
        this.depthBits      = depthBits;
    }

    /*
    public void SetIDs()
    {
        if (NameID == default)
            NameID = Shader. PropertyToID(textureName);
        if (RTID == default)
            RTID   = new RenderTargetIdentifier(NameID);
    }
    */
    
    // Temp getter/setter variables until I make an editor and handle serialization
    public int GetNameID()
    {
        return Shader.PropertyToID(textureName);
    }

    public RenderTargetIdentifier GetRTID()
    {
        return new RenderTargetIdentifier(GetNameID());
    }
}