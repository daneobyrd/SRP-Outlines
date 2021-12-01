// ╔══╦══╗  ┌──┬──┐
// ╠══╣  ║  ├──┤  │
// ║  ╠══╣  │  ├──┤
// ╚══╩══╝  └──┴──┘

/* Step-by-step for void RenderColorGaussianPyramid()
Mip Level 0 ─→ 1 (Enable COPY_MIP_0)
 *  0. bool firstDownsample = true;                                                             ┌──────────────────────────────┐
 *  Downsample                                                                                  │                         ↙    │    
 *  1. Set _Size to full-screen                                                                 │                      ↙       │
 *  2. Copy _Source (sourceRT) to _Mip0 (blurRT) ───→ Copy TEXTURE2D_X to RW_TEXTURE2D_X        │                   ↙          │
 *         Note: if(COPY_MIP_0) then _Mip0 is used as downsample input                          ╔══════════════╗               │
 *  3. Set downsample output (_Destination) as tempDownsampleRT mipLevel: 0                     ║              ║               │
 *  4. Dispatch compute using _Size/2 (dstMipWidth, dstMipHeight)                               ║              ║               │
 *     - This writes _Mip0 (a copy of sourceRT) to an area half its size.                       ║              ║               │ 
 *                                                                                              ╚══════════════╝ ──────────────┘
 *  Blur
 *  5. Set _Size to the same size as the downsample texture (the area we just wrote to).
 *  6. Set blur input (_Source) as tempDownsampleRT mipLevel: 0
 *  7. Set blur output (_Destination) as blurRT mipLevel: 1
 *  8. Dispatch compute to the same area used for downsampling
 *  
 *  9. Increase mip level by 1.
 *  10. Divide width and height by 2.
 *  11. Set firstIteration = false;
 *  
Mip Level 1 ─→ 2 (Disable COPY_MIP_0)
 *  1. Set downsample input (_Source) as blurRT (previous blur output) mipLevel: 1
 *  2. Set downsample output (_Destination) as tempDownsampleRT mipLevel: 1
 *  3. Continue for each mip level
         
Upsampling
┌──────════════════════════════┐════════ ╔══════════════════════╗
│                         ↙    │ ║                  ↗   ║
│                      ↙       │ ║              ↗       ║
│                   ↙          │ ┌──────────┐           ║
┌──────────────┐               │ │          │           ║
│              │               │ │          │           ║
│              │               │ └──────────┘ ══════════╝
*/

using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
            camTexDesc.depthStencilFormat = GraphicsFormat.R32_SFloat;
            camTexDesc.depthBufferBits = 0;
            camTexDesc.useMipMap = true; // Do not use autoGenerateMips: does not reliably generate mips.
            camTexDesc.enableRandomWrite = true;
            camTexDesc.dimension = TextureDimension.Tex2DArray;

            // Source TEXTURE ARRAY
            cmd.GetTemporaryRT(_sourceIntId, camTexDesc, FilterMode.Point);
            // Downsample TEXTURE ARRAY
            cmd.GetTemporaryRT(_tempDownsampleIntId, camTexDesc, FilterMode.Point);
            // Blur low mip TEXTURE ARRAY
            cmd.GetTemporaryRT(_blurIntId, camTexDesc, FilterMode.Point);
            // Blur high mip/upsample TEXTURE ARRAY
            cmd.GetTemporaryRT(_finalIntId, camTexDesc, FilterMode.Point);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            var width = opaqueDesc.width;
            var height = opaqueDesc.height;
            // opaqueDesc.mipCount = 8;
            // opaqueDesc.useMipMap = true;
            // opaqueDesc.enableRandomWrite = true;
            // opaqueDesc.colorFormat = RenderTextureFormat.ARGBFloat;
            
            RenderColorGaussianPyramid(cmd, new Vector2Int(width, height), sourceColorTargetID, tempDownsampleTargetID, blurTargetID, finalTargetID);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
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

            bool firstDownsample = true;
            var downsampleKernel = _computeShader.FindKernel("KColorDownsample");

            // mip 0 → 1 → 2 → 3 → 4
            
            while (srcMipWidth >= size.x / _mipLevels || srcMipHeight >= size.y / _mipLevels)
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);   // srcMipWidth/2, floor of 1
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1); // srcMipHeight/2, floor of 1

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                #region Downsample
            // -----------------------------------------------------------------------------------------------------------------------------------
                
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));

                // Note: blurRT is being used as a temp copy of source at mip level: 0. After this blurRT is only set as blur output/downsample input.
                if (firstDownsample) // Copy _Source (sourceRT) to _Mip0 (blurRT) ──→ Copy TEXTURE2D_X to RW_TEXTURE2D_X
                {
                    cmd.EnableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", sourceRT, 0); // TEXTURE2D_X _Source
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Mip0", blurRT, 0); // RW_TEXTURE2D_X _Mip0
                }
                else
                {
                    cmd.DisableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", blurRT, srcMipLevel); // RW_TEXTURE2D_X _Source
                }

                cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", tempDownsampleRT, srcMipLevel); // Change back to mipLevel 0?

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
                // Debug.Log( new Vector2(dstMipWidth, dstMipHeight));

                // Set _Source to the downsampled texture.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", tempDownsampleRT, srcMipLevel);

                // mipLevel: srcMipLevel + 1 because we are writing to the next mip level.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", blurRT, srcMipLevel + 1);

                cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);
                
                #endregion
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                srcMipLevel++;
                // Bitwise operations
                srcMipWidth = srcMipWidth >> 1;    // same as srcMipWidth /= 2. 
                srcMipHeight = srcMipHeight >> 1;  // same as srcMipHeight /= 2.

                // if (firstDownsample)
                // {
                    firstDownsample = false;
                // }
            }
            srcMipLevel = _mipLevels - 1; // Backup to ensure srcMipLevel is set to highest mip. ex: _miplevels(8) -1 sets highest srcMipLevel to 7
            

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            #region Upsample
        // ---------------------------------------------------------------------------------------------------------------------------------------
            
            bool firstUpsample = true;
            var upsampleKernel = _computeShader.FindKernel("KColorUpsample");

            while (srcMipWidth !> size.x / 2 || srcMipHeight !> size.y / 2) // Testing !> size / 2 again
            {
                int dstMipWidth = Mathf.Min(size.x, srcMipWidth << 1);   // srcMipWidth*2, ceiling of screen width
                int dstMipHeight = Mathf.Min(size.y, srcMipHeight << 1); // srcMipWidth*2, ceiling of screen height

                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));
                
                if (firstUpsample)
                {
                    cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_HighMip", blurRT, srcMipLevel);
                    cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_LowMip", finalRT, srcMipLevel - 1);
                }
                else
                {
                    cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_HighMip", finalRT, srcMipLevel);
                    cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_LowMip", blurRT, Mathf.Max(0, srcMipLevel - 1));
                }
                cmd.DispatchCompute(_computeShader, upsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                srcMipLevel--;
                // Bitwise operations
                srcMipWidth <<= 1;   // same as srcMipWidth *= 2.
                srcMipHeight <<= 1; // same as srcMipHeight *= 2.

                if (firstUpsample)
                {
                    firstUpsample = false;
                }
            }
            /*
            srcMipLevel = 0; // Unnecessary backup to ensure srcMipLevel is set to lowest mip.
            */
            
            #endregion
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            
            // Set _BlurUpsampleTexture to the upsampled blur texture mipLevel: 0
            cmd.SetGlobalTexture(_finalIntId, finalRT);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_sourceIntId);
            cmd.ReleaseTemporaryRT(_tempDownsampleIntId);
            cmd.ReleaseTemporaryRT(_blurIntId);  // low mip blur texture
            cmd.ReleaseTemporaryRT(_finalIntId); // upsampled blur texture
        }
    }
}