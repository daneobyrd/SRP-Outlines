// This was originally inspired by a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
public enum RenderQueueType
{
    Opaque = 0,
    Transparent = 1
}

[HideInInspector]
public enum CameraTargetMode
{
    Color = 0,
    ColorAndDepth = 1
}

[Serializable]
public class DrawToRTSettings
{
    public bool   enabled     = false;
    public string profilerTag;
    
    public FilteringSettings FilteringSettings; // Not serializable.

    [Header("Filters")]
    public RenderQueueType renderQueueType;

    public LayerMask layerMask = -1;
    public LightLayerEnum lightLayerMask = LightLayerEnum.Everything;

    [HideInInspector]
    public CameraTargetMode cameraTargetMode;
    
    public CustomColorTarget[] customColorTargets;
    public CustomDepthTarget customDepthTarget;

    public DrawToRTSettings(string name, RenderQueueType queueType)
    {
        profilerTag     = name;
        renderQueueType = queueType;

        customColorTargets = Array.Empty<CustomColorTarget>();
        customDepthTarget  = new CustomDepthTarget();
    }
}

[Serializable]
public class DrawToRT : ScriptableRenderPass
{
    private DrawToRTSettings _settings;
    private List<ShaderTagId> _ShaderTagIdList = new();
    
    public DrawToRT(DrawToRTSettings passSettings)
    {
        _settings = passSettings;
        
        var renderQueueRange = _settings.renderQueueType == RenderQueueType.Opaque
            ? RenderQueueRange.opaque
            : RenderQueueRange.transparent;

        _settings.FilteringSettings = new FilteringSettings(renderQueueRange, _settings.layerMask, (uint)_settings.lightLayerMask);
    }
    
    public void ShaderTagSetup()
    {
        foreach (var colorTarget in _settings.customColorTargets)
        {
            foreach (var colorTagId in colorTarget.lightModeTags
                                                  .Select(tempTag => new ShaderTagId(tempTag))
                                                  .Where(colorTagID => !_ShaderTagIdList.Contains(colorTagID)))
            {
                _ShaderTagIdList.Add(colorTagId);
            }
        }
        // Duplicate tags might not be an issue but this is checking for them as a precaution.
    }
    
    /// <summary>
    /// Sets an enum that is then used as a replacement for checking _settings.customTarget.enabled multiple times.
    /// </summary>
    /// <param name="passSettings"></param>
    private void CheckCameraTargetMode(DrawToRTSettings passSettings)
    {
        // Currently no Setting for DepthOnly pass.
        switch (passSettings.customDepthTarget.enabled)
        {
            case true:
                passSettings.cameraTargetMode = CameraTargetMode.ColorAndDepth;
                break;
            case false:
                if (passSettings.customColorTargets.Any(target => target.enabled = true))
                {
                    passSettings.cameraTargetMode = CameraTargetMode.Color;
                }
                break;
        }
    }
    public void Setup()
    {
        CheckCameraTargetMode(_settings);
        ShaderTagSetup();
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        var width = cameraTextureDescriptor.width;
        var height = cameraTextureDescriptor.height;
        cameraTextureDescriptor.msaaSamples = 1;
        
        #region Create Temporary Render Textures

        List<RenderTargetIdentifier> enabledColorAttachments = new();

        // Configure all enabled color attachments.
        foreach (var colorTarget in _settings.customColorTargets.Where(target => target.enabled = true))
        {
            // if (!colorTarget.enabled) continue;
            
            cmd.GetTemporaryRT(colorTarget.GetNameID(), width, height, (int)colorTarget.depthBits, FilterMode.Point, colorTarget.renderTextureFormat);

            // enabledColorAttachments.Add(colorTarget.RTID);
            enabledColorAttachments.Add(colorTarget.GetRTID());
        }

        if (_settings.customDepthTarget.enabled)
        {
            var depthTarget = _settings.customDepthTarget;

            cmd.GetTemporaryRT(depthTarget.GetNameID(), width, height, (int)depthTarget.depthBits, FilterMode.Point, RenderTextureFormat.Depth);
        }
        
        #endregion
        
        // Checking for edge-case where all targets are disabled in the editor.
        if (enabledColorAttachments.Count == 0 && !_settings.customDepthTarget.enabled) return;
        
        var mrt = enabledColorAttachments.ToArray();

        if (_settings.cameraTargetMode == CameraTargetMode.ColorAndDepth)
        {
            // ConfigureTarget(mrt, _settings.customDepthTarget.RTID);
            ConfigureTarget(mrt, _settings.customDepthTarget.GetRTID());
            ConfigureClear(ClearFlag.All, clearColor);
        }
        else //if (_settings.cameraTargetMode == CameraTargetMode.Color)
        {
            ConfigureTarget(mrt);
            ConfigureClear(ClearFlag.Color, clearColor);
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(_settings.profilerTag);

        var cameraData = renderingData.cameraData;
        
        SortingCriteria sortingCriteria =
            _settings.renderQueueType == RenderQueueType.Opaque ? cameraData.defaultOpaqueSortFlags
                                                                : SortingCriteria.CommonTransparent;

        DrawingSettings drawingSettings = CreateDrawingSettings( _ShaderTagIdList, ref renderingData, sortingCriteria);

        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _settings.FilteringSettings);
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        foreach (var colorTarget in _settings.customColorTargets.Where(target => target.enabled = true))
        {
            // if (!colorTarget.enabled) continue;
            // cmd.ReleaseTemporaryRT(colorTarget.NameID);
            cmd.ReleaseTemporaryRT(colorTarget.GetNameID());
        }

        if (_settings.customDepthTarget.enabled)
        {
            // cmd.ReleaseTemporaryRT(_settings.customDepthTarget.NameID);
            cmd.ReleaseTemporaryRT(_settings.customDepthTarget.GetNameID());
        }
    }
}