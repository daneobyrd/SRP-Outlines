// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resources.RenderPass.OutlineBuffers
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
            this.createTexture = createTexture;
            renderTargetInt = Shader.PropertyToID(textureName);
            TargetIdentifier = new RenderTargetIdentifier(renderTargetInt);
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
        private RenderTextureFormat depthFormat => colorSubTarget.renderTextureFormat; // Redundant but exists for consistency.

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        private RenderTextureDescriptor _cameraTextureDescriptor;
        
        #endregion

        // Must call Init before enqueuing pass
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
            _cameraTextureDescriptor = cameraTextureDescriptor;
            var width = _cameraTextureDescriptor.width;
            var height = _cameraTextureDescriptor.height;
            
            List<RenderTargetIdentifier> attachmentsToConfigure = new();

            cmd.GetTemporaryRT(nameID: colorIdInt,
                               width: width,
                               height: height,
                               depthBuffer: _textureDepthBufferBits,
                               filter: FilterMode.Point,
                               format: colorFormat);
            if (createColorTexture) attachmentsToConfigure.Add(colorIdInt);

            // Create temporary render texture to store outline opaque objects' depth in new global texture "_OutlineDepth".
            cmd.GetTemporaryRT(nameID: depthIdInt, 
                               width: width,
                               height: height,
                               depthBuffer: _textureDepthBufferBits,
                               filter: FilterMode.Point,
                               format: depthFormat);
            if (createDepthTexture) attachmentsToConfigure.Add(depthIdInt);
            
            // Configure color and depth targets
            ConfigureTarget(attachmentsToConfigure.ToArray());
            
            // Clear
            if (createColorTexture || createDepthTexture)
                ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            // _cameraTextureDescriptor.enableRandomWrite = true;

            SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
            
            // Set Global Textures (for... debug?). This may be deprecated.
            // if (createColorTexture)
            // {
            //     cmd.SetGlobalTexture(colorIdInt, colorTargetId, RenderTextureSubElement.Color);
            // }
            // if (createDepthTexture)
            // {
            //     cmd.SetGlobalTexture(depthIdInt, depthTargetId, RenderTextureSubElement.Depth);
            // }

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