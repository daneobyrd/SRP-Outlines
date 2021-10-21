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
        public RenderTargetHandle TargetHandle;
        public bool createTexture;
        public RenderTextureFormat renderTextureFormat;

        public PassSubTarget(string shaderName, bool createTexture, bool isDepth, RenderTextureFormat rtFormat)
        {
            this.shaderName = shaderName;
            this.createTexture = createTexture;
            TargetHandle = new RenderTargetHandle(shaderName);
            // TargetHandle.Init(shaderName);
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
        private PassSubTarget _colorTargetConfig; // => linework.colorSubTarget;
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private RenderTargetHandle colorHandle => _colorTargetConfig.TargetHandle;
        private bool createColorTexture => _colorTargetConfig.createTexture;
        private RenderTextureFormat colorFormat => _colorTargetConfig.renderTextureFormat;
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget _depthTargetConfig; // => linework.depthSubTarget;
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private RenderTargetHandle depthHandle => _depthTargetConfig.TargetHandle;
        private bool createDepthTexture => _depthTargetConfig.createTexture;
        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private ComputeShader computeShader => edge.computeBlur;

        private RenderTextureDescriptor m_CameraTextureDescriptor;
        private RenderTargetHandle _blurHandle;

        #endregion

        public ShaderPassToRT(OutlineSettings settings, string profilerTag, RenderPassEvent renderPassEvent, int depthBufferBits)
        {
            m_OutlineSettings = settings;
            m_ProfilerTag = profilerTag;
            _colorTargetConfig = linework.colorSubTarget;
            _depthTargetConfig = linework.depthSubTarget;

            colorHandle.Init(_colorTargetConfig.shaderName);
            depthHandle.Init(_depthTargetConfig.shaderName);

            m_ShaderTagIdList.Add(new ShaderTagId(_colorTargetConfig.shaderName));
            m_ShaderTagIdList.Add(new ShaderTagId(_depthTargetConfig.shaderName));
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

            // Create temporary color render texture array to be passed to the ComputeShader
            if (createColorTexture)
            {
                cmd.GetTemporaryRT(colorHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, _textureDepthBufferBits,
                    FilterMode.Bilinear, colorFormat, RenderTextureReadWrite.Default, 1, cameraTextureDescriptor.enableRandomWrite);
            }

            // Create temporary depth render texture to store in ComputeShader "_Source" depth buffer.
            /*
            if (createDepthTexture)
            {
                cmd.GetTemporaryRT(depthHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, _textureDepthBufferBits,
                    FilterMode.Point, RenderTextureFormat.Depth);
            }
            */

            // Create temporary render texture so blurHandle can be used in cmd.SetGlobalTexture()
            cmd.GetTemporaryRT(_blurHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, _textureDepthBufferBits,
                FilterMode.Bilinear, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, cameraTextureDescriptor.msaaSamples,
                cameraTextureDescriptor.enableRandomWrite);
            
            // Configure color and depth targets
            ConfigureTarget(new RenderTargetIdentifier[]{ colorHandle.id, _blurHandle.id }, depthHandle.id);
            if (createColorTexture || createDepthTexture)
            {
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            m_CameraTextureDescriptor.enableRandomWrite = true;
            Vector4 cameraSize = new Vector4(m_CameraTextureDescriptor.width, m_CameraTextureDescriptor.height, 0, 0);

            SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);

            computeShader.EnableKeyword("COPY_MIP_0");
            cmd.SetComputeVectorParam(computeShader, "_Size", cameraSize);

            // KernelIndex: 0 = KColorGaussian
            // KernelIndex: 1 = KColorDownsample
            cmd.SetComputeTextureParam(computeShader, 0, "_Source", colorHandle.Identifier(), 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(computeShader, 0, "_Destination", _blurHandle.Identifier(), 0);
            // cmd.SetComputeTextureParam(computeShader, 0, "_Source", depthHandle.Identifier(), 0, RenderTextureSubElement.Depth);
            cmd.DispatchCompute(computeShader, 0, Mathf.CeilToInt(cameraSize.x / 8f), Mathf.CeilToInt(cameraSize.y / 8f), 1);


            cmd.SetComputeTextureParam(computeShader, 1, "_Source", colorHandle.Identifier(), 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(computeShader, 1, "_Destination", _blurHandle.Identifier(), 0);
            cmd.DispatchCompute(computeShader, 1, Mathf.CeilToInt(cameraSize.x / 8f), Mathf.CeilToInt(cameraSize.y / 8f), 1);

            cmd.SetGlobalTexture("_BlurResults", _blurHandle.id);
            context.ExecuteCommandBuffer(cmd);
            cmd.ReleaseTemporaryRT(_blurHandle.id);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (createColorTexture) cmd.ReleaseTemporaryRT(colorHandle.id);
            if (createDepthTexture) cmd.ReleaseTemporaryRT(depthHandle.id);
        }
    }
}