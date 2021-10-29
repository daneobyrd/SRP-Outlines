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

    public enum BlurType
    {
        ColorPyramid,
        GaussianPyramid,
        // Kawase,
        // Box
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
        
        public PassSubTarget(List<string> lightmodeTags, string textureName, TargetType type, bool createTexture, RenderTextureFormat rtFormat)
        {
            this.lightmodeTags = lightmodeTags;
            this.textureName = textureName;
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
        private EdgeDetectionSettings edge => _settings.edgeSettings;

        private string _profilerTag;

        private List<ShaderTagId> _shaderTagIdList = new();
        private int _textureDepthBufferBits;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget colorSubTarget => linework.colorSubTarget;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private int colorIntId => colorSubTarget.renderTargetInt;
        private RenderTargetIdentifier colorTargetId => colorSubTarget.TargetIdentifier;
        private bool createColorTexture => colorSubTarget.createTexture;
        private RenderTextureFormat colorFormat => colorSubTarget.renderTextureFormat;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private PassSubTarget depthSubTarget => linework.depthSubTarget;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private int depthIntId => depthSubTarget.renderTargetInt;
        private RenderTargetIdentifier depthTargetId => depthSubTarget.TargetIdentifier;

        private bool createDepthTexture => depthSubTarget.createTexture;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        private ComputeShader _computeShader;

        private RenderTextureDescriptor _cameraTextureDescriptor;
        private int _blurIntId = Shader.PropertyToID("_BlurResults");
        private RenderTargetIdentifier blurTargetId => new(_blurIntId);
        
        // private DebugTargetView debugTargetView => _settings.debugTargetView;
        private BlurType _blurType
        {
            get => _settings.edgeSettings.BlurType;
            set => _settings.edgeSettings.BlurType = value;
        }

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
            _cameraTextureDescriptor = cameraTextureDescriptor;

            List<RenderTargetIdentifier> attachmentsToConfigure = new();

            switch (_blurType)
            {
                case BlurType.ColorPyramid:
                    cmd.GetTemporaryRTArray(nameID: colorIntId,
                                            width: cameraTextureDescriptor.width,
                                            height: cameraTextureDescriptor.height,
                                            slices: 5,
                                            depthBuffer: _textureDepthBufferBits,
                                            filter: FilterMode.Point,
                                            format: colorFormat,
                                            readWrite: RenderTextureReadWrite.Default,
                                            antiAliasing: 1,
                                            enableRandomWrite: true);
                    
                    cmd.GetTemporaryRTArray(nameID: _blurIntId,
                                            width: cameraTextureDescriptor.width,
                                            height: cameraTextureDescriptor.height,
                                            slices: 5,
                                            depthBuffer: _textureDepthBufferBits,
                                            filter: FilterMode.Point,
                                            format: RenderTextureFormat.ARGBFloat,
                                            readWrite: RenderTextureReadWrite.Default,
                                            antiAliasing: 1,
                                            enableRandomWrite: true);
                    break;

                case BlurType.GaussianPyramid:
                    cmd.GetTemporaryRT(nameID: colorIntId, 
                                       width: cameraTextureDescriptor.width,
                                       height: cameraTextureDescriptor.height, 
                                       depthBuffer: _textureDepthBufferBits,
                                       filter: FilterMode.Point,
                                       format: colorFormat,
                                       readWrite: RenderTextureReadWrite.Default,
                                       antiAliasing: 1,
                                       enableRandomWrite: true);
                    
                    cmd.GetTemporaryRT(nameID: _blurIntId,
                                       width: cameraTextureDescriptor.width,
                                       height: cameraTextureDescriptor.height,
                                       depthBuffer: _textureDepthBufferBits,
                                       filter: FilterMode.Point,
                                       format: RenderTextureFormat.ARGBFloat,
                                       readWrite: RenderTextureReadWrite.Default,
                                       antiAliasing: 1,
                                       enableRandomWrite: true);
                    break;
                // case BlurType.Kawase:
                // break;
                // case BlurType.Box:
                // break;
                default:
                    _blurType = BlurType.GaussianPyramid;
                    break;
            }
            if (createColorTexture) attachmentsToConfigure.Add(colorTargetId);

            // Create temporary render texture to store outline opaque objects' depth in new global texture "_OutlineDepth".
            cmd.GetTemporaryRT(nameID: depthIntId, 
                               width: cameraTextureDescriptor.width,
                               height: cameraTextureDescriptor.height,
                               depthBuffer: _textureDepthBufferBits,
                               filter: FilterMode.Point,
                               format: RenderTextureFormat.Depth);
            if (createDepthTexture) attachmentsToConfigure.Add(depthIntId);
            
            
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

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // _computeShader.DisableKeyword("COPY_MIP_0");
            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(width, height, 0, 0));
            
            switch (_blurType)
            {
                case BlurType.ColorPyramid:
                    var colorKernel = _computeShader.FindKernel("KColorGaussian");
                    cmd.SetComputeTextureParam(_computeShader, colorKernel, "_Source", colorTargetId, 0, RenderTextureSubElement.Color);
                    cmd.SetComputeTextureParam(_computeShader, colorKernel, "_Destination", blurTargetId, 0);
                    cmd.DispatchCompute(_computeShader, colorKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                    // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                    var downsampleKernel = _computeShader.FindKernel("KColorDownsample");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", colorTargetId, 0, RenderTextureSubElement.Color);
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", blurTargetId, 0);
                    cmd.DispatchCompute(_computeShader, downsampleKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                    break;
                case BlurType.GaussianPyramid:
                    var gaussKernel = _computeShader.FindKernel("GAUSSIAN_PYRAMID");
                    cmd.SetComputeFloatParam(_computeShader, "_gaussian_sigma", edge.gaussianSigma);
                    cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", colorTargetId, 0, RenderTextureSubElement.Color);
                    cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", blurTargetId, 0);
                    cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                    break;
                default:
                    _blurType = BlurType.GaussianPyramid;
                    break;
            }
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

            cmd.SetGlobalTexture(_blurIntId, blurTargetId, RenderTextureSubElement.Color);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (createColorTexture) cmd.ReleaseTemporaryRT(colorIntId);
            if (createDepthTexture) cmd.ReleaseTemporaryRT(depthIntId);
            cmd.ReleaseTemporaryRT(_blurIntId);
        }
    }
}