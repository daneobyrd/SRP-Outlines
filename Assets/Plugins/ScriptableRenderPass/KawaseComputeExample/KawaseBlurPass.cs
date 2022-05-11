// Quick-access Copy/Paste grids for comments
// ╔══╦══╗  ┌──┬──┐
// ╠══╬══╣  │  │  │
// ║  ║  ║  ├──┼──┤
// ╚══╩══╝  └──┴──┘

using System;

namespace Plugins.ScriptableRenderPass
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public sealed class KawaseBlurPass : ScriptableRenderPass
    {
        private bool _enabled;
        private readonly string _profilerTag;
        private Material _material;
        private ComputeShader _computeShader;

        private int _sourceId;
        private readonly int _tempBlurId1 = Shader.PropertyToID("_tempBlur1");
        private readonly int _tempBlurId2 = Shader.PropertyToID("_tempBlur2");
        private readonly int _finalId = Shader.PropertyToID("_FinalBlur");

        private RenderTargetIdentifier SourceColorTarget;// => new(_sourceId);
        private RenderTargetIdentifier DownsampleTarget  => new(_tempBlurId1);
        private RenderTargetIdentifier BlurTarget        => new(_tempBlurId2);
        private RenderTargetIdentifier FinalTarget       => new(_finalId);

        private RenderTargetIdentifier cameraColorTexture;
        
        #region Kawase Properties

        private float _threshold;
        private float _intensity;
        private int _passes;

        #endregion

        public KawaseBlurPass(string profilerTag)
        {
            _profilerTag = profilerTag;
        }

        // Prev used for drawing to texture but not blitting that texture to screen in this pass.
        public void Setup(string sourceName, ComputeShader computeShader,
                          int blurPasses, float threshold, float intensity)
        {
            _sourceId         = Shader.PropertyToID(sourceName);
            SourceColorTarget = new RenderTargetIdentifier(_sourceId);
            
            _computeShader    = computeShader;
            _passes           = blurPasses;
            _threshold        = threshold;
            _intensity        = intensity;
        }

        // Provide material for blitting blur to screen.
        public void Setup(Material blitMaterial, ComputeShader computeShader,
                          int blurPasses, float threshold, float intensity)
        {
            // -1 for cameraTarget
            // _sourceId         = -1;
            // SourceColorTarget = new RenderTargetIdentifier(_sourceId);
            
            _material         = blitMaterial;
            _computeShader    = computeShader;
            _passes           = blurPasses;
            _threshold        = threshold;
            _intensity        = intensity;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var camTexDesc = cameraTextureDescriptor;
            camTexDesc.colorFormat       = RenderTextureFormat.ARGB32;
            camTexDesc.msaaSamples       = 1;
            // camTexDesc.mipCount          = _passes;
            // camTexDesc.useMipMap         = true;
            camTexDesc.enableRandomWrite = true;

            // Source texture
            cmd.GetTemporaryRT(_sourceId, camTexDesc, FilterMode.Bilinear);
            // Temp intermediate textures
            cmd.GetTemporaryRT(_tempBlurId1, camTexDesc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_tempBlurId2, camTexDesc, FilterMode.Bilinear);
            // Final texture
            cmd.GetTemporaryRT(_finalId, camTexDesc, FilterMode.Bilinear);

            ConfigureTarget(_finalId);
            ConfigureClear(ClearFlag.None, clearColor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", _material, GetType().Name);
                return;
            }
            
            cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

            CameraData cameraData = renderingData.cameraData;

            RenderTextureDescriptor opaqueDesc = cameraData.cameraTargetDescriptor;
            opaqueDesc.enableRandomWrite = true;
            
            Vector2Int screenSize = new Vector2Int(opaqueDesc.width, opaqueDesc.height);

            RenderKawaseBlur(cmd, screenSize,
                             cameraColorTexture,
                             DownsampleTarget,
                             BlurTarget,
                             FinalTarget);

            // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material, default, 0);
            
            Blit(cmd, ref renderingData, _material, 0);
            
            context.ExecuteCommandBuffer(cmd);
            // cmd.Clear();
            
            CommandBufferPool.Release(cmd);
        }

        private void RenderKawaseBlur(CommandBuffer cmd, Vector2Int size,
                                      RenderTargetIdentifier sourceRT,
                                      RenderTargetIdentifier tempRT1,
                                      RenderTargetIdentifier tempRT2,
                                      RenderTargetIdentifier finalRT)
        {
            // RenderTargetIdentifier blackTex = new RenderTargetIdentifier(Texture2D.blackTexture);
            RenderTargetIdentifier linearGrayTex = new RenderTargetIdentifier(Texture2D.linearGrayTexture);
            
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

            // Set Size to full-size for correct texel_size
            cmd.SetComputeVectorParam(_computeShader, "_Size", new Vector4(size.x, size.y, 0, 0));

            cmd.SetComputeFloatParam(_computeShader, "offset", 1.5f);
            // cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", blackTex);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT1);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT2);

            cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);

            // Pass 3
            cmd.SetComputeFloatParam(_computeShader, "offset", 2.5f);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT2);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT1);

            cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);

            if (_passes <= 3) return;
            // Pass 4
            cmd.SetComputeFloatParam(_computeShader, "offset", 2.5f);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT1);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT2);

            cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);

            if (_passes <= 4) return;
            // Pass 5
            cmd.SetComputeFloatParam(_computeShader, "offset", 3.5f);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Source", tempRT2);
            cmd.SetComputeTextureParam(_computeShader, kBlur, "_Result", tempRT1);

            cmd.DispatchCompute(_computeShader, kBlur, numthreads.x, numthreads.y, 1);


            /* Upsample Pass */

            numthreads.x = Mathf.CeilToInt(size.x / 8f);
            numthreads.y = Mathf.CeilToInt(size.y / 8f);

            // Default
            var upsampleRT = tempRT1;
            // Odd no. of passes
            if (_passes % 2 == 0)
            {
                // Even no.of passes
                upsampleRT = tempRT2;
            }

            // Can't remember why upsample _Size had to be twice the size of the viewport
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
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (_sourceId != -1)
            {
                cmd.ReleaseTemporaryRT(_sourceId);
            }

            cmd.ReleaseTemporaryRT(_tempBlurId1);
            cmd.ReleaseTemporaryRT(_tempBlurId2);
            cmd.ReleaseTemporaryRT(_finalId);     // upsampled blur texture
        }
    }
}