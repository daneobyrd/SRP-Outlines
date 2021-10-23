// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Shaders.OutlineBuffers
{
    public class FullscreenEdgeDetectionBlit : ScriptableRenderPass
    {
        private OutlineSettings _settings;

        string m_ProfilerTag;
        private Material m_Material;

        private ScriptableRenderer m_Renderer;
        private bool m_HasDepth;

        RenderTargetHandle m_Source; // _BlurResults
        RenderTargetHandle m_OutlineDepth; // _OutlineDepth

        DebugTargetView debugTargetView => _settings.debugTargetView;

        RenderTargetHandle m_TemporaryColorTexture; // _OutlineTexture to be generated
        RenderTargetHandle m_DestinationTexture; // Camera target
        bool m_newTexture; // Create outline texture?

        public FullscreenEdgeDetectionBlit(string name)
        {
            do
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                m_ProfilerTag = name;
            } while (renderPassEvent != RenderPassEvent.BeforeRenderingTransparents && m_ProfilerTag != name);
        }

        // public void Init(Material initMaterial, ScriptableRenderer newRenderer, bool depth)
        // {
        //     m_Material = initMaterial;
        //     m_Renderer = newRenderer;
        //     m_HasDepth = depth;
        // }

        public void Init(Material initMaterial, string textureName, bool newTexture)
        {
            m_Material = initMaterial;
            m_Source = new RenderTargetHandle(new RenderTargetIdentifier(textureName));
            m_Source.Init(textureName);
            m_Renderer = null;
            m_newTexture = newTexture;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // This might be auto-handled by RenderTargetHandle.Identifier(), not counting the conditional check for m_IsDepth (which needs to be changed as well).
            switch (m_Source.id)
            {
                // When we have a RenderTexture as m_Source
                case -2 when m_newTexture:
                    cmd.GetTemporaryRT(m_Source.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 24, FilterMode.Point,
                        RenderTextureFormat.ARGBFloat);
                    break;
                case -1:
                    m_Source = new RenderTargetHandle(m_HasDepth ? m_Renderer.cameraDepthTarget : m_Renderer.cameraColorTarget);
                    break;
            }

            if (m_HasDepth)
            {
                ConfigureTarget(m_Source.Identifier(), depthAttachment: m_OutlineDepth.Identifier());
            }
            else
            {
                ConfigureTarget(m_Source.id);
            }

            if (m_newTexture) ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null) return;
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            textureDescriptor.depthBufferBits = 0;

            Camera camera = renderingData.cameraData.camera;

            cmd.SetGlobalTexture("_MainTex", m_Source.id);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (m_Source.id != -1 && m_newTexture) cmd.ReleaseTemporaryRT(m_Source.id);
        }
    }
}