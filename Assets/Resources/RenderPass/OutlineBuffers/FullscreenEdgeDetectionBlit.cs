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
        private ComputeShader _computeShader = null;
        
        private RenderTargetHandle _source; // _BlurResults or _OutlineOpaque
        private RenderTargetHandle _outlineHandle = new (new RenderTargetIdentifier("_OutlineTexture"));
        private RenderTargetHandle _outlineDepth = new(new RenderTargetIdentifier("_OutlineDepth"));
        private RenderTargetHandle _combinedHandle;
        
        public FullscreenEdgeDetectionBlit(string name)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _profilerTag = name;
        }

        public void Init(Material initMaterial, ScriptableRenderer newRenderer, ComputeShader computeShader, string sourceTextureName, bool hasDepth)
        {
            _material = initMaterial;
            _source = new RenderTargetHandle(new RenderTargetIdentifier(sourceTextureName));
            _source.Init(sourceTextureName);
            _renderer = newRenderer;
            _hasDepth = hasDepth;
            _computeShader = computeShader;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            _outlineHandle.Init(new RenderTargetIdentifier("_OutlineTexture"));
            if (_hasDepth) _outlineDepth.Init(new RenderTargetIdentifier("_OutlineDepth"));
            cmd.GetTemporaryRT(_source.id, cameraTextureDescriptor);
            cmd.GetTemporaryRT(_combinedHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 24,
                FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default,1, enableRandomWrite:true);
            cmd.GetTemporaryRT(_outlineDepth.id, cameraTextureDescriptor);
            cmd.GetTemporaryRT(_outlineHandle.id, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 24,
                FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default,1, enableRandomWrite:true);
            if (_hasDepth)
            {
                ConfigureTarget(_combinedHandle.Identifier(), _outlineDepth.Identifier());
            }
            else
            {
                ConfigureTarget(_combinedHandle.Identifier());
            }
            
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
            var camSize = new Vector4(width, height, 0, 0);
            // Camera camera = renderingData.cameraData.camera;

            // Edge detection compute
            // ---------------------------------------------------------------------------------------------------------------
            var laplacian = _computeShader.FindKernel("MAIN_LAPLACIAN");
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize );
            cmd.SetComputeTextureParam(_computeShader, laplacian, "source", _source.Identifier());
            cmd.SetComputeTextureParam(_computeShader, laplacian, "result", _outlineHandle.Identifier());
            cmd.DispatchCompute(_computeShader, laplacian, Mathf.CeilToInt(width / 32f), Mathf.CeilToInt(height / 32f), 1);

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
                // cmd.Blit(debugHandle.Identifier(), renderingData.cameraData.renderer.cameraColorTarget);
            // }
            // else
            // {
                // Blit render feature camera color target to _combinedHandle to be combined with outline texture in _material's shader
                cmd.Blit(_renderer.cameraColorTarget, _combinedHandle.Identifier(), _material);
                // Copy CombinedTexture to active camera color target
                cmd.Blit(_combinedHandle.Identifier(), renderingData.cameraData.renderer.cameraColorTarget);
                // Copy outline depth to camera depth target for use in other features, like a transparent pass.
                if (_hasDepth) cmd.Blit(_outlineDepth.Identifier(), renderingData.cameraData.renderer.cameraDepthTarget);
            // }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
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