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
    public RenderQueueType renderQueueType;
    
    // TODO: Figure out the right way to display FilteringSettings in the inspector.
    public FilteringSettings FilteringSettings;

    public LayerMask layerMask;
    public uint lightLayerMask;
    
    public RenderTextureFormat colorFormat;
    public int depthBufferBits;

    public CustomPassTarget[] customColorTargets;
    public CustomPassTarget customDepthTarget;

    public ShaderPassToRTSettings(string name, RenderQueueType queueType)
    {
        profilerTag     = name;
        renderQueueType = queueType;
        // FilteringSettings ;
        colorFormat     = RenderTextureFormat.ARGBFloat;
        depthBufferBits = 16;

        // RenderTargets
        customColorTargets = new[]
        {
            new CustomPassTarget(new List<string> { "Outline" }, "_CustomOpaqueColor", CustomPassTargetType.Color, true, colorFormat)
        };
        customDepthTarget  = new CustomPassTarget(new List<string> { "Outline" }, "_CustomOpaqueDepth", CustomPassTargetType.Color, false);
    }
}

[Serializable]
public class ShaderPassToRT : ScriptableRenderPass
{
    #region Variables

    private ShaderPassToRTSettings _settings; 
    private List<ShaderTagId> _shaderTagIdList = new();
    
    #endregion

    public ShaderPassToRT(ShaderPassToRTSettings passSettings)
    {
        _settings = passSettings;

        var renderQueueRange = _settings.renderQueueType == RenderQueueType.Opaque
            ? RenderQueueRange.opaque
            : RenderQueueRange.transparent;

        _settings.FilteringSettings = new FilteringSettings(renderQueueRange, _settings.layerMask, _settings.lightLayerMask);
    }

    public void ShaderTagSetup()
    {
        // For each custom color Render Target, add all non-duplicate LightMode tags to _shaderTagIdList
        foreach (var colorTarget in _settings.customColorTargets)
        {
            // for colorTagId, add new ShaderTagId(tempTag) given that colorTagId is not already in _shaderTagIdList.
            foreach (var colorTagId in colorTarget.lightModeTags.Select(tempTag => new ShaderTagId(tempTag))
                                                  .Where(colorTagID => !_shaderTagIdList.Contains(colorTagID)))
            {
                _shaderTagIdList.Add(colorTagId);
            }
        }

        if (!_settings.customDepthTarget.enabled) return;
        foreach (var depthTagId in _settings.customDepthTarget.lightModeTags)
        {
            _shaderTagIdList.Add(new ShaderTagId(depthTagId));
        }
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
        var width = camTexDesc.width;
        var height = camTexDesc.height;
        camTexDesc.colorFormat     = _settings.colorFormat;
        camTexDesc.depthBufferBits = _settings.depthBufferBits;
        camTexDesc.useMipMap       = false;

        List<RenderTargetIdentifier> configuredColorAttachments = new();

        // Configure all enabled color attachments.
        foreach (var colorTarget in _settings.customColorTargets)
        {
            if (!colorTarget.enabled) continue;
            cmd.GetTemporaryRT(colorTarget.RTIntId, camTexDesc, FilterMode.Point);
            configuredColorAttachments.Add(colorTarget.RTIdentifier);
        }

        if (_settings.customDepthTarget.enabled)
        {
            cmd.GetTemporaryRT(_settings.customDepthTarget.RTIntId, width, height, _settings.depthBufferBits, FilterMode.Point,
                               RenderTextureFormat.Depth);
        }

        if (configuredColorAttachments.Count == 0 && !_settings.customDepthTarget.enabled) return;
        if (_settings.customDepthTarget.enabled)
        {
            ConfigureTarget(configuredColorAttachments.ToArray(), _settings.customDepthTarget.RTIdentifier);
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

        SortingCriteria sortingCriteria = _settings.renderQueueType == RenderQueueType.Opaque
            ? renderingData.cameraData.defaultOpaqueSortFlags
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
        // foreach (var colorTarget in _settings.customColorTargets)
        // {
        //     if (colorTarget.enabled) cmd.ReleaseTemporaryRT(colorTarget.RTIntId);
        // }

        if (_settings.customColorTargets[0].enabled) cmd.ReleaseTemporaryRT(_settings.customColorTargets[0].RTIntId);
        if (_settings.customDepthTarget.enabled) cmd.ReleaseTemporaryRT(_settings.customDepthTarget.RTIntId);
    }
}