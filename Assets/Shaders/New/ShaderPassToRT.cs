using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Shaders.New
{
    [System.Serializable]
    public class PassSubTarget
    {
        public string shaderName;
        public RenderTargetIdentifier targetIdentifier;
        public RenderTargetHandle targetHandle;
        public bool createTexture;
        public RenderTextureFormat _renderTextureFormat;

        public PassSubTarget(string shaderName, bool createTexture, RenderTextureFormat renderTextureFormat)
        {
            targetIdentifier = new RenderTargetIdentifier(shaderName);
            targetHandle = new RenderTargetHandle(targetIdentifier);
            targetHandle.Init(targetIdentifier);
        }
        
        public PassSubTarget(string shaderName, bool createTexture, bool isDepth)
        {
            targetIdentifier = new RenderTargetIdentifier(shaderName);
            targetHandle = new RenderTargetHandle(targetIdentifier);
            targetHandle.Init(targetIdentifier);
            if (isDepth)
            {
                _renderTextureFormat = RenderTextureFormat.Depth;
            }
        }
        
    }

    public class ShaderPassToRT : ScriptableRenderPass
    {
        #region Variables
        private CustomColorBufferOutlines.OutlineSettings _outlineSettings;
        private ComputeShader _computeShader;

        private RenderQueueType _renderQueueType;
        private FilteringSettings _filteringSettings;
        private string _profilerTag;

        private ShaderTagId _shaderTagId;
        private int _textureDepth;

        private PassSubTarget _colorConfig;
        private RenderTargetIdentifier ColorTargetId => _colorConfig.targetIdentifier;
        private RenderTargetHandle ColorHandle => _colorConfig.targetHandle;
        private bool CreateColorTexture => _colorConfig.createTexture;
        private RenderTextureFormat ColorFormat => _colorConfig._renderTextureFormat;

        private PassSubTarget _depthConfig;
        private RenderTargetHandle DepthHandle => _depthConfig.targetHandle;
        private RenderTargetIdentifier DepthTargetId => _depthConfig.targetIdentifier;
        private bool CreateDepthTexture => _depthConfig.createTexture;
        private RenderTextureFormat DepthFormat => _depthConfig._renderTextureFormat;
        #endregion
        
        public ShaderPassToRT(string profilerTag,
                              PassSubTarget colorConfig,
                              PassSubTarget depthConfig,
                              RenderPassEvent renderPassEvent,
                              RenderQueueType renderQueueType,
                              int depth)
        {
            
            _profilerTag = profilerTag;
            
            _colorConfig = colorConfig;
            _depthConfig = depthConfig;
            this.renderPassEvent = renderPassEvent;
            _renderQueueType = renderQueueType;
            var renderQueueRange = (renderQueueType == RenderQueueType.Opaque)
                ? RenderQueueRange.opaque
                : RenderQueueRange.transparent;
            _filteringSettings = new FilteringSettings(renderQueueRange, ~0);
            _textureDepth = depth;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            
            if (CreateColorTexture)
            {
                cmd.GetTemporaryRT(ColorHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, _textureDepth, FilterMode.Point,
                    ColorFormat);
            }

            if (CreateDepthTexture)
            {
                cmd.GetTemporaryRT(DepthHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, _textureDepth, FilterMode.Point,
                    DepthFormat);
            }

            ConfigureTarget(ColorTargetId, DepthTargetId);

            // cmd.CopyTexture(DepthTargetId, (int)RenderTextureSubElement.Depth,ColorTargetId, (int)RenderTextureSubElement.Depth);

            if (CreateColorTexture || CreateDepthTexture)
            {
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camData = renderingData.cameraData.camera;
            Vector4 camSize = new Vector4(camData.scaledPixelWidth, camData.scaledPixelHeight, 0, 0);

            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            SortingCriteria sortingCriteria = _renderQueueType == RenderQueueType.Opaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagId, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

            cmd.SetComputeTextureParam(_computeShader, 0, "_Source", ColorTargetId, 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(_computeShader, 0, "_Source", DepthTargetId, 0, RenderTextureSubElement.Depth);
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
            cmd.DispatchCompute(_computeShader, 0, 8, 8, 1);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (CreateColorTexture) cmd.ReleaseTemporaryRT(ColorHandle.id);
            if (CreateDepthTexture) cmd.ReleaseTemporaryRT(DepthHandle.id);
        }
    }
}