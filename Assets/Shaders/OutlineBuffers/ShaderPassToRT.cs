using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Shaders.OutlineBuffers
{
    [System.Serializable]
    public struct PassSubTarget
    {
        public string shaderName;
        public RenderTargetIdentifier TargetIdentifier;
        public RenderTargetHandle TargetHandle;
        public bool createTexture;
        public RenderTextureFormat renderTextureFormat;

        public PassSubTarget(string shaderName, bool createTexture, bool isDepth, RenderTextureFormat rtFormat)
        {
            this.shaderName = shaderName;
            this.createTexture = createTexture;
            TargetIdentifier = new RenderTargetIdentifier(shaderName);
            TargetHandle = new RenderTargetHandle(TargetIdentifier);
            // TargetHandle.Init(TargetIdentifier);
            renderTextureFormat = isDepth ? RenderTextureFormat.Depth : rtFormat;
        }
    }

    public class ShaderPassToRT : ScriptableRenderPass
    {
        #region Variables

        private RenderQueueType renderQueueType => filter.renderQueueType;
        private FilteringSettings m_FilteringSettings;

        private OutlineSettings m_OutlineSettings;
        private FilterSettings filter => m_OutlineSettings.filterSettings;
        private LineworkSettings linework => m_OutlineSettings.lineworkSettings;
        private EdgeDetectionSettings edge => m_OutlineSettings.edgeSettings;

        private string m_ProfilerTag;

        private List<ShaderTagId> m_ShaderTagIdList = new();
        private int _textureDepthBufferBits;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget colorTargetConfig => linework.colorSubTarget;
        private RenderTargetHandle colorHandle => colorTargetConfig.TargetHandle;
        private RenderTargetIdentifier colorTargetId => colorTargetConfig.TargetIdentifier;
        private bool createColorTexture => colorTargetConfig.createTexture;
        private RenderTextureFormat colorFormat => colorTargetConfig.renderTextureFormat;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget depthTargetConfig => linework.depthSubTarget;
        private RenderTargetHandle depthHandle => depthTargetConfig.TargetHandle;
        private RenderTargetIdentifier depthTargetId => depthTargetConfig.TargetIdentifier;
        private bool createDepthTexture => depthTargetConfig.createTexture;
        // private RenderTextureFormat depthFormat => RenderTextureFormat.Depth;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private ComputeShader computeShader => edge.computeBlur;

        private RenderTextureDescriptor m_CameraTextureDescriptor;
        private RenderTargetHandle _blurHandle;

        #endregion

        public ShaderPassToRT(OutlineSettings settings, string profilerTag, RenderPassEvent renderPassEvent, int depthBufferBits)
        {
            m_OutlineSettings = settings;
            m_ProfilerTag = profilerTag;
            m_ShaderTagIdList.Add(new ShaderTagId(linework.colorSubTarget.shaderName));
            m_ShaderTagIdList.Add(new ShaderTagId(linework.depthSubTarget.shaderName));

            colorHandle.Init(colorTargetId);
            depthHandle.Init(depthTargetId);
            // colorTargetConfig = linework.colorSubTarget;
            // depthTargetConfig = linework.depthSubTarget;
            // computeShader = edge.computeBlur;

            this.renderPassEvent = renderPassEvent;
            var renderQueueRange = (filter.renderQueueType == RenderQueueType.Opaque) ? RenderQueueRange.opaque : RenderQueueRange.transparent;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, filter.layerMask);
            _textureDepthBufferBits = depthBufferBits;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_CameraTextureDescriptor = cameraTextureDescriptor;
            m_CameraTextureDescriptor.enableRandomWrite = true;

            if (createColorTexture)
            {
                cmd.GetTemporaryRTArray(colorHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 4, _textureDepthBufferBits,
                    FilterMode.Point, colorFormat, RenderTextureReadWrite.Default, 1, cameraTextureDescriptor.enableRandomWrite);
            }

            if (createDepthTexture)
            {
                cmd.GetTemporaryRT(depthHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, _textureDepthBufferBits,
                    FilterMode.Point, RenderTextureFormat.Depth);
            }
            
            ConfigureTarget(colorHandle.id, depthHandle.id);

            if (createColorTexture || createDepthTexture)
            {
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            m_CameraTextureDescriptor.enableRandomWrite = true;
            Vector4 camSize = new Vector4(m_CameraTextureDescriptor.width, m_CameraTextureDescriptor.height, 0, 0);

            SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);

            computeShader.EnableKeyword("COPY_MIP_0");
            cmd.SetComputeTextureParam(computeShader, 0, "_Source", colorHandle.Identifier(), 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(computeShader, 0, "_Source", depthHandle.Identifier(), 0, RenderTextureSubElement.Depth);

            cmd.SetComputeVectorParam(computeShader, "_Size", camSize);

            cmd.SetComputeTextureParam(computeShader, 1, "_Destination", _blurHandle.Identifier(),0);

            cmd.DispatchCompute(computeShader, 0 /*KColorGaussian*/, 8, 8, 1);
            cmd.DispatchCompute(computeShader, 1 /*KColorDownsample*/, 8, 8, 1);

            cmd.SetGlobalTexture("_BlurResults", _blurHandle.id);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (createColorTexture) cmd.ReleaseTemporaryRT(colorHandle.id);
            if (createDepthTexture) cmd.ReleaseTemporaryRT(depthHandle.id);
            // cmd.ReleaseTemporaryRT(_blurHandle.id);
        }
    }
}