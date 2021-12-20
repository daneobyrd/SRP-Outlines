// This was originally inspired by of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ShaderPassToRT : ScriptableRenderPass
{
    #region Variables

    private string _profilerTag;
    private OutlineSettings _settings;
    private FilterSettings  filter          => _settings.filterSettings;
    private RenderQueueType renderQueueType => filter.renderQueueType;

    private FilteringSettings _filteringSettings;
    private LineworkSettings linework => _settings.lineworkSettings;

    private List<ShaderTagId> _shaderTagIdList = new() {Capacity = 0};
    private int _texDepthBufferBits;

    // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    #region Color

    private PassSubTarget colorSubTarget => linework.colorSubTarget;

    // -----------------------------------------------------------------------------------------------------------------------------------------------------
    private int                    colorIdInt         => colorSubTarget.renderTargetInt;
    private RenderTargetIdentifier colorTargetId      => colorSubTarget.targetIdentifier;
    private bool                   createColorTexture => colorSubTarget.createTexture;
    private RenderTextureFormat    colorFormat        => colorSubTarget.renderTextureFormat;

    #endregion

    // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    #region Depth

    private PassSubTarget depthSubTarget => linework.depthSubTarget;

    // -------------------------------------------------------------------------------------------------------------------------------------------
    private int                    depthIdInt    => depthSubTarget.renderTargetInt;
    private RenderTargetIdentifier depthTargetId => depthSubTarget.targetIdentifier;

    private bool createDepthTexture => depthSubTarget.createTexture;
    // private RenderTextureFormat depthFormat => colorSubTarget.renderTextureFormat; // Auto-set by PassSubTarget constructor (TargetType)

    #endregion

    // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    #endregion

    public ShaderPassToRT(OutlineSettings settings, string profilerTag, RenderPassEvent renderPassEvent, int depthBufferBits)
    {
        _settings    = settings;
        _profilerTag = profilerTag;

        // Temporary way to allow for setting all layer masks using default value (-1).
        var lightLayerMask = (uint) filter.lightLayerMask;
        if (filter.lightLayerMask == -1)
        {
            lightLayerMask = uint.MaxValue; // uint.MaxValue = all light layers
        }
        // TODO: Create interface/editor class for renderer feature and render passes

        this.renderPassEvent = renderPassEvent;
        var renderQueueRange = filter.renderQueueType
                               == RenderQueueType.Opaque
            ? RenderQueueRange.opaque
            : RenderQueueRange.transparent;

        _filteringSettings = new FilteringSettings(renderQueueRange, filter.layerMask, lightLayerMask);

        _texDepthBufferBits = depthBufferBits;
    }

    public void ShaderTagSetup(bool hasDepth)
    {
        int colorTagCount = colorSubTarget.lightModeTags.Count;
        int depthTagCount = depthSubTarget.lightModeTags.Count;

        var tagCount = hasDepth ? colorTagCount + depthTagCount : colorTagCount;

        if (_shaderTagIdList.Count >= tagCount) return;
        foreach (var colorTag in colorSubTarget.lightModeTags)
        {
            _shaderTagIdList.Add(new ShaderTagId(colorTag));
        }

        if (!hasDepth) return;
        foreach (var depthTag in depthSubTarget.lightModeTags)
        {
            _shaderTagIdList.Add(new ShaderTagId(depthTag));
        }
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
        var width = camTexDesc.width;
        var height = camTexDesc.height;
        camTexDesc.colorFormat     = colorFormat;
        camTexDesc.depthBufferBits = 0;
        camTexDesc.useMipMap       = false;


        List<RenderTargetIdentifier> colorAttConfigList = new() /*{ _cameraColorIdentifier }*/;
        if (createColorTexture)
        {
            cmd.GetTemporaryRT(colorIdInt, camTexDesc, FilterMode.Point);
            colorAttConfigList.Add(colorTargetId);
        }

        if (createDepthTexture)
        {
            cmd.GetTemporaryRT(depthIdInt, width, height, _texDepthBufferBits, FilterMode.Point, RenderTextureFormat.Depth);
        }

        switch (createColorTexture, createDepthTexture)
        {
            case (true, true):
                ConfigureTarget(colorAttConfigList.ToArray(), depthTargetId);
                ConfigureClear(ClearFlag.All, clearColor);
                break;
            case (true, false):
                // ConfigureInput(ScriptableRenderPassInput.Color);
                ConfigureTarget(colorAttConfigList.ToArray());
                ConfigureClear(ClearFlag.Color, clearColor);
                break;
            default:
                ConfigureTarget(colorAttConfigList.ToArray());
                ConfigureClear(ClearFlag.Color, clearColor);
                break;
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

        var cameraData = renderingData.cameraData;
        var camera = renderingData.cameraData.camera;
        var cullingResults = renderingData.cullResults;

        cameraData.antialiasing = AntialiasingMode.None;
        
        SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque
            ? renderingData.cameraData.defaultOpaqueSortFlags
            : SortingCriteria.CommonTransparent;

        DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        if (createColorTexture) cmd.ReleaseTemporaryRT(colorIdInt);
        if (createDepthTexture) cmd.ReleaseTemporaryRT(depthIdInt);
    }
}