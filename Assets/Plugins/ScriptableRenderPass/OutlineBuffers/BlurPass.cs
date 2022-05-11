// Quick-access Copy/Paste grids
// ╔══╦══╗  ┌──┬──┐
// ╠══╬══╣  │  │  │
// ║  ║  ║  ├──┼──┤
// ╚══╩══╝  └──┴──┘

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Plugins.ScriptableRenderPass
{
    public enum BlurType
    {
        Gaussian = 0,
        Kawase = 1
    }

    /// <inheritdoc />
    public class BlurPass : UnityEngine.Rendering.Universal.ScriptableRenderPass
    {
        private bool _enabled;
        private string _profilerTag;
        private ComputeShader _computeShader;
        private BlurType _blurType;

        #region Gaussian Properties

        private int _totalMips;
        private int _sourceId;
        private readonly int _tempBlurId1 = Shader.PropertyToID("_tempBlur1");
        private readonly int _tempBlurId2 = Shader.PropertyToID("_tempBlur2");
        private readonly int _finalId = Shader.PropertyToID("_FinalBlur");

        private RenderTargetIdentifier SourceColorTarget => new(_sourceId); // _OutlineOpaque
        private RenderTargetIdentifier DownsampleTarget  => new(_tempBlurId1);
        private RenderTargetIdentifier BlurTarget        => new(_tempBlurId2);
        private RenderTargetIdentifier FinalTarget       => new(_finalId);

        #endregion

        #region Kawase Properties

        private float _threshold;
        private float _intensity;
        private const bool CopyToFrameBuffer = false;
        private int _passes;

        #endregion

        public BlurPass(string profilerTag)
        {
            _profilerTag = profilerTag;
        }

        public void Init(string sourceName, BlurType blurType, ComputeShader computeShader, int blurPasses, float threshold, float intensity)
        {
            _sourceId      = Shader.PropertyToID(sourceName);
            _blurType      = blurType;
            _computeShader = computeShader;
            _totalMips     = blurPasses;
            _passes        = blurPasses;
            _threshold     = threshold;
            _intensity     = intensity;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var camTexDesc = cameraTextureDescriptor;
            camTexDesc.msaaSamples       = 1;
            camTexDesc.colorFormat       = RenderTextureFormat.ARGBFloat;
            camTexDesc.depthBufferBits   = (int) DepthBits.None;
            camTexDesc.enableRandomWrite = true;

            // _Source does not need mip maps
            RenderTextureDescriptor sourceDesc = camTexDesc;
            sourceDesc.mipCount  = 0;
            sourceDesc.useMipMap = false;

            // Source texture
            cmd.GetTemporaryRT(_sourceId, sourceDesc, FilterMode.Bilinear);

            switch (_blurType)
            {
                case BlurType.Gaussian:
                {
                    camTexDesc.mipCount  = _totalMips;
                    camTexDesc.useMipMap = true; // Do not use autoGenerateMips: does not reliably generate mips.

                    // Blur high mip/upsample texture
                    var upsampleRTDesc = camTexDesc;
                    upsampleRTDesc.mipCount = _totalMips - 1;

                    cmd.GetTemporaryRT(_finalId, upsampleRTDesc, FilterMode.Bilinear);
                    break;
                }
                case BlurType.Kawase:
                    // camTexDesc.mipCount  = _passes;
                    // camTexDesc.useMipMap = true; // Testing using different mip level for subsequent blurs

                    var finalBlurRTDesc = camTexDesc;
                    finalBlurRTDesc.mipCount  = 0;
                    finalBlurRTDesc.useMipMap = false;

                    cmd.GetTemporaryRT(_finalId, finalBlurRTDesc, FilterMode.Bilinear);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            cmd.GetTemporaryRT(_tempBlurId1, camTexDesc, FilterMode.Bilinear);

            cmd.GetTemporaryRT(_tempBlurId2, camTexDesc, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            var opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            var screenSize = new Vector2Int(opaqueDesc.width, opaqueDesc.height);
            opaqueDesc.enableRandomWrite = true;

            switch (_blurType)
            {
                case BlurType.Gaussian:
                    RenderGaussianPyramid(cmd, screenSize,
                                          SourceColorTarget,
                                          DownsampleTarget,
                                          BlurTarget,
                                          FinalTarget);
                    break;
                case BlurType.Kawase:
                    RenderKawaseBlur(cmd, screenSize,
                                     SourceColorTarget,
                                     DownsampleTarget,
                                     BlurTarget,
                                     FinalTarget);
                    break;
                default:
                    _blurType = BlurType.Kawase;
                    break;
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void RenderGaussianPyramid(CommandBuffer cmd, Vector2Int size,
                                           RenderTargetIdentifier sourceRT,
                                           RenderTargetIdentifier downsampleRT,
                                           RenderTargetIdentifier blurRT,
                                           RenderTargetIdentifier upsampleRT)
        {
            int srcMipLevel = 0;
            int srcMipWidth = size.x;
            int srcMipHeight = size.y;

            int dstMipLevel = 1;
            int maxMipLevel = _totalMips - 1;
            bool firstDownsample = true;

            bool MaxMipReached()
            {
                return srcMipLevel == maxMipLevel;
            }

            var kColorDownsample = _computeShader.FindKernel("KColorDownsample");
            var kColorGaussian = _computeShader.FindKernel("KColorGaussian");
            var kColorUpsample = _computeShader.FindKernel("KColorUpsample");


            // for (var i = 0; i < maxMipLevel; i++)
            while (srcMipWidth > (size.x >> maxMipLevel) || srcMipHeight > (size.y >> maxMipLevel))
            {
                int dstMipWidth = Mathf.Max(1, srcMipWidth >> 1);
                int dstMipHeight = Mathf.Max(1, srcMipHeight >> 1);

                Vector2Int numthreads = default;
                numthreads.x = Mathf.CeilToInt(dstMipWidth / 8f);
                numthreads.y = Mathf.CeilToInt(dstMipHeight / 8f);

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region Downsample

                // -----------------------------------------------------------------------------------------------------------------------------------

                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));

                if (firstDownsample)
                {
                    cmd.EnableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Source",
                                               sourceRT); // TEXTURE2D_X _Source
                    cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Mip0", blurRT, 0);
                }
                else
                {
                    cmd.DisableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Source", blurRT,
                                               srcMipLevel); // RW_TEXTURE2D_X _Source
                }

                cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Destination", downsampleRT, dstMipLevel);

                // Using dstMipWidth & dstMipHeight for threadGroups(numthreads.x, numthreads.y) is
                // what makes the compute shader write to a smaller region of the downsampleTargetID.
                cmd.DispatchCompute(_computeShader, kColorDownsample, numthreads.x, numthreads.y, 1);

                #endregion

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                // Set blur src mip = dst mip
                // srcMipLevel = dstMipLevel;
                srcMipLevel++;

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region BlurDown

                // -----------------------------------------------------------------------------------------------------------------------------------

                // The data we want to blur is in the area defined by (dstMipWidth, dstMipHeight) - the area we wrote the downsample texture to.
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));

                // Set _Source to the downsampled texture.
                cmd.SetComputeTextureParam(_computeShader, kColorGaussian, "_Source", downsampleRT, srcMipLevel);

                cmd.SetComputeTextureParam(_computeShader, kColorGaussian, "_Destination", blurRT, dstMipLevel);


                cmd.DispatchCompute(_computeShader, kColorGaussian, numthreads.x, numthreads.y, 1);

                #endregion

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                // Do not increase srcMipLevel if maxMipReached.
                if (MaxMipReached()) break;
                // if (srcMipLevel == maxMipLevel) break;

                /* Prepare for next loop */

                // Decrease srcMipWidth & Height for when dstMipWidth & Height are set at start of next loop
                srcMipWidth  >>= 1;
                srcMipHeight >>= 1;

                // Set downsample dstMipLevel to srcMipLevel + 1
                dstMipLevel++;

                if (firstDownsample)
                {
                    firstDownsample = false;
                }
            }

            // Set upsample dstMipLevel = srcMipLevel - 1 ... = maxMipLevel - 1
            dstMipLevel = srcMipLevel - 1;

            // Viewport Size
            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));

            for (var i = maxMipLevel; i > 0; i--)
                // while (srcMipWidth < size.x || srcMipHeight < size.y)
            {
                int dstMipWidth = Mathf.Min(size.x, srcMipWidth << 1);   // srcMipWidth * 2, ceiling of screen width
                int dstMipHeight = Mathf.Min(size.y, srcMipHeight << 1); // srcMipWidth * 2, ceiling of screen height

                var numthreadsX = Mathf.CeilToInt(dstMipWidth / 8f);
                var numthreadsY = Mathf.CeilToInt(dstMipHeight / 8f);

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region Upsample

                // ---------------------------------------------------------------------------------------------------------------------------------------

                // Smaller Texture Params
                var lowSourceSize = new Vector2(srcMipWidth, srcMipHeight);
                var lowSourceTexelSize = lowSourceSize / size;
                // xy: low src size, zw: low src texel size
                var blurBicubic = new Vector4(srcMipWidth, srcMipHeight, lowSourceTexelSize.x, lowSourceTexelSize.y);
                cmd.SetComputeVectorParam(_computeShader, "_BlurBicubicParams", blurBicubic);

                // Larger Texture Params
                var highSourceSize = new Vector2(dstMipWidth, dstMipHeight);
                var highSourceTexelSize = highSourceSize / size;
                // xy; high src size, zw: high src texel size
                var dstSize = new Vector4(dstMipWidth, dstMipHeight, highSourceTexelSize.x, highSourceTexelSize.y);

                // Ensure dstSize.xy = size for last upsample
                if (dstMipLevel == 0)
                {
                    dstSize = new Vector4(size.x, size.y, highSourceTexelSize.x, highSourceTexelSize.y);
                }

                cmd.SetComputeVectorParam(_computeShader, "_TexelSize", dstSize);

                cmd.SetComputeFloatParam(_computeShader, "_Scatter", 1);

                cmd.SetComputeTextureParam(_computeShader, kColorUpsample, "_LowResMip", blurRT, srcMipLevel);
                cmd.SetComputeTextureParam(_computeShader, kColorUpsample, "_HighResMip", upsampleRT, dstMipLevel);


                // threadGroups divisor must match upsample KERNEL_SIZE.
                if (dstMipLevel == 0)
                {
                    numthreadsX = Mathf.CeilToInt(size.x / 8f);
                    numthreadsY = Mathf.CeilToInt(size.y / 8f);
                }

                cmd.DispatchCompute(_computeShader, kColorUpsample, numthreadsX, numthreadsY, 1);

                #endregion

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                // If dstMipLevel = 0 then no further blurring/upsampling is needed.
                if (dstMipLevel == 0) break;

                // Set blur srcMipLevel = dstMipLevel
                srcMipLevel--;

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region BlurUp

                // -----------------------------------------------------------------------------------------------------------------------------------

                // The data we want to blur is in the area defined by (dstMipWidth, dstMipHeight) ------- the area we wrote the upsample texture to.
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));
                // Set _Source to the upsampled texture.
                cmd.SetComputeTextureParam(_computeShader, kColorGaussian, "_Source", upsampleRT, srcMipLevel);

                cmd.SetComputeTextureParam(_computeShader, kColorGaussian, "_Destination", blurRT, dstMipLevel);

                cmd.DispatchCompute(_computeShader, kColorGaussian, numthreadsX, numthreadsY, 1);

                #endregion

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                /* Prepare for next loop */

                // Set upsample dstMipLevel = srcMipLevel - 1
                dstMipLevel--;

                // Increase srcMipWidth & Height for when dstMipWidth & Height are set at start of next loop
                srcMipWidth  <<= 1;
                srcMipHeight <<= 1;
            }

            // SetGlobalTexture _FinalBlur
            cmd.SetGlobalTexture(_finalId, upsampleRT);
        }

        private void RenderKawaseBlur(CommandBuffer cmd, Vector2Int size,
                                      RenderTargetIdentifier sourceRT,
                                      RenderTargetIdentifier tempRT1,
                                      RenderTargetIdentifier tempRT2,
                                      RenderTargetIdentifier finalRT)
        {
            // Downsample size
            var width = size.x >> 1;
            var height = size.y >> 1;

            var kBlur = _computeShader.FindKernel("KBlur");
            var kBlurUpsample = _computeShader.FindKernel("KBlurUpsample");

            // Set dispatch threadGroups to downsampled size
            Vector2Int numthreads = default;
            numthreads.x = Mathf.CeilToInt(width / 8f);
            numthreads.y = Mathf.CeilToInt(height / 8f);

            // Set _Size to downsample size
            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(width, height, 0, 0));

            // Pass 1
            cmd.SetComputeFloatParam(_computeShader, "offset", 0.5f);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", sourceRT);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT1);

            cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);

            // Pass 2

            // Set Size to full-size for correct texelSize
            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(size.x, size.y, 0, 0));

            cmd.SetComputeFloatParam(_computeShader, "offset", 1.5f);
            // cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", Texture2D.blackTexture);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT1);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT2);

            cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);

            // Pass 3
            cmd.SetComputeFloatParam(_computeShader, "offset", 2.5f);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT2);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT1);

            cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);

            if (_passes > 3)
            {
                // Pass 4
                cmd.SetComputeFloatParam(_computeShader, "offset", 2.5f);
                cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT1);
                cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT2);

                cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);

                if (_passes > 4)
                {
                    // Pass 5
                    cmd.SetComputeFloatParam(_computeShader, "offset", 3.5f);
                    cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT2);
                    cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT1);

                    cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);
                }
            }

            /* Upsample Pass */

            numthreads.x = Mathf.CeilToInt(size.x / 8f);
            numthreads.y = Mathf.CeilToInt(size.y / 8f);

            // Default
            var upsampleRT = tempRT1;
            // Odd no. of passes
            if (_passes % 2 != 0)
            {
            }
            else
            {
                // Even no.of passes
                upsampleRT = tempRT2;
            }

            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(size.x << 1, size.y << 1, 0, 0));
            cmd.SetComputeFloatParam(_computeShader, "threshold", _threshold);
            cmd.SetComputeFloatParam(_computeShader, "intensity", _intensity);
            cmd.SetComputeTextureParam(_computeShader, kBlurUpsample, "_Source", upsampleRT);
            cmd.SetComputeTextureParam(_computeShader, kBlurUpsample, "_Result", finalRT);

            cmd.DispatchCompute(_computeShader, kBlurUpsample, numthreads.x, numthreads.y, 1);
            cmd.SetGlobalTexture(_finalId, finalRT);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_sourceId);
            cmd.ReleaseTemporaryRT(_tempBlurId1);
            cmd.ReleaseTemporaryRT(_tempBlurId2); // low mip blur texture
            cmd.ReleaseTemporaryRT(_finalId);     // upsampled blur texture
        }
    }
}