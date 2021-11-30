// This was originally inspired by of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ReSharper disable once CheckNamespace
namespace RenderPass.OutlineBuffers
{
    public class ShaderPassToRT : ScriptableRenderPass
    {
        #region Variables

        private OutlineSettings _settings;
        private FilterSettings filter => _settings.filterSettings;

        private RenderQueueType renderQueueType => filter.renderQueueType;

        private FilteringSettings _filteringSettings;

        private LineworkSettings linework => _settings.lineworkSettings;

        private string _profilerTag;

        // Rider initializes this by setting Capacity = 0.
        private List<ShaderTagId> _shaderTagIdList = new() { Capacity = 0 };
        private int _textureDepthBufferBits = 0;

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private PassSubTarget colorSubTarget => linework.colorSubTarget;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private int colorIdInt => colorSubTarget.renderTargetInt;
        private RenderTargetIdentifier colorTargetId => colorSubTarget.targetIdentifier;
        private bool createColorTexture => colorSubTarget.createTexture;
        private RenderTextureFormat colorFormat => colorSubTarget.renderTextureFormat;

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private PassSubTarget depthSubTarget => linework.depthSubTarget;
        // -------------------------------------------------------------------------------------------------------------------------------------------
        private int depthIdInt => depthSubTarget.renderTargetInt;
        private RenderTargetIdentifier depthTargetId => depthSubTarget.targetIdentifier;
        private bool createDepthTexture => depthSubTarget.createTexture;
        // private RenderTextureFormat depthFormat => colorSubTarget.renderTextureFormat; // Auto-set by PassSubTarget constructor (TargetType)

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        
        #endregion

        // Must call ShaderTagSetup before enqueuing pass.
        public void ShaderTagSetup(bool hasDepth)
        {
            var totalShaderNames = new List<string>();

            totalShaderNames.AddRange(colorSubTarget.lightModeTags);
            if (hasDepth)
            {
                totalShaderNames.AddRange(depthSubTarget.lightModeTags);
            }

            if (_shaderTagIdList.Count >= totalShaderNames.Count) return;
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

        public ShaderPassToRT(OutlineSettings settings, string profilerTag, RenderPassEvent renderPassEvent, int depthBufferBits)
        {
            _settings = settings;
            _profilerTag = profilerTag;

            // Temporary way to allow for setting all layer masks using default value (-1).
            var lightLayerMask = (uint)filter.lightLayerMask;
            if (filter.lightLayerMask == -1)
            {
                lightLayerMask = uint.MaxValue;
            }
            // TODO: Create interface/editor class for renderer feature and render passes
            
            this.renderPassEvent = renderPassEvent;
            var renderQueueRange = (filter.renderQueueType == RenderQueueType.Opaque) ? RenderQueueRange.opaque : RenderQueueRange.transparent;
            _filteringSettings = new FilteringSettings(renderQueueRange, filter.layerMask, lightLayerMask);
            
            _textureDepthBufferBits = depthBufferBits;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
            var width = camTexDesc.width;
            var height = camTexDesc.height;
            camTexDesc.colorFormat = colorFormat;
            // camTexDesc.graphicsFormat = (GraphicsFormat) colorFormat;
            camTexDesc.depthBufferBits = _textureDepthBufferBits;
            // camTexDesc.msaaSamples = 1;
            camTexDesc.dimension = TextureDimension.Tex2DArray;
            
            List<RenderTargetIdentifier> colorAttachmentsToConfigure = new();

            cmd.GetTemporaryRT(colorIdInt, camTexDesc, FilterMode.Point);
            if (createColorTexture) colorAttachmentsToConfigure.Add(colorTargetId); // Recently changed from int to RTIdentifier

            // Create temporary render texture to store outline opaque objects' depth in new global texture "_OutlineDepth".
            cmd.GetTemporaryRT(depthIdInt, width, height, _textureDepthBufferBits, FilterMode.Point, RenderTextureFormat.Depth);

            // switch (createColorTexture)
            // {
            //     case true when createDepthTexture:
            //         // Configure color and depth targets
            //         ConfigureTarget(colorAttachmentsToConfigure.ToArray(), depthTargetId);
            //         break;
            //     case true when !createDepthTexture:
            //         ConfigureTarget(colorAttachmentsToConfigure.ToArray());
            //         break;
            // }
            if (createColorTexture)
            {
                ConfigureTarget(colorTargetId);
            }
            
            // Clear
            if (createColorTexture || createDepthTexture)
            {
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            renderingData.cameraData.cameraTargetDescriptor.enableRandomWrite = true;
            
            SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (createColorTexture) cmd.ReleaseTemporaryRT(colorIdInt);
            if (createDepthTexture) cmd.ReleaseTemporaryRT(depthIdInt);
        }
    }
}