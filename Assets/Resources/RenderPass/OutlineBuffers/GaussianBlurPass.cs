﻿// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderPass.OutlineBuffers
{
    public class GaussianBlurPass : ScriptableRenderPass
    {
        #region Variables

        private string _profilerTag;
        private ComputeShader _computeShader;
        private int _mipLevels;

        private int _sourceIntId;
        private int _tempDownsampleIntId = Shader.PropertyToID("_DownsampleTex");
        private int _blurIntId = Shader.PropertyToID("_BlurResults");
        private int _finalIntId = Shader.PropertyToID("_BlurUpsampleTex");

        private RenderTargetIdentifier sourceColorTargetID => new(_sourceIntId); // _OutlineOpaque
        private RenderTargetIdentifier tempDownsampleTargetID => new(_tempDownsampleIntId);
        private RenderTargetIdentifier blurTargetID => new(_blurIntId);
        private RenderTargetIdentifier finalTargetID => new(_finalIntId);

        // private RenderTextureDescriptor _camTexDesc;

        #endregion

        public GaussianBlurPass(string profilerTag)
        {
            _profilerTag = profilerTag;
        }

        public void Init(string sourceName, ComputeShader computeShader, int pyramidLevels)
        {
            _sourceIntId = Shader.PropertyToID(sourceName);
            _computeShader = computeShader;
            _mipLevels = pyramidLevels;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
            var width = camTexDesc.width;
            var height = camTexDesc.height;
            camTexDesc.msaaSamples = 1;
            camTexDesc.mipCount = _mipLevels;
            camTexDesc.colorFormat = RenderTextureFormat.ARGBFloat;
            camTexDesc.depthBufferBits = 0;
            camTexDesc.useMipMap = true; // Do not use autoGenerateMips: does not reliably generate mips.
            camTexDesc.enableRandomWrite = true;
            if (camTexDesc.mipCount == 1)
            {
                camTexDesc.mipCount = 8;
            } // Sanity Check that mip count is being correctly set

            camTexDesc.dimension = TextureDimension.Tex2DArray;

            // Source TEXTURE ARRAY
            cmd.GetTemporaryRT(_sourceIntId, camTexDesc);
            // Downsample TEXTURE ARRAY
            cmd.GetTemporaryRT(_tempDownsampleIntId, camTexDesc);
            // Blur low mip TEXTURE ARRAY
            cmd.GetTemporaryRT(_blurIntId, camTexDesc);
            // Blur high mip/upsample texture
            cmd.GetTemporaryRT(_finalIntId, camTexDesc);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            var width = opaqueDesc.width;
            var height = opaqueDesc.height;
            opaqueDesc.mipCount = 8;
            opaqueDesc.useMipMap = true;
            // opaqueDesc.autoGenerateMips = true;
            opaqueDesc.enableRandomWrite = true;

            RenderColorGaussianPyramid(cmd, new Vector2Int(width, height), sourceColorTargetID, tempDownsampleTargetID, blurTargetID, finalTargetID);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


        /* Process
         FirstIteration:
            Downsample
            1. Set _Size to full-screen
            2. Copy _Source (sourceRT) to _Mip0 (blurRT) ───→ Copy TEXTURE2D_X to RW_TEXTURE2D_X
            3. Set downsample output (_Destination) as tempDownsampleRT mipLevel: 0
            4. Dispatch compute using _Size/2 (dstMipWidth, dstMipHeight)
               - This writes _Mip0 (a copy of sourceRT) to an area half its size.
            
            Blur
            5. Set _Size to the same size as the downsample texture (the area we just wrote to).
            6. Set blur input (_Source) as tempDownsampleRT mipLevel: 0
            7. Set blur output (_Destination) as blurRT mipLevel: 1
            8. Dispatch compute to the same area used for downsampling
            
            9. Increase mip level by 1.
            10. Divide width and height by 2.
            11. Set firstIteration = false;
        */

        private void RenderColorGaussianPyramid(CommandBuffer cmd,
                                                Vector2Int size,
                                                RenderTargetIdentifier sourceRT,
                                                RenderTargetIdentifier tempDownsampleRT,
                                                RenderTargetIdentifier blurRT,
                                                RenderTargetIdentifier finalRT)
        {
            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;

            bool firstIteration = true;

            while (srcMipWidth >= size.x / _mipLevels || srcMipHeight >= size.y / _mipLevels)
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);   // srcMipWidth/2, floor of 1
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1); // srcMipHeight/2, floor of 1

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                #region Downsample
            // -----------------------------------------------------------------------------------------------------------------------------------
                
                var downsampleKernel = _computeShader.FindKernel("KColorDownsample");
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));

                // Note: blurRT is being used as a temp copy of source here, after this it is only used for blurring.
                if (firstIteration) // Copy _Source (sourceRT) to _Mip0 (blurRT) ───→ Copy TEXTURE2D_X tp RW_TEXTURE2D_X
                {
                    cmd.EnableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", sourceRT, 0);
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Mip0", blurRT, 0);
                }
                else
                {
                    cmd.DisableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", blurRT, srcMipLevel);
                }

                cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", tempDownsampleRT, srcMipLevel);

                // This is what makes the compute write to a smaller region of the tempDownsampleTargetID.
                cmd.DispatchCompute(_computeShader, downsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                #endregion
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

            
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                #region Blur
            // -----------------------------------------------------------------------------------------------------------------------------------

                var gaussKernel = _computeShader.FindKernel("KColorGaussian");
                cmd.DisableShaderKeyword("COPY_MIP_0");

                // The data we want to blur is in the area defined by (dstMipWidth, dstMipHeight) ------- the area we wrote the downsample texture to.
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));

                // Set _Source to the downsampled texture.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", tempDownsampleRT, srcMipLevel);

                // mipLevel: srcMipLevel + 1 because we are writing to the next mip level.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", blurRT, srcMipLevel + 1);

                cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);
                
                #endregion
                
                srcMipLevel++;
                // Bitwise operations
                srcMipWidth = srcMipWidth >> 1;    // same as srcMipWidth /= 2. 
                srcMipHeight = srcMipHeight >> 1;  // same as srcMipHeight /= 2.

                firstIteration = false;
            }
            srcMipLevel = _mipLevels; // Backup to ensure srcMipLevel is set to highest mip.
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            #region Upsample
        // ---------------------------------------------------------------------------------------------------------------------------------------
        
            var upsampleKernel = _computeShader.FindKernel("KColorUpsample");
            
            // Paranoid check/stop-gap in-case mip width & height are greater than size.xy/2 but less than size.xy.
            while (srcMipWidth !> size.x / 2 || srcMipHeight !> size.y / 2)
            {
                srcMipLevel = Mathf.Max(1, srcMipLevel); // Set minimum mip level as 1 because of line 199.

                int dstMipWidth = Mathf.Min(size.x, srcMipWidth << 1);   // srcMipWidth*2, ceiling of screen width
                int dstMipHeight = Mathf.Min(size.y, srcMipHeight << 1); // srcMipWidth*2, ceiling of screen height

                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));
                cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_LowMip", blurRT, srcMipLevel);
                cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_HighMip", finalRT, srcMipLevel - 1);
                cmd.DispatchCompute(_computeShader, upsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                // At this point srcMipLevel will never be 0, thus srcMipLevel--; will never return a negative value.
                srcMipLevel--;
                // Bitwise operations
                srcMipWidth = srcMipWidth << 1;   // same as srcMipWidth *= 2.
                srcMipHeight = srcMipHeight << 1; // same as srcMipHeight *= 2.
            }
            srcMipLevel = 0; // Unnecessary backup to ensure srcMipLevel is set to lowest mip.
            
            #endregion
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            
            // Set _BlurUpsampleTexture to the upsampled blur texture mipLevel: 0
            cmd.SetGlobalTexture(_finalIntId, finalRT);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_sourceIntId);
            cmd.ReleaseTemporaryRT(_tempDownsampleIntId);
            cmd.ReleaseTemporaryRT(_blurIntId);  // low mip blur texture
            cmd.ReleaseTemporaryRT(_finalIntId); // upsampled blur texture
        }
    }
}