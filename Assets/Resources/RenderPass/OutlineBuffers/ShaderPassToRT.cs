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

        public PassSubTarget(List<string> lightmodeTags, string texName, TargetType type, bool createTexture, RenderTextureFormat rtFormat)
        {
            this.lightmodeTags = lightmodeTags;
            this.textureName = texName;
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

        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private PassSubTarget colorSubTarget => linework.colorSubTarget;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private int colorIdInt => colorSubTarget.renderTargetInt;
        private RenderTargetIdentifier colorTargetId => colorSubTarget.TargetIdentifier;
        private bool createColorTexture => colorSubTarget.createTexture;
        private RenderTextureFormat colorFormat => colorSubTarget.renderTextureFormat;

        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private PassSubTarget depthSubTarget => linework.depthSubTarget;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------
        private int depthIdInt => depthSubTarget.renderTargetInt;
        private RenderTargetIdentifier depthTargetId => depthSubTarget.TargetIdentifier;
        private bool createDepthTexture => depthSubTarget.createTexture;

        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        private ComputeShader _computeShader;
        private RenderTextureDescriptor _cameraTextureDescriptor;

        private int downsampleOutputInt;
        private RenderTargetIdentifier downsampleOutputId => new(downsampleOutputInt);
        
        private int blurOutputIntID = Shader.PropertyToID("_BlurResults");
        private RenderTargetIdentifier blurOutputId => new(blurOutputIntID);

        // private DebugTargetView debugTargetView => _settings.debugTargetView;
        private BlurType blurType
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

        public ShaderPassToRT(OutlineSettings settings, string profilerTag, ComputeShader computeShader, RenderPassEvent renderPassEvent,
            int depthBufferBits)
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

            switch (blurType)
            {
                case BlurType.ColorPyramid:
                    cmd.GetTemporaryRTArray(nameID: colorIdInt,
                                            width: cameraTextureDescriptor.width,
                                            height: cameraTextureDescriptor.height,
                                            slices: 1,
                                            depthBuffer: _textureDepthBufferBits,
                                            filter: FilterMode.Point,
                                            format: colorFormat,
                                            readWrite: RenderTextureReadWrite.Default,
                                            antiAliasing: 1,
                                            enableRandomWrite: true);
                    
                    cmd.GetTemporaryRTArray(nameID: blurOutputIntID,
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
                    cmd.GetTemporaryRT(nameID: colorIdInt, 
                                       width: cameraTextureDescriptor.width,
                                       height: cameraTextureDescriptor.height, 
                                       depthBuffer: _textureDepthBufferBits,
                                       filter: FilterMode.Point,
                                       format: colorFormat,
                                       readWrite: RenderTextureReadWrite.Default,
                                       antiAliasing: 1,
                                       enableRandomWrite: true);
                    
                    cmd.GetTemporaryRT(nameID: blurOutputIntID,
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
                    blurType = BlurType.GaussianPyramid;
                    break;
            }

            if (createColorTexture) attachmentsToConfigure.Add(colorIdInt);

            // Create temporary render texture to store outline opaque objects' depth in new global texture "_OutlineDepth".
            cmd.GetTemporaryRT(nameID: depthIdInt, 
                               width: cameraTextureDescriptor.width,
                               height: cameraTextureDescriptor.height,
                               depthBuffer: _textureDepthBufferBits,
                               filter: FilterMode.Point,
                               format: RenderTextureFormat.Depth);
            if (createDepthTexture) attachmentsToConfigure.Add(depthIdInt);
            
            
            // Configure color and depth targets
            ConfigureTarget(attachmentsToConfigure.ToArray());
            if (createColorTexture || createDepthTexture)
                // cmd.SetGlobalTexture(colorIntId, colorTargetId, RenderTextureSubElement.Color); 
                ConfigureClear(ClearFlag.All, Color.black);
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

            #region Compute Blur

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            _computeShader.DisableKeyword("COPY_MIP_0");
            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(width, height, 0, 0 ));

            switch (blurType)
            {
                case BlurType.ColorPyramid:
                    var downsampleKernel = _computeShader.FindKernel("KColorDownsample");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", colorIdInt, 0, RenderTextureSubElement.Color);
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", downsampleOutputInt, 0);
                    cmd.DispatchCompute(_computeShader, downsampleKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                    // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                    var colorKernel = _computeShader.FindKernel("KColorGaussian");
                    var blurInput = downsampleOutputInt;
                    cmd.SetComputeTextureParam(_computeShader, colorKernel, "_Source", blurInput, 0, RenderTextureSubElement.Color);
                    cmd.SetComputeTextureParam(_computeShader, colorKernel, "_Destination", blurOutputIntID, 0);
                    cmd.DispatchCompute(_computeShader, colorKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                    break;
                case BlurType.GaussianPyramid:
                    var gaussKernel = _computeShader.FindKernel("GAUSSIAN_PYRAMID");
                    cmd.SetComputeFloatParam(_computeShader, "_gaussian_sigma", edge.gaussianSigma);
                    cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", colorIdInt, 0, RenderTextureSubElement.Color);
                    cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", blurOutputIntID, 0);
                    cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                    break;
                default:
                    blurType = BlurType.GaussianPyramid;
                    break;
            }

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

            #endregion

            cmd.SetGlobalTexture(blurOutputIntID, blurOutputId, RenderTextureSubElement.Color);
            
            // void RenderColorGaussianPyramid(CommandBuffer cmd, Vector4 size, RenderTargetIdentifier sourceRT, RenderTargetIdentifier destinationRT)
            // {
            //     int prevMip = 0;
            //     int currentMip = 1;
            //     while (width >= 16 || height >= 16)
            //     {
            //         cmd.SetComputeVectorParam(computeShader, "_Size", new Vector4(width, height, 0, 0));
            //         var downsampleKernel = computeShader.FindKernel("KColorDownsample");
            //         var gaussKernel = computeShader.FindKernel("KColorGaussian");
            //         // if (first iteration)
            //         {
            //             computeShader.EnableKeyword("COPY_MIP_0");
            //             cmd.SetComputeTextureParam(computeShader, downsampleKernel, "_Source", sourceRT, 0, RenderTextureSubElement.Color);
            //             cmd.SetComputeTextureParam(computeShader, downsampleKernel, "_Mip0", destinationRT, 0);
            //         }
            //         // else
            //         {
            //             computeShader.DisableKeyword("COPY_MIP_0");
            //             cmd.SetComputeTextureParam(computeShader, downsampleKernel, "_Source", destinationRT, prevMip, RenderTextureSubElement.Color);
            //         }
            //         cmd.SetComputeTextureParam(computeShader, downsampleKernel, "_Destination", blurInputID, 0);
            //         //Set vector size //Acoarding to the draw procedural example
            //         cmd.DispatchCompute(computeShader, downsampleKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
            //
            //         //Set size params
            //         computeShader.DisableKeyword("COPY_MIP_0");
            //         cmd.SetComputeTextureParam(computeShader, downsampleKernel, "_Source", blurInputID, 0, RenderTextureSubElement.Color);
            //         cmd.SetComputeTextureParam(computeShader, downsampleKernel, "_Destination", targetID, currentMip);
            //         //Set vector size //Acoarding to the draw procedural example
            //         cmd.DispatchCompute(computeShader, gaussKernel, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
            //     }
            //
            //     cmd.SetGlobalTexture(blurInputInt, blurInputID, RenderTextureSubElement.Color);
            // }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (createColorTexture) cmd.ReleaseTemporaryRT(colorIdInt);
            if (createDepthTexture) cmd.ReleaseTemporaryRT(depthIdInt);
            if (blurType == BlurType.ColorPyramid)
            {
                cmd.ReleaseTemporaryRT(downsampleOutputInt);
            }
            cmd.ReleaseTemporaryRT(blurOutputIntID);
        }
    }
}