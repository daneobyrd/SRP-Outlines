using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Shaders.OutlineBuffers
{
    [System.Serializable]
    public class PassSubTarget
    {
        public string shaderName;
        public RenderTargetIdentifier TargetIdentifier;
        public RenderTargetHandle TargetHandle;
        public bool createTexture;
        public RenderTextureFormat renderTextureFormat;

        public PassSubTarget(string shaderName, bool createTexture, bool isDepth, RenderTextureFormat rtFormat)
        {
            this.createTexture = createTexture;
            TargetIdentifier = new RenderTargetIdentifier(shaderName);
            TargetHandle = new RenderTargetHandle(TargetIdentifier);
            // Moved to Configure() // targetHandle.Init(targetIdentifier);
            this.renderTextureFormat = isDepth ? RenderTextureFormat.Depth : rtFormat;
        }
    }

    public class ShaderPassToRT : ScriptableRenderPass
    {
        #region Variables

        private OutlineSettings _outlineSettings;

        private RenderQueueType _renderQueueType;
        private FilteringSettings _filteringSettings;
        private string _profilerTag;

        private ShaderTagId _shaderTagId;
        private int _textureDepthBufferBits;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private PassSubTarget _colorConfig;
        private RenderTargetHandle colorHandle => _colorConfig.TargetHandle;
        private RenderTargetIdentifier colorTargetId => _colorConfig.TargetIdentifier;
        private bool createColorTexture => _colorConfig.createTexture;
        private RenderTextureFormat colorFormat => _colorConfig.renderTextureFormat;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private PassSubTarget _depthConfig;
        private RenderTargetHandle depthHandle => _depthConfig.TargetHandle;
        private RenderTargetIdentifier depthTargetId => _depthConfig.TargetIdentifier;
        private bool createDepthTexture => _depthConfig.createTexture;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private ComputeShader _computeShader;

        private RenderTextureDescriptor _cameraTextureDescriptor;
        private RenderTargetHandle _blurHandle;

        #endregion

        public ShaderPassToRT(OutlineSettings settings, string profilerTag, PassSubTarget colorConfig, PassSubTarget depthConfig,
            RenderPassEvent renderPassEvent, RenderQueueType renderQueueType, int depthBufferBits)
        {
            _profilerTag = profilerTag;
            _outlineSettings = settings;
            _colorConfig = colorConfig;
            _depthConfig = depthConfig;
            this.renderPassEvent = renderPassEvent;
            _renderQueueType = renderQueueType;
            var renderQueueRange = (renderQueueType == RenderQueueType.Opaque)
                ? RenderQueueRange.opaque
                : RenderQueueRange.transparent;
            _filteringSettings = new FilteringSettings(renderQueueRange, ~0);
            _textureDepthBufferBits = depthBufferBits;
        }

        public ShaderPassToRT(OutlineSettings settings, int depthBufferBits)
        {
            _profilerTag = settings.profilerTag;
            _outlineSettings = settings;
            _colorConfig = settings.colorTarget;
            _depthConfig = settings.depthTarget;
            renderPassEvent = settings.renderPassEvent;
            _renderQueueType = settings.renderQueueType;
            var renderQueueRange = (settings.renderQueueType == RenderQueueType.Opaque)
                ? RenderQueueRange.opaque
                : RenderQueueRange.transparent;
            _filteringSettings = new FilteringSettings(renderQueueRange, ~0);
            _textureDepthBufferBits = depthBufferBits;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            colorHandle.Init(colorTargetId);
            depthHandle.Init(depthTargetId);
            _computeShader = _outlineSettings.computeBlur;
            
            // TODO: Unify use of camSize or _cameraTextureDescription for width/height values passed to render textures.
            _cameraTextureDescriptor = cameraTextureDescriptor;
            cameraTextureDescriptor.enableRandomWrite = true;

            if (createColorTexture)
            {
                cmd.GetTemporaryRTArray(colorHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 4, _textureDepthBufferBits,
                    FilterMode.Point, colorFormat, RenderTextureReadWrite.Default, 1, cameraTextureDescriptor.enableRandomWrite);
            }

            if (createDepthTexture)
            {
                cmd.GetTemporaryRTArray(depthHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 4, _textureDepthBufferBits,
                    FilterMode.Point, RenderTextureFormat.Depth);
            }

            ConfigureTarget(colorHandle.id, depthHandle.id);

            // cmd.CopyTexture(DepthTargetId, (int)RenderTextureSubElement.Depth,ColorTargetId, (int)RenderTextureSubElement.Depth);

            if (createColorTexture || createDepthTexture)
            {
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // TODO: Unify use of camSize or _cameraTextureDescription for width/height values passed to render textures.
            var camData = renderingData.cameraData.camera;
            Vector4 camSize = new Vector4(camData.pixelWidth, camData.pixelHeight, 0, 0);

            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            SortingCriteria sortingCriteria = _renderQueueType == RenderQueueType.Opaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagId, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

            cmd.SetComputeTextureParam(_computeShader, 0, "_Source", colorHandle.id, 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(_computeShader, 0, "_Source", depthHandle.id, 0, RenderTextureSubElement.Depth);
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
            cmd.SetComputeTextureParam(_computeShader, 1, "_Destination", _blurHandle.id);

            cmd.DispatchCompute(_computeShader, 0 /*KColorGaussian*/, 8, 8, 1);
            cmd.DispatchCompute(_computeShader, 1 /*KColorDownsample*/, 8, 8, 1);

            cmd.SetGlobalTexture("_BlurResults", _blurHandle.id);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (createColorTexture) cmd.ReleaseTemporaryRT(colorHandle.id);
            if (createDepthTexture) cmd.ReleaseTemporaryRT(depthHandle.id);
        }
    }
}