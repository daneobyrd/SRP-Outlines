// This was originally inspired by a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
public enum RenderQueueType
{
    Opaque = 0,
    Transparent = 1
}

[Serializable]
public class ShaderPassToRTSettings
{
    public bool enabled;
    public string profilerTag;
    
    public FilteringSettings FilteringSettings; // Not serializable.

    [Header("Filters")]
    public RenderQueueType renderQueueType;
    public LayerMask layerMask = 1;
    public LightLayerEnum lightLayerMask;

    [Header("Render Texture Settings")]
    public RenderTextureDescriptor CameraRTDescriptor; 
    // Not Serializable
    public RenderTextureFormat colorFormat;
    public int depthBufferBits;

    // TODO: Replace with list of CustomPassTargets that accept CustomColorTargets and (one) CustomDepthTarget.
    public CustomColorTarget[] customColorTargets;
    public CustomDepthTarget customDepthTarget;

    public ShaderPassToRTSettings(string name, RenderQueueType queueType)
    {
        profilerTag     = name;
        renderQueueType = queueType;
        colorFormat     = RenderTextureFormat.ARGBFloat;
        depthBufferBits = 16;

        // RenderTargets
        customColorTargets = new CustomColorTarget[]
        {
            new(true, "_OutlineOpaqueColor", new List<string> { "Outline" }, colorFormat)
        };
        customDepthTarget = new CustomDepthTarget(false, "_OutlineOpaqueDepth", new List<string> { "Outline" });
    }
}

/// <inheritdoc />
[Serializable]
public class ShaderPassToRT : ScriptableRenderPass
{
    private ShaderPassToRTSettings _settings; 
    private List<ShaderTagId> _shaderTagIdList = new();
    
    public ShaderPassToRT(ShaderPassToRTSettings passSettings)
    {
        _settings = passSettings;

        var renderQueueRange = _settings.renderQueueType == RenderQueueType.Opaque
            ? RenderQueueRange.opaque
            : RenderQueueRange.transparent;

        _settings.FilteringSettings = new FilteringSettings(renderQueueRange, _settings.layerMask, (uint)_settings.lightLayerMask);
    }

    public void ShaderTagSetup()
    {
        // For each custom color Render Target, add all non-duplicate LightMode tags to _shaderTagIdList
        foreach (var colorTarget in _settings.customColorTargets)
        {
            // for colorTagId, add new ShaderTagId(tempTag) given that colorTagId is not already in _shaderTagIdList.
            foreach (var colorTagId in colorTarget.lightModeTags
                                                  .Select(tempTag => new ShaderTagId(tempTag))
                                                  .Where(colorTagID => !_shaderTagIdList.Contains(colorTagID)))
            {
                _shaderTagIdList.Add(colorTagId);
            }
        }
        // Duplicate tags might not be an issue but this is checking for them as a precaution.

        if (!_settings.customDepthTarget.enabled) return;
        foreach (var depthTagId in _settings.customDepthTarget.lightModeTags)
        {
            _shaderTagIdList.Add(new ShaderTagId(depthTagId));
        }
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        var camTexDesc /*= _settings.CameraRTDescriptor*/ = cameraTextureDescriptor;
        var width = camTexDesc.width;
        var height = camTexDesc.height;
        camTexDesc.msaaSamples     = 1;
        camTexDesc.colorFormat     = _settings.colorFormat;
        camTexDesc.depthBufferBits = _settings.depthBufferBits;

        List<RenderTargetIdentifier> configuredColorAttachments = new();

        // Configure all enabled color attachments.
        foreach (var colorTarget in _settings.customColorTargets)
        {
            if (!colorTarget.enabled) continue;
            cmd.GetTemporaryRT(colorTarget.NameID, camTexDesc, FilterMode.Point);
            configuredColorAttachments.Add(colorTarget.RTID);
        }

        if (_settings.customDepthTarget.enabled)
        {
            // renderTextureFormat is auto-set to depth by CustomDepthTarget constructor
            cmd.GetTemporaryRT(_settings.customDepthTarget.NameID, width, height, _settings.depthBufferBits, FilterMode.Point, _settings.customDepthTarget.renderTextureFormat);
        }

        if (configuredColorAttachments.Count == 0 && !_settings.customDepthTarget.enabled) return;
        if (_settings.customDepthTarget.enabled)
        {
            ConfigureTarget(configuredColorAttachments.ToArray(), _settings.customDepthTarget.RTID);
            ConfigureClear(ClearFlag.All, clearColor);
        }
        else
        {
            ConfigureTarget(configuredColorAttachments.ToArray());
            ConfigureClear(ClearFlag.Color, clearColor);
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(_settings.profilerTag);

        SortingCriteria sortingCriteria =
            _settings.renderQueueType == RenderQueueType.Opaque ? renderingData.cameraData.defaultOpaqueSortFlags
                                                                : SortingCriteria.CommonTransparent;

        DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);
       
        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _settings.FilteringSettings);

        // Test: Make custom pass textures available after temp RT is released
        /*
        foreach (CustomPassTarget colorTarget in _settings.customColorTargets)
        {
            if (!colorTarget.enabled) continue;
            cmd.SetGlobalTexture(colorTarget.RTIntId, colorTarget.RTIdentifier);
        }
        */

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        foreach (var colorTarget in _settings.customColorTargets)
        {
            if (colorTarget.enabled) cmd.ReleaseTemporaryRT(colorTarget.NameID);
        }

        if (_settings.customDepthTarget.enabled) cmd.ReleaseTemporaryRT(_settings.customDepthTarget.NameID);
    }
}