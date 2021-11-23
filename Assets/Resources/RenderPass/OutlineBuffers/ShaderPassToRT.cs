// This was originally inspired by of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderPass.OutlineBuffers
{
    public enum TargetType
    {
        Color,
        Depth,
        Shadowmap
    }
    
    [System.Serializable]
    public struct PassSubTarget
    {
        public List<string> lightmodeTags;
        public string textureName;
        [HideInInspector] public int renderTargetInt;
        public RenderTargetIdentifier TargetIdentifier;
        public bool createTexture;
        public RenderTextureFormat renderTextureFormat;

        public PassSubTarget(List<string> lightmodeTags, string texName, TargetType type, bool createTexture, RenderTextureFormat rtFormat)
        {
            this.lightmodeTags = lightmodeTags;
            textureName = texName;
            renderTargetInt = Shader.PropertyToID(textureName);
            TargetIdentifier = new RenderTargetIdentifier(renderTargetInt);
            this.createTexture = createTexture;
            renderTextureFormat = type switch
            {
                TargetType.Color => rtFormat,
                TargetType.Depth => RenderTextureFormat.Depth,
                TargetType.Shadowmap => RenderTextureFormat.Shadowmap,
                _ => rtFormat
            };
        }
    }

    public class ShaderPassToRT : ScriptableRenderPass
    {
        #region Variables

        private RenderQueueType renderQueueType => filter.renderQueueType;
        private FilteringSettings _filteringSettings;

        private OutlineSettings _settings;
        private FilterSettings filter => _settings.filterSettings;
        private LineworkSettings linework => _settings.lineworkSettings;

        private string _profilerTag;

        private List<ShaderTagId> _shaderTagIdList = new();
        private int _textureDepthBufferBits;

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private PassSubTarget colorSubTarget => linework.colorSubTarget;
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private int colorIdInt => colorSubTarget.renderTargetInt;
        private RenderTargetIdentifier colorTargetId => colorSubTarget.TargetIdentifier;
        private bool createColorTexture => colorSubTarget.createTexture;
        private RenderTextureFormat colorFormat => colorSubTarget.renderTextureFormat;

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private PassSubTarget depthSubTarget => linework.depthSubTarget;
        // -------------------------------------------------------------------------------------------------------------------------------------------
        private int depthIdInt => depthSubTarget.renderTargetInt;
        private RenderTargetIdentifier depthTargetId => depthSubTarget.TargetIdentifier;
        private bool createDepthTexture => depthSubTarget.createTexture;
        // private RenderTextureFormat depthFormat => colorSubTarget.renderTextureFormat; // Redundant but exists for clarity.

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        
        #endregion

        // Must call Init before enqueuing pass.
        public void Init(bool hasDepth)
        {
            var totalShaderNames = new List<string>();

            totalShaderNames.AddRange(colorSubTarget.lightmodeTags);
            if (hasDepth)
            {
                totalShaderNames.AddRange(depthSubTarget.lightmodeTags);
            }

            if (_shaderTagIdList.Count >= totalShaderNames.Count) return;
            foreach (var colorTag in colorSubTarget.lightmodeTags)
            {
                _shaderTagIdList.Add(new ShaderTagId(colorTag));
            }

            if (!hasDepth) return;
            foreach (var depthTag in depthSubTarget.lightmodeTags)
            {
                _shaderTagIdList.Add(new ShaderTagId(depthTag));
            }
        }

        public ShaderPassToRT(OutlineSettings settings, string profilerTag, ComputeShader computeShader, RenderPassEvent renderPassEvent,
            int depthBufferBits)
        {
            _settings = settings;
            _profilerTag = profilerTag;
            this.renderPassEvent = renderPassEvent;
            var renderQueueRange = (filter.renderQueueType == RenderQueueType.Opaque) ? RenderQueueRange.opaque : RenderQueueRange.transparent;
            _filteringSettings = new FilteringSettings(renderQueueRange, filter.layerMask);
            _textureDepthBufferBits = depthBufferBits;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
            camTexDesc.colorFormat = colorFormat;
            camTexDesc.depthBufferBits = _textureDepthBufferBits;
            camTexDesc.msaaSamples = 1;
            camTexDesc.bindMS = true;
            
            // List<RenderTargetIdentifier> attachmentsToConfigure = new();

            cmd.GetTemporaryRT(colorIdInt, camTexDesc);
            // if (createColorTexture) attachmentsToConfigure.Add(colorTargetId); // Recently changed from int to RTIdentifier

            // Create temporary render texture to store outline opaque objects' depth in new global texture "_OutlineDepth".
            cmd.GetTemporaryRT(depthIdInt, camTexDesc);
            // if (createDepthTexture) attachmentsToConfigure.Add(depthTargetId); // Recently changed from int to RTIdentifier
            
            // Configure color and depth targets
            ConfigureTarget(colorTargetId, depthTargetId); // Changed to explicit instead of attachmentsToConfigure.ToArray() for debug.
            
            // Clear
            if (createColorTexture || createDepthTexture)
                ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            
            SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
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
}