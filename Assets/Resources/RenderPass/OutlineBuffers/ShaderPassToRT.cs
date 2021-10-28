// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

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
        public List<string> shaderNames;
        public string textureName;
        public RenderTargetHandle TargetHandle;
        public bool createTexture;
        public RenderTextureFormat renderTextureFormat;
        public PassSubTarget(List<string> shaderNames, string textureName, TargetType type, bool createTexture, RenderTextureFormat rtFormat)
        {
            this.shaderNames = shaderNames;
            this.textureName = textureName;
            this.createTexture = createTexture;
            TargetHandle = new RenderTargetHandle(new RenderTargetIdentifier(textureName));
            TargetHandle.Init(new RenderTargetIdentifier(textureName));
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
        // private LineworkSettings linework => _settings.lineworkSettings;
        // private EdgeDetectionSettings edge => _settings.edgeSettings;

        private string _profilerTag;

        private List<ShaderTagId> _shaderTagIdList = new();
        private int _textureDepthBufferBits;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget colorTargetConfig => _settings.lineworkSettings.colorSubTarget;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private RenderTargetHandle colorHandle => colorTargetConfig.TargetHandle;
        private bool createColorTexture => colorTargetConfig.createTexture;
        private RenderTextureFormat colorFormat => colorTargetConfig.renderTextureFormat;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget depthTargetConfig => _settings.lineworkSettings.depthSubTarget;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private RenderTargetHandle depthHandle => depthTargetConfig.TargetHandle;
        private bool createDepthTexture => depthTargetConfig.createTexture;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private ComputeShader _computeShader;

        private RenderTextureDescriptor _cameraTextureDescriptor;
        private RenderTargetHandle _blurHandle;

        private DebugTargetView debugTargetView => _settings.debugTargetView;
        #endregion

        // Must call Init before enqueuing pass
        public void Init(bool hasDepth)
        {
            var totalShaderNames = new List<string>();

            totalShaderNames.AddRange(colorTargetConfig.shaderNames);
            if (hasDepth)
            {
                totalShaderNames.AddRange(depthTargetConfig.shaderNames);
            }
            
            if (_shaderTagIdList.Count >= totalShaderNames.Count) return;
            foreach (var colorTag in colorTargetConfig.shaderNames)
            {
                _shaderTagIdList.Add(new ShaderTagId(colorTag));
            }
            if (!hasDepth) return;
            foreach (var depthTag in depthTargetConfig.shaderNames)
            {
                _shaderTagIdList.Add(new ShaderTagId(depthTag));
            }
        }

        public ShaderPassToRT(OutlineSettings settings, string profilerTag, ComputeShader computeShader, RenderPassEvent renderPassEvent, int depthBufferBits)
        {
            _settings = settings;
            _profilerTag = profilerTag;
            _computeShader = computeShader;
            this.renderPassEvent = renderPassEvent;
            var renderQueueRange = (filter.renderQueueType == RenderQueueType.Opaque) ? RenderQueueRange.opaque : RenderQueueRange.transparent;
            _filteringSettings = new FilteringSettings(renderQueueRange, filter.layerMask);
            _textureDepthBufferBits = depthBufferBits;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // colorHandle.Init(new RenderTargetIdentifier(colorTargetConfig.textureName));
            // depthHandle.Init(new RenderTargetIdentifier(depthTargetConfig.textureName));
            // Debug.Log(nameof(colorTargetConfig.TargetHandle));
            _cameraTextureDescriptor = cameraTextureDescriptor;
            _cameraTextureDescriptor.enableRandomWrite = true;

            List<RenderTargetIdentifier> attachmentsToConfigure = new();
            
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
                                        enableRandomWrite: _cameraTextureDescriptor.enableRandomWrite);
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

            // if (_debugTargetView is DebugTargetView.None or DebugTargetView.BlurResults)
            // {
                cmd.GetTemporaryRTArray(nameID: _blurHandle.id,
                                        width: cameraTextureDescriptor.width,
                                        height: cameraTextureDescriptor.height,
                                        slices: 5,
                                        depthBuffer: _textureDepthBufferBits,
                                        filter: FilterMode.Point,
                                        format: RenderTextureFormat.ARGBFloat,
                                        readWrite: RenderTextureReadWrite.Default,
                                        antiAliasing: 1,
                                        enableRandomWrite: _cameraTextureDescriptor.enableRandomWrite);
            // }


            // Configure color and depth targets
                ConfigureTarget(attachmentsToConfigure.ToArray());
                
            if (createColorTexture || createDepthTexture)
            {
                ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            _cameraTextureDescriptor.enableRandomWrite = true;
            var width = _cameraTextureDescriptor.width;
            var height = _cameraTextureDescriptor.height;

            SortingCriteria sortingCriteria = renderQueueType == RenderQueueType.Opaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

            cmd.SetGlobalTexture(colorTargetConfig.textureName, colorHandle.Identifier());

            if (createDepthTexture)
            {
                cmd.SetGlobalTexture(depthTargetConfig.textureName, depthHandle.Identifier());
            }

            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            // computeShader.DisableKeyword("COPY_MIP_0");
            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(width, height, 0, 0));

            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            var gaussKernel = _computeShader.FindKernel("KColorGaussian");
            cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", colorHandle.Identifier(), 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", _blurHandle.Identifier(), 0);
            cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            var downsampleKernel = _computeShader.FindKernel("KColorDownsample");
            cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", colorHandle.Identifier(), 0, RenderTextureSubElement.Color);
            cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", _blurHandle.Identifier(), 0);
            cmd.DispatchCompute(_computeShader, downsampleKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);

            // -----------------------------------------------------------------------------------------------------------------------------------------------------
            cmd.SetGlobalTexture("_BlurResults", _blurHandle.Identifier(), RenderTextureSubElement.Color);
            
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (createColorTexture) cmd.ReleaseTemporaryRT(colorHandle.id);
            if (createDepthTexture) cmd.ReleaseTemporaryRT(depthHandle.id);
            cmd.ReleaseTemporaryRT(_blurHandle.id);
        }
    }
}