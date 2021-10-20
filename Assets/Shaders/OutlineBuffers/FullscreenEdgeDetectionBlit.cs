// This is a modified version of a Scriptable Render Pass written by Harry Heath.
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
        private bool m_IsDepth;

        int m_TextureId = -1;
        RenderTargetIdentifier m_Source;

        // TODO: Blit blur to camera for debug view before working on edge detection.
        private bool blurDebugView => _settings.edgeSettings.blurDebugView; 
        RenderTargetHandle m_TemporaryColorTexture;
        RenderTargetHandle m_DestinationTexture;
        bool m_newTexture;

        public FullscreenEdgeDetectionBlit(string name)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            m_ProfilerTag = name;
        }

        public void Init(Material initMaterial, ScriptableRenderer newRenderer, bool depth)
        {
            m_Material = initMaterial;
            m_Renderer = newRenderer;
            m_TextureId = -1;
            m_IsDepth = depth;
        }

        public void Init(Material initMaterial, string textureName, bool newTexture)
        {
            m_Material = initMaterial;
            m_TextureId = Shader.PropertyToID(textureName);
            m_Source = new RenderTargetIdentifier(m_TextureId);
            m_Renderer = null;
            m_newTexture = newTexture;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_TextureId != -1 && m_newTexture)
            {
                cmd.GetTemporaryRT(m_TextureId, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 24, FilterMode.Point,
                    RenderTextureFormat.ARGBFloat);
            }
            else if (m_TextureId == -1)
            {
                m_Source = m_IsDepth ? m_Renderer.cameraDepthTarget : m_Renderer.cameraColorTarget;
            }

            ConfigureTarget(m_Source);
            if (m_newTexture) ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            textureDescriptor.depthBufferBits = 0;

            Camera camera = renderingData.cameraData.camera;

            cmd.SetGlobalTexture("_MainTex", m_Source);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (m_TextureId != -1 && m_newTexture) cmd.ReleaseTemporaryRT(m_TextureId);
        }
    }
}