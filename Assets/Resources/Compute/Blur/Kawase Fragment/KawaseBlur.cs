// Source: https://github.com/Unity-Technologies/UniversalRenderingExamples/blob/master/Assets/_CompletedDemos/3DCharacterUI/KawaseBlur.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class KawaseBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial = null;

        [Range(2, 15)] public int blurPasses = 1;

        [Range(1, 4)] public int downsample = 1;
        public bool copyToFramebuffer;
        public string targetName = "_blurTexture";
    }

    public KawaseBlurSettings settings = new KawaseBlurSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material blurMaterial;
        public int passes;
        public int downsample;
        public bool copyToFramebuffer;
        public string targetName;
        string profilerTag;

        int tmpId1;
        int tmpId2;

        RenderTargetIdentifier tmpRT1;
        RenderTargetIdentifier tmpRT2;

        RenderTargetIdentifier cameraColorTexture;

        public CustomRenderPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width / downsample;
            var height = cameraTextureDescriptor.height / downsample;

            tmpId1 = Shader.PropertyToID("tmpBlurRT1");
            tmpId2 = Shader.PropertyToID("tmpBlurRT2");
            cmd.GetTemporaryRT(tmpId1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(tmpId2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            tmpRT1 = new RenderTargetIdentifier(tmpId1);
            tmpRT2 = new RenderTargetIdentifier(tmpId2);

            ConfigureTarget(tmpRT1);
            ConfigureTarget(tmpRT2);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            // first pass
            // cmd.GetTemporaryRT(tmpId1, opaqueDesc, FilterMode.Bilinear);
            cmd.SetGlobalFloat("_offset", 1.5f);
            cmd.Blit(cameraColorTexture, tmpRT1, blurMaterial);

            for (var i = 1; i < passes - 1; i++)
            {
                cmd.SetGlobalFloat("_offset", 0.5f + i);
                cmd.Blit(tmpRT1, tmpRT2, blurMaterial);

                // pingpong
                (tmpRT1, tmpRT2) = (tmpRT2, tmpRT1);
            }

            // final pass
            cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);
            if (copyToFramebuffer)
            {
                cmd.Blit(tmpRT1, cameraColorTexture, blurMaterial);
            }
            else
            {
                cmd.Blit(tmpRT1, tmpRT2, blurMaterial);
                cmd.SetGlobalTexture(targetName, tmpRT2);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    CustomRenderPass _scriptablePass;

    public override void Create()
    {
        _scriptablePass = new CustomRenderPass("KawaseBlur");
        _scriptablePass.blurMaterial = settings.blurMaterial;
        _scriptablePass.passes = settings.blurPasses;
        _scriptablePass.downsample = settings.downsample;
        _scriptablePass.copyToFramebuffer = settings.copyToFramebuffer;
        _scriptablePass.targetName = settings.targetName;

        _scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_scriptablePass);
    }
}