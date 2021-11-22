// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
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

        private int _sourceIntId;
        private int _tempDownsampleIntId = Shader.PropertyToID("_DownsampleTex");
        private int _blurIntId = Shader.PropertyToID("_BlurResults");
        private int _finalIntId = Shader.PropertyToID("_BlurUpsampleTex");

        private RenderTargetIdentifier sourceColorTargetID => new(_sourceIntId); // _OutlineOpaque
        private RenderTargetIdentifier tempDownsampleTargetID => new(_tempDownsampleIntId);
        private RenderTargetIdentifier blurTargetID => new(_blurIntId);
        private RenderTargetIdentifier finalTargetID => new(_finalIntId);
        
        private RenderTextureDescriptor _camTexDesc;

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

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _camTexDesc = cameraTextureDescriptor;
            var width = _camTexDesc.width;
            var height = _camTexDesc.height;
            _camTexDesc.msaaSamples = 1;
            _camTexDesc.mipCount = 8;
            _camTexDesc.colorFormat = RenderTextureFormat.ARGBFloat;
            _camTexDesc.depthBufferBits = 0;
            _camTexDesc.dimension = TextureDimension.Tex2DArray;
            _camTexDesc.enableRandomWrite = true;
            
            // Source TEXTURE ARRAY
            cmd.GetTemporaryRT(_sourceIntId, _camTexDesc, FilterMode.Point);
            // Downsample TEXTURE ARRAY
            cmd.GetTemporaryRT(_tempDownsampleIntId, _camTexDesc, FilterMode.Point);
            // Blur smallest downsample texture ARRAY
            cmd.GetTemporaryRT(_blurIntId, _camTexDesc, FilterMode.Point);
            // Blur upsample texture
            cmd.GetTemporaryRT(_finalIntId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, 1, true);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            var width  = _camTexDesc.width;
            var height = _camTexDesc.height;

            
            // RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            // opaqueDesc.depthBufferBits = 0;
            
            RenderColorGaussianPyramid(cmd, new Vector2Int(width, height), sourceColorTargetID, tempDownsampleTargetID, blurTargetID, finalTargetID);

            cmd.SetGlobalTexture(_finalIntId, finalTargetID);
            // Added blit here for testing - haven't gotten to try it yet because scene is freezing when I switch renderer data
            Blit(cmd, finalTargetID, -1);

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
            
            bool firstIteration = true;
            
            while (srcMipWidth >= srcMipWidth/8 || srcMipHeight >= srcMipHeight/8)
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
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", sourceRT, 0);
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Mip0", blurRT, 0);
                }
                else
                {
                    cmd.DisableShaderKeyword("COPY_MIP_0");
                    cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Source", blurRT, srcMipLevel);
                }
                cmd.SetComputeTextureParam(_computeShader, downsampleKernel, "_Destination", tempDownsampleRT, srcMipLevel);
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));
                
                // This is what makes the compute write to a smaller region of the tempDownsampleTargetID.
                cmd.DispatchCompute(_computeShader, downsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);
                
                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                // Blur
                // -----------------------------------------------------------------------------------------------------------------------------------
                var gaussKernel = _computeShader.FindKernel("KColorGaussian");
                
                cmd.DisableShaderKeyword("COPY_MIP_0");
                // The data we want to blur is in the area defined by (dstMipWidth, dstMipHeight)
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(dstMipWidth, dstMipHeight, 0, 0));
                // Set _Source to the downsampled texture
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Source", tempDownsampleRT, srcMipLevel);
                // srcMipLevel + 1 because we are writing to the next mip level.
                cmd.SetComputeTextureParam(_computeShader, gaussKernel, "_Destination", blurRT, srcMipLevel + 1);
                
                cmd.DispatchCompute(_computeShader, gaussKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);
                
                srcMipLevel++;
                // Bitwise operation; same as srcMipWidth /= 2.
                srcMipWidth = srcMipWidth >> 1;
                srcMipHeight = srcMipHeight >> 1;

                firstIteration = false;
            }
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Upsample Blurred Downsampled Texture
            // -----------------------------------------------------------------------------------------------------------------------------------
            var upsampleKernel = _computeShader.FindKernel("KColorUpsample");
            while (srcMipWidth < size.x || srcMipHeight < size.y)
            {
                srcMipLevel = Mathf.Max(0, srcMipLevel);
                // dstMipWidth&Height are now 2x srcMipWidth&Height
                int dstMipWidth  = Mathf.Min(size.x, srcMipWidth << 1);
                int dstMipHeight = Mathf.Min(size.y, srcMipHeight << 1);
                
                cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(srcMipWidth, srcMipHeight, 0, 0));
                cmd.SetComputeVectorParam(_computeShader, "_UpsampleSize", new Vector4(dstMipWidth, dstMipHeight, 0, 0));
                cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_LowMip", blurRT, srcMipLevel);
                cmd.SetComputeTextureParam(_computeShader, upsampleKernel, "_HighMip", finalRT, srcMipLevel - 1);
                cmd.DispatchCompute(_computeShader, upsampleKernel, Mathf.CeilToInt(dstMipWidth / 8f), Mathf.CeilToInt(dstMipHeight / 8f), 1);

                srcMipLevel--;
                // Bitwise operation; same as srcMipWidth *= 2.
                srcMipWidth = srcMipWidth << 1;
                srcMipHeight = srcMipHeight << 1;
            }
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