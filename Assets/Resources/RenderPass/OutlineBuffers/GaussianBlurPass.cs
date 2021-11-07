﻿// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resources.RenderPass.OutlineBuffers
{
    public class GaussianBlurPass : ScriptableRenderPass
    {
        #region Variables
        
        private string _profilerTag;
        private ComputeShader _computeShader;

        private int _sourceIntId;
        private int tempDownsampleIntId => Shader.PropertyToID("_DownsampleTex");
        private int blurIntId => Shader.PropertyToID("_BlurResults");

        private RenderTargetIdentifier tempColorTargetID => new(_sourceIntId); // _OutlineOpaque
        private RenderTargetIdentifier tempDownsampleTargetID => new(tempDownsampleIntId);
        private RenderTargetIdentifier finalTargetID => new(blurIntId);
        private int _tempFinalInt = Shader.PropertyToID("tempFinalTex");
        private RenderTargetIdentifier tempFinal => new(_tempFinalInt);
        
        private RenderTextureDescriptor _cameraTextureDescriptor;

        #endregion

        public GaussianBlurPass(string profilerTag)
        {
            _profilerTag = profilerTag;
        }
        
        public void Setup(string sourceName, ComputeShader computeShader)
        {
            _sourceIntId = Shader.PropertyToID(sourceName);
            _computeShader = computeShader;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor camTexDesc) //called before execute. if not overriden renders to camera target?
        {
            _cameraTextureDescriptor = camTexDesc;
            _cameraTextureDescriptor.enableRandomWrite = true;
            var width = _cameraTextureDescriptor.width;
            var height = _cameraTextureDescriptor.height;
            
            // Source texture
            cmd.GetTemporaryRTArray(nameID: _sourceIntId,
                                    width: width,
                                    height: height,
                                    slices: 1,
                                    depthBuffer: 0,
                                    filter: FilterMode.Point,
                                    format: RenderTextureFormat.ARGBFloat,
                                    readWrite: RenderTextureReadWrite.Default,
                                    antiAliasing: 1,
                                    enableRandomWrite: true);
            // Downsample texture
            cmd.GetTemporaryRTArray(nameID: tempDownsampleIntId,
                                    width: width,
                                    height: height,
                                    slices: 1,
                                    depthBuffer: 0,
                                    filter: FilterMode.Point,
                                    format: RenderTextureFormat.ARGBFloat,
                                    readWrite: RenderTextureReadWrite.Default,
                                    antiAliasing: 1,
                                    enableRandomWrite: true);
            // Blur result
            cmd.GetTemporaryRT(nameID: blurIntId,
                               width: width,
                               height: height,
                               depthBuffer: 0,
                               filter: FilterMode.Point,
                               format: RenderTextureFormat.ARGBFloat,
                               readWrite: RenderTextureReadWrite.Default,
                               antiAliasing: 1,
                               enableRandomWrite: true);

            cmd.GetTemporaryRT(_tempFinalInt,
                                width,
                                height,
                                0,
                                FilterMode.Point);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            _cameraTextureDescriptor.enableRandomWrite = true;
            var width = _cameraTextureDescriptor.width;
            var height = _cameraTextureDescriptor.height;

            RenderColorGaussianPyramid(cmd, new Vector2Int(width, height), tempColorTargetID, tempDownsampleTargetID, finalTargetID);

            cmd.SetGlobalTexture(blurIntId, tempFinal, RenderTextureSubElement.Color);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private void RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size, RenderTargetIdentifier sourceRT, RenderTargetIdentifier tempDownsampleRT, RenderTargetIdentifier finalRT)
        {
            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;
            
            bool firstIteration = true;

            while (srcMipWidth >= 8 || srcMipHeight >= 8)
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);
                
                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                // Downsample
                // -----------------------------------------------------------------------------------------------------------------------------------
                var downsampleKernel = _computeShader.FindKernel("KColorDownsample");
                if (firstIteration)
                {
                    cmd.EnableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", sourceRT, 0, RenderTextureSubElement.Color);
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Mip0", finalRT, 0);
                }
                else
                {
                    cmd.DisableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", finalRT, srcMipLevel, RenderTextureSubElement.Color);
                }
                cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", tempDownsampleRT, 0);
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));
                
                // This is what makes the compute write to a smaller region of the tempDownsampleTargetID.
                cmd.DispatchCompute(_computeShader, downsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);
                
                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                // Blur
                // -----------------------------------------------------------------------------------------------------------------------------------
                var gaussKernel = _computeShader.FindKernel("KColorGaussian");
                cmd.DisableShaderKeyword("COPY_MIP_0");
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", tempDownsampleRT, 0, RenderTextureSubElement.Color);
                // srcMipLevel + 1 because we are writing to the next mip level.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", finalRT, srcMipLevel + 1);
                // The data we want to blur is in the area defined by (dstMipWidth, dstMipHeight)
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));
                
                cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);
                
                srcMipLevel++;
                // Bitwise operation; same as srcMipWidth /= 2.
                srcMipWidth = srcMipWidth >> 1;
                srcMipHeight = srcMipHeight >> 1;

                firstIteration = false;
            }
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Upscale Blurred Mip4
            // -----------------------------------------------------------------------------------------------------------------------------------

            var upscaleKernel = _computeShader.FindKernel("KColorUpscale");
            cmd.SetComputeVectorParam(_computeShader, "_UpscaleSize", new Vector4(size.x, size.y, 0, 0));
            cmd.SetComputeTextureParam(_computeShader, upscaleKernel, "_Final_Mip4", finalRT, 0);
            cmd.SetComputeTextureParam(_computeShader, upscaleKernel, "_Upscale", tempFinal, 0);
            cmd.DispatchCompute(_computeShader, upscaleKernel, Mathf.CeilToInt(srcMipWidth / 8f), Mathf.CeilToInt(srcMipHeight / 8f), 1);
        }


        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_sourceIntId);
            cmd.ReleaseTemporaryRT(tempDownsampleIntId);
            cmd.ReleaseTemporaryRT(blurIntId); // Used by tempDownsampleTarget
            cmd.ReleaseTemporaryRT(_tempFinalInt);
        }
    }
}