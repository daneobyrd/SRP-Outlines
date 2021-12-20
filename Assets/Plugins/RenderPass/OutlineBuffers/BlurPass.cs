// Quick-access Copy/Paste grids
// ╔══╦══╗  ┌──┬──┐
// ╠══╬══╣  │  │  │
// ║  ║  ║  ├──┼──┤
// ╚══╩══╝  └──┴──┘

/* Step-by-step for void RenderColorGaussianPyramid()
Mip Level 0 ─→ 1 (Enable COPY_MIP_0)
 *  0. bool firstDownsample = true;                                                     
 *  Downsample                                                                              
 *  1. Set _Size to full-screen                                                         
 *  2. Copy _Source (sourceRT) to _Mip0 (blurRT) ───→ Copy TEXTURE2D_X to RW_TEXTURE2D_X
 *         Note: if(COPY_MIP_0) then _Mip0 is used as downsample input                  
 *  3. Set downsample output (_Destination) as tempDownsampleRT mipLevel: 0             
 *  4. Dispatch compute using _Size/2 (dstMipWidth, dstMipHeight)                       
 *     - This writes _Mip0 (a copy of sourceRT) to an area half its size.
 * 
 *  ┌──────────────────────────────┐
 *  │ A                       ↙    │
 *  │                      ↙       │
 *  │                   ↙          │
 *  ╔══════════════╗               │
 *  ║ B            ║               │
 *  ║              ║               │
 *  ║              ║               │
 *  ╚══════════════╝ ──────────────┘
 * 
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
 *  ┌──────────────┐       ╔══════════════╗
 *  │ B            │       ║ C            ║
 *  │              │ ────→ ║              ║
 *  │              │       ║              ║
 *  └──────────────┘       ╚══════════════╝
 * 
Mip Level 1 ─→ 2 (Disable COPY_MIP_0)
 *  1. Set downsample input (_Source) as blurRT (previous blur output) mipLevel: 1
 *  2. Set downsample output (_Destination) as tempDownsampleRT mipLevel: 1
 *  3. Continue for each mip level
         
Upsample
╔══════════════════════════════╗
║                          ↗   ║
║                      ↗       ║
║                  ↗           ║
┌──────────────┐               ║
│              │               ║
│              │               ║ 
│              │               ║
└──────────────┘ ══════════════╝
*/

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum BlurType
{
    Gaussian = 0,
    Kawase = 1
}

/// <inheritdoc />
public class BlurPass : ScriptableRenderPass
{
    private string _profilerTag;
    private ComputeShader _computeShader;
    private BlurType _blurType;

    #region Gaussian Properties

    private int _totalMips;
    private int _sourceId;
    private readonly int _downsampleId = Shader.PropertyToID("_DownsampleTex");
    private readonly int _blurId = Shader.PropertyToID("_BlurResults");
    private readonly int _finalId = Shader.PropertyToID("_FinalBlur");

    private RenderTargetIdentifier sourceColorTarget => new(_sourceId); // _OutlineOpaque
    private RenderTargetIdentifier downsampleTarget  => new(_downsampleId);
    private RenderTargetIdentifier blurTarget        => new(_blurId);
    private RenderTargetIdentifier finalTarget       => new(_finalId);

    #endregion

    #region Kawase Properties

    private float _threshold;
    private float _intensity;
    private const bool CopyToFrameBuffer = false;

    #endregion

    public BlurPass(string profilerTag)
    {
        _profilerTag = profilerTag;
    }

    public void Init(string sourceName, BlurType blurType, ComputeShader computeShader, int pyramidLevels)
    {
        _sourceId      = Shader.PropertyToID(sourceName);
        _blurType      = blurType;
        _computeShader = computeShader;
        _totalMips     = pyramidLevels;
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
        sourceDesc.mipCount          = 0;
        sourceDesc.useMipMap         = false;

        // Source texture
        cmd.GetTemporaryRT(_sourceId, sourceDesc, FilterMode.Bilinear);

        switch (_blurType)
        {
            case BlurType.Gaussian:
            {
                camTexDesc.mipCount  = _totalMips;
                camTexDesc.useMipMap = true; // Do not use autoGenerateMips: does not reliably generate mips.

                // Downsample texture
                cmd.GetTemporaryRT(_downsampleId, camTexDesc, FilterMode.Point);

                // Blur low mip texture
                cmd.GetTemporaryRT(_blurId, camTexDesc, FilterMode.Point);

                // Blur high mip/upsample texture
                var upsampleRTDesc = camTexDesc;
                upsampleRTDesc.mipCount = _totalMips - 1;

                cmd.GetTemporaryRT(_finalId, upsampleRTDesc, FilterMode.Bilinear);
                break;
            }
            case BlurType.Kawase:
            {
                cmd.GetTemporaryRT(_blurId, camTexDesc, FilterMode.Bilinear);

                cmd.GetTemporaryRT(_finalId, camTexDesc, FilterMode.Bilinear);
                break;
            }
        }
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

        RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
        var screenSize = new Vector2Int(opaqueDesc.width, opaqueDesc.height);
        opaqueDesc.enableRandomWrite = true;

        switch (_blurType)
        {
            case BlurType.Gaussian:
                RenderGaussianPyramid(cmd, screenSize,
                                      sourceColorTarget,
                                      downsampleTarget,
                                      blurTarget,
                                      finalTarget);
                break;
            case BlurType.Kawase:
                RenderKawaseBlur(cmd, screenSize,
                                 blurTarget,
                                 finalTarget);
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

            var numthreadsX = Mathf.CeilToInt(dstMipWidth / 8f);
            var numthreadsY = Mathf.CeilToInt(dstMipHeight / 8f);
            
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

            #region Downsample

            // -----------------------------------------------------------------------------------------------------------------------------------

            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));

            if (firstDownsample)
            {
                cmd.EnableShaderKeyword("COPY_MIP_0");
                cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Source", sourceRT);                     // TEXTURE2D_X _Source
                cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Mip0", blurRT, 0);
            }
            else
            {
                cmd.DisableShaderKeyword("COPY_MIP_0");
                cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Source", blurRT, srcMipLevel); // RW_TEXTURE2D_X _Source
            }

            cmd.SetComputeTextureParam(_computeShader, kColorDownsample, "_Destination", downsampleRT, dstMipLevel);

            // Using dstMipWidth & dstMipHeight for threadGroups(numthreadsX, numthreadsY) is
            // what makes the compute shader write to a smaller region of the downsampleTargetID.
            cmd.DispatchCompute(_computeShader, kColorDownsample, numthreadsX, numthreadsY, 1);

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

            
            cmd.DispatchCompute(_computeShader, kColorGaussian, numthreadsX, numthreadsY, 1);

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
                                  RenderTargetIdentifier tempRT1,
                                  RenderTargetIdentifier tempRT2)
    {
        var width = size.x >>= 1;
        var height = size.y >>= 1;

        var kBlurKernel = _computeShader.FindKernel("KBlur");
        var numthreadsX = Mathf.CeilToInt(width / 8f);
        var numthreadsY = Mathf.CeilToInt(height / 8f);

        
        cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(width, height, 0, 0));

        // Pass 1
        cmd.SetComputeFloatParam(_computeShader, "offset", 0.5f);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Source", sourceColorTarget);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Result", tempRT2);

        cmd.DispatchCompute(_computeShader, kBlurKernel, numthreadsX, numthreadsY, 1);
        
        // Pass 2
        cmd.SetComputeFloatParam(_computeShader, "offset", 1.5f);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Source", tempRT2);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Result", tempRT1);

        cmd.DispatchCompute(_computeShader, kBlurKernel, numthreadsX, numthreadsY, 1);
        
        // Pass 3
        cmd.SetComputeFloatParam(_computeShader, "offset", 2.5f);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Source", tempRT1);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Result", tempRT2);

        cmd.DispatchCompute(_computeShader, kBlurKernel, numthreadsX, numthreadsY, 1);

        // Pass 4
        cmd.SetComputeFloatParam(_computeShader, "offset", 2.5f);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Source", tempRT2);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Result", tempRT1);

        cmd.DispatchCompute(_computeShader, kBlurKernel, numthreadsX, numthreadsY, 1);
        
        // Final Pass
        numthreadsX =   Mathf.CeilToInt(size.x / 8f);
        numthreadsY =   Mathf.CeilToInt(size.y / 8f);
        
        cmd.SetComputeFloatParam(_computeShader, "offset", 3.5f);
        cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(size.x, size.y, 0, 0));

        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Source", tempRT1);
        cmd.SetComputeTextureParam(_computeShader, kBlurKernel, "_Result", tempRT2);

        cmd.DispatchCompute(_computeShader, kBlurKernel, numthreadsX, numthreadsY, 1);
        
        cmd.SetGlobalTexture(_finalId, tempRT2);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(_sourceId);
        cmd.ReleaseTemporaryRT(_downsampleId);
        cmd.ReleaseTemporaryRT(_blurId);  // low mip blur texture
        cmd.ReleaseTemporaryRT(_finalId); // upsampled blur texture
    }
}