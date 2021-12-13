// Quick-access Copy/Paste grids
// ╔══╦══╗  ┌──┬──┐
// ╠══╬══╣  │  │  │
// ║  ║  ║  ├──┼──┤
// ╚══╩══╝  └──┴──┘

/* Step-by-step for void RenderColorGaussianPyramid()
Mip Level 0 ─→ 1 (Enable COPY_MIP_0)
 *  0. bool firstDownsample = true;                                                        ┌──────────────────────────────┐
 *  Downsample                                                                             │                         ↙    │    
 *  1. Set _Size to full-screen                                                            │                      ↙       │
 *  2. Copy _Source (sourceRT) to _Mip0 (blurRT) ───→ Copy TEXTURE2D_X to RW_TEXTURE2D_X   │                   ↙          │
 *         Note: if(COPY_MIP_0) then _Mip0 is used as downsample input                     ╔══════════════╗               │
 *  3. Set downsample output (_Destination) as tempDownsampleRT mipLevel: 0                ║              ║               │
 *  4. Dispatch compute using _Size/2 (dstMipWidth, dstMipHeight)                          ║              ║               │
 *     - This writes _Mip0 (a copy of sourceRT) to an area half its size.                  ║              ║               │ 
 *                                                                                         ╚══════════════╝ ──────────────┘
 *  Blur
 *  5. Set _Size to the same size as the downsample texture (the area we just wrote to).
 *  6. Set blur input (_Source) as tempDownsampleRT mipLevel: 0
 *  7. Set blur output (_Destination) as blurRT mipLevel: 1
 *  8. Dispatch compute to the same area used for downsampling
 *  
 *  9. Increase mip level by 1.
 *  10. Divide width and height by 2.
 *  11. Set firstDownsample = false;
 *  
Mip Level 1 ─→ 2 (Disable COPY_MIP_0)
 *  1. Set downsample input (_Source) as blurRT (previous blur output) mipLevel: 1
 *  2. Set downsample output (_Destination) as tempDownsampleRT mipLevel: 1
 *  3. Continue for each mip level
         
Upsample
╔═══════════════════════════════╗
║                           ↗   ║
║                       ↗       ║
║                   ↗           ║
┌──────────────┐                ║
│              │                ║
│              │                ║ 
│              │                ║
└──────────────┘ ═══════════════╝
*/

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
        private int _totalMips;
        
        private int _sourceId;
        private readonly int _downsampleId = Shader.PropertyToID("_DownsampleTex");
        private readonly int _blurId = Shader.PropertyToID("_BlurResults");
        private readonly int _upsampleId = Shader.PropertyToID("_BlurredUpsampleTex");
        
        private RenderTargetIdentifier sourceColorTarget => new(_sourceId); // _OutlineOpaque
        private RenderTargetIdentifier downsampleTarget => new(_downsampleId);
        private RenderTargetIdentifier blurTarget => new(_blurId);
        private RenderTargetIdentifier finalTarget => new(_upsampleId);

        #endregion

        public GaussianBlurPass(string profilerTag)
        {
            _profilerTag = profilerTag;
        }

        public void Init(string sourceName, ComputeShader computeShader, int pyramidLevels)
        {
            _sourceId = Shader.PropertyToID(sourceName);
            _computeShader = computeShader;
            _totalMips = pyramidLevels;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
            camTexDesc.msaaSamples = 1;
            camTexDesc.mipCount = _totalMips;
            camTexDesc.colorFormat = RenderTextureFormat.ARGBFloat;
            camTexDesc.depthBufferBits = (int) DepthBits.None;
            camTexDesc.useMipMap = true; // Do not use autoGenerateMips: does not reliably generate mips.
            camTexDesc.enableRandomWrite = true;
            // camTexDesc.useDynamicScale = true;

            // _Source does not need mip maps
            RenderTextureDescriptor sourceDesc = camTexDesc;
            sourceDesc.useMipMap = false;
            
            // _BlurRT needs one more mip than up/downsample
            RenderTextureDescriptor blurDesc = camTexDesc;
            sourceDesc.mipCount = _totalMips + 1;


            // Source TEXTURE ARRAY
            cmd.GetTemporaryRT(_sourceId, sourceDesc, FilterMode.Bilinear);
            // Downsample TEXTURE ARRAY
            cmd.GetTemporaryRT(_downsampleId, camTexDesc, FilterMode.Bilinear);
            // Blur low mip TEXTURE ARRAY
            cmd.GetTemporaryRT(_blurId, blurDesc, FilterMode.Bilinear);
            // Blur high mip/upsample TEXTURE ARRAY
            cmd.GetTemporaryRT(_upsampleId, camTexDesc, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            var screenSize = new Vector2Int(opaqueDesc.width, opaqueDesc.height);

            RenderColorGaussianPyramid(cmd, screenSize,
                                       sourceColorTarget,
                                       downsampleTarget,
                                       blurTarget,
                                       finalTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderColorGaussianPyramid(CommandBuffer cmd, Vector2Int size,
                                                RenderTargetIdentifier sourceRT,
                                                RenderTargetIdentifier downsampleRT,
                                                RenderTargetIdentifier blurRT,
                                                RenderTargetIdentifier upsampleRT)
        {
            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;

            int dstMipLevel = 0;
            
            var downsampleKernel = _computeShader.FindKernel("KColorDownsample");
            var gaussKernel = _computeShader.FindKernel("KColorGaussian");
            var upsampleKernel = _computeShader.FindKernel("KColorUpsample");

            int maxMipLevel = _totalMips - 1;
            bool MaxMipReached() { return srcMipLevel == maxMipLevel; }
            
            bool firstDownsample = true;

            for (var i = 0; i < maxMipLevel; i++)
            // while (srcMipWidth > (size.x >> _totalMips) || srcMipHeight > (size.y >> _totalMips))
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                // srcMipLevel = Mathf.Min(srcMipLevel, maxMipLevel);       // Likely unnecessary, maybe Debug.Assert this.
                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region Downsample

                // -----------------------------------------------------------------------------------------------------------------------------------

                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));

                if (firstDownsample)
                {
                    cmd.EnableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", sourceRT); // TEXTURE2D_X _Source
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Mip0", blurRT, 0); // RW_TEXTURE2D_X _Source
                }
                else
                {
                    cmd.DisableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", blurRT, srcMipLevel); // RW_TEXTURE2D_X _Source
                }

                cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", downsampleRT, dstMipLevel);

                // This is what makes the compute write to a smaller region of the downsampleTargetID.
                cmd.DispatchCompute(_computeShader, downsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                #endregion

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                dstMipLevel++;
                
                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region BlurDown

                // -----------------------------------------------------------------------------------------------------------------------------------
                
                // The data we want to blur is in the area defined by (dstMipWidth, dstMipHeight) - the area we wrote the downsample texture to.
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));

                // Set _Source to the downsampled texture.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", downsampleRT, srcMipLevel);

                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", blurRT, dstMipLevel);

                cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                #endregion
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                
                // Do not increase srcMipLevel if maxMipReached.
                if (MaxMipReached()) break; 
                
                srcMipLevel++;
                // Bitshift operations
                srcMipWidth >>= 1; 
                srcMipHeight >>= 1;

                if (firstDownsample) { firstDownsample = false; }
            }
            
            dstMipLevel = srcMipLevel - 1;
            // while (srcMipLevel >= 0)
            for (var i = srcMipLevel; i > 0; i--)
            // while (srcMipWidth < size.x || srcMipHeight < size.y)
            {
                int dstMipWidth = Mathf.Min(size.x, srcMipWidth << 1); // srcMipWidth*2, ceiling of screen width
                int dstMipHeight = Mathf.Min(size.y, srcMipHeight << 1); // srcMipWidth*2, ceiling of screen height

                // srcMipLevel = Mathf.Max(srcMipLevel, 0);       // Likely unnecessary, maybe Debug.Assert this.
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                #region Upsample
            // ---------------------------------------------------------------------------------------------------------------------------------------
                // var firstUpsample = maxMipReached();
                
                // Viewport Size
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                
                // Smaller Texture
                    var lowSourceSize = new Vector2(srcMipWidth, srcMipHeight);
                    var lowSourceTexelSize = lowSourceSize / size;
                cmd.SetComputeVectorParam(_computeShader, "_BlurBicubicParams", new Vector4(srcMipWidth, srcMipHeight, lowSourceTexelSize.x, lowSourceTexelSize.y));
                
                // Larger Texture
                    var highSourceSize = new Vector2(dstMipWidth, dstMipHeight);
                    var highSourceTexelSize = highSourceSize / size;
                cmd.SetComputeVectorParam(_computeShader, "_TexelSize", new Vector4(dstMipWidth, dstMipHeight, highSourceTexelSize.x, highSourceTexelSize.y));
                
                cmd.SetComputeFloatParam(_computeShader, "_Scatter", 0.5f);

                cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_LowResMip", blurRT, srcMipLevel);
                cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_HighResMip", upsampleRT, dstMipLevel);
                
                // threadGroups divisor must match ColorPyramid KERNEL_SIZE.
                cmd.DispatchCompute(_computeShader, upsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                #endregion
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            
            if (dstMipLevel == 0) break; // If mipLevel = 0 then no further blurring/upsampling is needed.
            
            srcMipLevel--;
            // Bitshift operations
            srcMipWidth <<= 1;
            srcMipHeight <<= 1;

                
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                #region BlurUp
            // -----------------------------------------------------------------------------------------------------------------------------------
            
                // The data we want to blur is in the area defined by (dstMipWidth, dstMipHeight) ------- the area we wrote the upsample texture to.
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));
                // Set _Source to the upsampled texture.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", upsampleRT, srcMipLevel);

                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", blurRT, dstMipLevel);

                cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                #endregion
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

            dstMipLevel--;

            }

            // SetGlobalTexture _BlurredUpsampleTexture
            cmd.SetGlobalTexture(_upsampleId, upsampleRT);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_sourceId);
            cmd.ReleaseTemporaryRT(_downsampleId);
            cmd.ReleaseTemporaryRT(_blurId); // low mip blur texture
            cmd.ReleaseTemporaryRT(_upsampleId); // upsampled blur texture
        }
    }
}