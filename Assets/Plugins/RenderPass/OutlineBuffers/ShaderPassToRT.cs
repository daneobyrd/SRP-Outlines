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

public enum RenderQueueType
{
    Opaque = 0,
    Transparent = 1
}

[Serializable]
public class ShaderPassToRT : ScriptableRenderPass
{
    [Serializable]
    public class ShaderPassToRTSettings
    {
        public bool enabled;
        public string profilerTag;
        public RenderQueueType renderQueueType;
        public FilteringSettings FilteringSettings;
        public RenderTextureFormat colorFormat;
        public int depthBufferBits;

        public CustomPassTarget[] customColorTargets;
        public CustomPassTarget customDepthTarget;

        public ShaderPassToRTSettings(string name, RenderQueueType queueType)
        {
            profilerTag       = name;
            renderQueueType   = queueType;
            FilteringSettings = new FilteringSettings();
            colorFormat       = RenderTextureFormat.ARGBFloat;
            depthBufferBits   = 16;

            // RenderTargets
            customColorTargets = new[]
            {
                new CustomPassTarget(new List<string> {"Outline"},
                                     "_CustomColor",
                                     CustomPassTargetType.Color,
                                     true,
                                     colorFormat)
            };
            
            customDepthTarget =
                new CustomPassTarget(new List<string> {"Outline"},
                                     "_CustomDepth",
                                     CustomPassTargetType.Depth,
                                     false);
        }
    }

    #region Variables

    public ShaderPassToRTSettings settings;
    private string _profilerTag;
    private List<ShaderTagId> _shaderTagIdList = new();
    private int DepthBufferBits => settings.depthBufferBits;


    #region Render Targets

    private CustomPassTarget CustomColorRT0 => settings.customColorTargets[0];
    private CustomPassTarget CustomColorRT1 => settings.customColorTargets[1];
    private CustomPassTarget CustomColorRT2 => settings.customColorTargets[2];
    private CustomPassTarget CustomColorRT3 => settings.customColorTargets[3];
    private CustomPassTarget CustomColorRT4 => settings.customColorTargets[4];
    private CustomPassTarget CustomColorRT5 => settings.customColorTargets[5];
    private CustomPassTarget CustomColorRT6 => settings.customColorTargets[6];
    private CustomPassTarget CustomColorRT7 => settings.customColorTargets[7];

    private CustomPassTarget[] CustomColorAttachments => new[]
    {
        CustomColorRT0,
        CustomColorRT1,
        CustomColorRT2,
        CustomColorRT3,
        CustomColorRT4,
        CustomColorRT5,
        CustomColorRT6,
        CustomColorRT7
    };
    
    private CustomPassTarget CustomDepthTarget => settings.customDepthTarget;
    private bool             DepthEnabled      => CustomDepthTarget.enabled;

    #endregion
    
    #endregion

    public ShaderPassToRT(ShaderPassToRTSettings passSettings)
    {
        settings     = passSettings;
        _profilerTag = settings.profilerTag;

        var renderQueueRange = settings.renderQueueType == RenderQueueType.Opaque
            ? RenderQueueRange.opaque
            : RenderQueueRange.transparent;

        settings.FilteringSettings = new FilteringSettings(renderQueueRange, default, default);
    }

    public void ShaderTagSetup()
    {
        // For each custom color Render Target, add all non-duplicate LightMode tags to _shaderTagIdList
        foreach (var colorTarget in CustomColorAttachments)
        {
            // for colorTagId, add new ShaderTagId(tempTag) given that colorTagId is not already in _shaderTagIdList.
            foreach (var colorTagId in colorTarget.lightModeTags.Select(tempTag => new ShaderTagId(tempTag))
                                                  .Where(colorTagID => !_shaderTagIdList.Contains(colorTagID)))
            {
                _shaderTagIdList.Add(colorTagId);
            }
        }

        if (!CustomDepthTarget.enabled) return;
        foreach (var depthTagId in CustomDepthTarget.lightModeTags)
        {
            _shaderTagIdList.Add(new ShaderTagId(depthTagId));
        }
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
        var width = camTexDesc.width;
        var height = camTexDesc.height;
        camTexDesc.colorFormat     = settings.colorFormat;
        camTexDesc.depthBufferBits = DepthBufferBits;
        camTexDesc.useMipMap       = false;

        List<RenderTargetIdentifier> configuredColorAttachments = new();

        // Configure all enabled color attachments.
        foreach (var colorTarget in settings.customColorTargets)
        {
            if (!colorTarget.enabled) continue;
            cmd.GetTemporaryRT(colorTarget.RTIntId, camTexDesc, FilterMode.Point);
            configuredColorAttachments.Add(colorTarget.RTIdentifier);
        }

        if (DepthEnabled)
        {
            cmd.GetTemporaryRT(CustomDepthTarget.RTIntId, width, height, DepthBufferBits, FilterMode.Point,
                               RenderTextureFormat.Depth);
        }

        if (configuredColorAttachments.Count == 0 && !DepthEnabled) return;
        if (DepthEnabled)
        {
            ConfigureTarget(configuredColorAttachments.ToArray(), CustomDepthTarget.RTIdentifier);
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
        CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
        
        SortingCriteria sortingCriteria = settings.renderQueueType == RenderQueueType.Opaque
            ? renderingData.cameraData.defaultOpaqueSortFlags
            : SortingCriteria.CommonTransparent;

        DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref settings.FilteringSettings);

        // Test: Make custom pass textures available after temp RT is released
        foreach (var colorTarget in settings.customColorTargets)
        {
            if (!colorTarget.enabled) continue;
            cmd.SetGlobalTexture(colorTarget.textureName, colorTarget.RTIdentifier);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        foreach (var colorTarget in settings.customColorTargets)
        {
            if (colorTarget.enabled) cmd.ReleaseTemporaryRT(colorTarget.RTIntId);
        }

        if (DepthEnabled) cmd.ReleaseTemporaryRT(CustomDepthTarget.RTIntId);
    }
}