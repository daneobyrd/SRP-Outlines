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
        public string textureName;
        public RenderTargetHandle TargetHandle;
        public bool createTexture;
        public RenderTextureFormat renderTextureFormat;

        public PassSubTarget(string shaderName, string textureName, bool createTexture, bool isDepth, RenderTextureFormat rtFormat)
        {
            this.shaderName = shaderName;
            this.textureName = textureName;
            this.createTexture = createTexture;
            TargetHandle = new RenderTargetHandle(new RenderTargetIdentifier(textureName));
            TargetHandle.Init(textureName);
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
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private RenderTargetHandle colorHandle => colorTargetConfig.TargetHandle;
        private bool createColorTexture => colorTargetConfig.createTexture;
        private RenderTextureFormat colorFormat => colorTargetConfig.renderTextureFormat;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget depthTargetConfig => linework.depthSubTarget;
        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private RenderTargetHandle depthHandle => depthTargetConfig.TargetHandle;
        private bool createDepthTexture => depthTargetConfig.createTexture;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private ComputeShader computeShader => edge.computeBlur;

        private RenderTextureDescriptor m_CameraTextureDescriptor;
        private RenderTargetHandle _blurHandle;

        #endregion

        public ShaderPassToRT(OutlineSettings settings,
                              string profilerTag,
                              RenderPassEvent renderPassEvent,
                              int depthBufferBits)
        {
            m_OutlineSettings = settings;
            m_ProfilerTag = profilerTag;

            if (m_ShaderTagIdList is { Count: 0 })
            {
                m_ShaderTagIdList.Add(new ShaderTagId(colorTargetConfig.shaderName));
                m_ShaderTagIdList.Add(new ShaderTagId(depthTargetConfig.shaderName));
            }

            this.renderPassEvent = renderPassEvent;
            var renderQueueRange = (filter.renderQueueType == RenderQueueType.Opaque) ? RenderQueueRange.opaque : RenderQueueRange.transparent;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, filter.layerMask);
            _textureDepthBufferBits = depthBufferBits;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_CameraTextureDescriptor = cameraTextureDescriptor;
            m_CameraTextureDescriptor.enableRandomWrite = true;

            List<RenderTargetIdentifier> attachmentsToConfigure = new ();
            
            // Create temporary color render texture array for cmd.SetComputeTextureParam("_Source").
            if (createColorTexture)
            {
                cmd.GetTemporaryRTArray(nameID: colorHandle.id,
                                        width: cameraTextureDescriptor.width,
                                        height: cameraTextureDescriptor.height,
                                        slices: 5,
                                        depthBuffer: _textureDepthBufferBits,
                                        filter: FilterMode.Point,
                                        format: colorFormat,
                                        readWrite: RenderTextureReadWrite.Default,
                                        antiAliasing: 1,
                                        enableRandomWrite: m_CameraTextureDescriptor.enableRandomWrite);
                attachmentsToConfigure.Add(item: colorHandle.id);
            }

            // Create temporary render texture to store outline opaque objects' depth in new global texture "_OutlineDepth".
            if (createDepthTexture)
            {
                cmd.GetTemporaryRT(nameID: depthHandle.id, 
                                   width: cameraTextureDescriptor.width,
                                   height: cameraTextureDescriptor.height,
                                   depthBuffer: _textureDepthBufferBits,
                                   filter: FilterMode.Point,
                                   format: RenderTextureFormat.Depth);
                attachmentsToConfigure.Add(item: depthHandle.id);
            }
            
            // Create temporary render texture so blurHandle can be used in cmd.SetGlobalTexture("_BlurResults").
            cmd.GetTemporaryRTArray(nameID: _blurHandle.id,
                                    width: cameraTextureDescriptor.width,
                                    height: cameraTextureDescriptor.height,
                                    slices: 5,
                                    depthBuffer: _textureDepthBufferBits,
                                    filter: FilterMode.Point,
                                    format: RenderTextureFormat.ARGBFloat,
                                    readWrite: RenderTextureReadWrite.Default,
                                    antiAliasing: 1,
                                    enableRandomWrite: m_CameraTextureDescriptor.enableRandomWrite);

            
            // Configure color and depth targets
            ConfigureTarget(attachmentsToConfigure.ToArray());
            // if (m_OutlineSettings.edgeSettings.blurDebugView)
            // {
            //     ConfigureTarget(_blurHandle.id);
            // }

            // switch (createColorTexture)
            // {
            //     case true when createDepthTexture:
            //         ConfigureClear(ClearFlag.All, Color.black);
            //         break;
            //     case true when !createDepthTexture:
            //         ConfigureClear(ClearFlag.Color, Color.black);
            //         break;
            //     case false when createDepthTexture:
            //         ConfigureClear(ClearFlag.Depth, Color.black);
            //         break;
            //     case false when !createDepthTexture:
            //         ConfigureClear(ClearFlag.None, Color.clear);
            //         break;
            // }
            if (createColorTexture || createDepthTexture)
            {
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            m_CameraTextureDescriptor.enableRandomWrite = true;
            var width = m_CameraTextureDescriptor.width;
            var height = m_CameraTextureDescriptor.height;

            SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);

            if (createDepthTexture)
            {
                cmd.SetGlobalTexture("_OutlineDepth", depthHandle.Identifier(), RenderTextureSubElement.Depth);
            }
            
            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            // computeShader.DisableKeyword("COPY_MIP_0");
            cmd.SetComputeVectorParam(computeShader, "_Size", new Vector4(width, height, 0, 0));

            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            // KernelIndex: 0 = KColorGaussian
            cmd.SetComputeTextureParam(computeShader, 0, "_Source", colorHandle.Identifier(), 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(computeShader, 0, "_Destination", _blurHandle.Identifier(), 0);
            cmd.DispatchCompute(computeShader, 0, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            // KernelIndex: 1 = KColorDownsample
            cmd.SetComputeTextureParam(computeShader, 1, "_Source", colorHandle.Identifier(), 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(computeShader, 1, "_Destination", _blurHandle.Identifier(), 0);
            cmd.DispatchCompute(computeShader, 1, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            cmd.SetGlobalTexture("_BlurResults", _blurHandle.Identifier(), RenderTextureSubElement.Color);

            // if (m_OutlineSettings.edgeSettings.blurDebugView)
            // {
            //     cmd.Blit(_blurHandle.Identifier(), RenderTargetHandle.CameraTarget.id);
            // }
            cmd.ReleaseTemporaryRT(_blurHandle.id);
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