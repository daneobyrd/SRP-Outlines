// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resources.RenderPass.OutlineBuffers
{
    public class FullscreenEdgeDetectionBlit : ScriptableRenderPass
    {
        private OutlineSettings _settings;

        private string _profilerTag;
        private Material _material;
        private DebugTargetView debugTargetView => _settings.debugTargetView;

        private ScriptableRenderer _renderer;
        private bool _hasDepth;
        private ComputeShader computeShader => _settings.edgeSettings.computeLines;

        private RenderTargetHandle _source; // _BlurResults or _OutlineOpaque
        private RenderTargetHandle _outlineHandle = new (new RenderTargetIdentifier("_OutlineTexture"));
        private RenderTargetHandle _outlineDepth = new(new RenderTargetIdentifier("_OutlineDepth"));
        private RenderTargetHandle _combinedHandle;
        
        public FullscreenEdgeDetectionBlit(string name)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _profilerTag = name;
        }

        public void Init(Material initMaterial, ScriptableRenderer newRenderer, string sourceTextureName, bool hasDepth)
        {
            _material = initMaterial;
            _source = new RenderTargetHandle(new RenderTargetIdentifier(sourceTextureName));
            _source.Init(sourceTextureName);
            _renderer = newRenderer;
            _hasDepth = hasDepth;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _outlineHandle.Init("_OutlineTexture");
            // if (m_HasDepth) m_OutlineDepth.Init("_OutlineDepth");
            
            cmd.GetTemporaryRT(_combinedHandle.id, cameraTextureDescriptor);
            // cmd.GetTemporaryRT(m_OutlineDepth.id, cameraTextureDescriptor);
            cmd.GetTemporaryRT(_outlineHandle.id, cameraTextureDescriptor);
            // if (m_HasDepth)
            // {
            //     ConfigureTarget(m_CombinedRTHandle.Identifier(), m_OutlineDepth.Identifier());
            // }
            // else
            // {
                ConfigureTarget(_combinedHandle.Identifier());
            // }
            
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            textureDescriptor.depthBufferBits = 0;
            var width = textureDescriptor.width;
            var height = textureDescriptor.height;
            
            Camera camera = renderingData.cameraData.camera;

            // Edge detection compute
            // ---------------------------------------------------------------------------------------------------------------
            var laplacian = computeShader.FindKernel("MAIN_LAPLACIAN");
            cmd.SetComputeTextureParam(computeShader, laplacian, "source", _source.Identifier());
            cmd.SetComputeTextureParam(computeShader, laplacian, "result", _outlineHandle.Identifier());
            cmd.DispatchCompute(computeShader, laplacian, Mathf.CeilToInt(width / 32f), Mathf.CeilToInt(height / 32f), 1);

            // ---------------------------------------------------------------------------------------------------------------
            // Set global texture _OutlineTexture with computed edge data.
            cmd.SetGlobalTexture("_OutlineTexture", _outlineHandle.Identifier());

            // if (debugTargetView != DebugTargetView.None)
            // {
            //     var debugHandle = debugTargetView switch
            //     {
            //         DebugTargetView.ColorTarget_1 => _source,
            //         DebugTargetView.Depth => _outlineDepth,
            //         DebugTargetView.BlurResults => _source,
            //         DebugTargetView.EdgeResults => _outlineHandle,
            //         _ => new RenderTargetHandle()
            //     };
            //     cmd.Blit(debugHandle.Identifier(), renderingData.cameraData.renderer.cameraColorTarget);
            // }
            // else
            // {
                // Blit render feature camera color target to m_FinalColorTarget to be combined with outline texture in m_Material's shader
                cmd.Blit(_renderer.cameraColorTarget, _combinedHandle.Identifier(), _material);
                // Copy CombinedTexture to active camera color target
                cmd.CopyTexture(_combinedHandle.Identifier(), renderingData.cameraData.renderer.cameraColorTarget);
                // Copy outline depth to camera depth target for use in other features, like a transparent pass.
                if (_hasDepth) cmd.CopyTexture(_outlineDepth.Identifier(), renderingData.cameraData.renderer.cameraDepthTarget);
            // }
            cmd.SetGlobalTexture("_MainTex", _combinedHandle.Identifier());
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (_source.id != -1)
                cmd.ReleaseTemporaryRT(_source.id);
            if (_combinedHandle.id != -1)
                cmd.ReleaseTemporaryRT(_combinedHandle.id);
            if (_outlineDepth.id != -1)
                cmd.ReleaseTemporaryRT(_outlineDepth.id);
        }
    }
}