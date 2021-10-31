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
        private int blitIndex => _settings.edgeSettings.blitIndex;
        private DebugTargetView debugTargetView => _settings.debugTargetView;

        private ScriptableRenderer _renderer;
        private bool _hasDepth;
        private ComputeShader _computeShader;
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _sourceIntId;   // _BlurResults or _OutlineOpaque (Debug)
        private RenderTargetIdentifier _sourceId;
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private readonly int _outlineIntId = Shader.PropertyToID("_OutlineTexture");
        private RenderTargetIdentifier outlineTargetId => new (_outlineIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private readonly int _outlineDepthIntId = Shader.PropertyToID("_OutlineDepth");
        private RenderTargetIdentifier outlineDepthTargetId => new (_outlineDepthIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private readonly int _combinedIntId = Shader.PropertyToID("_MainTex");
        private RenderTargetIdentifier combinedTargetId => new (_combinedIntId);
        private RenderTargetHandle combinedTargetHandle => new (new RenderTargetIdentifier(_combinedIntId));
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _destinationIntId = -1;
        private RenderTargetIdentifier destinationTargetId => new (_destinationIntId);
        
        
        public FullscreenEdgeDetectionBlit(string name)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _profilerTag = name;
        }

        public void Init(Material initMaterial, ScriptableRenderer newRenderer, ComputeShader computeShader, string sourceTextureName, bool hasDepth)
        {
            _material = initMaterial;
            _sourceIntId = Shader.PropertyToID(sourceTextureName);
            _renderer = newRenderer;
            _hasDepth = hasDepth;
            _computeShader = computeShader;
            combinedTargetHandle.Init(combinedTargetId);
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;
            
            cmd.GetTemporaryRT(_sourceIntId, width, height, 24, FilterMode.Point, RenderTextureFormat.ARGBFloat);
            
            cmd.GetTemporaryRT(_combinedIntId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat,
                                RenderTextureReadWrite.Default, 1, true);
            if (_hasDepth) cmd.GetTemporaryRT(_outlineDepthIntId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.GetTemporaryRT(_outlineIntId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat,
                                RenderTextureReadWrite.Default, 1, true);

            cmd.GetTemporaryRT(_destinationIntId, cameraTextureDescriptor);

            ConfigureTarget(_destinationIntId);
            // if (_hasDepth)
            // {
            //     ConfigureTarget(combinedTargetId, depthAttachment: outlineDepthTargetId);
            // }
            // else
            // {
            // ConfigureTarget(combinedTargetId);
            // }
            // _renderer.ConfigureCameraTarget(combinedTargetId, outlineTargetId);

            // ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;
            CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);
            RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            textureDescriptor.depthBufferBits = 0;
            textureDescriptor.enableRandomWrite = true;
            var width = textureDescriptor.width;
            var height = textureDescriptor.height;
            var camSize = new Vector4(width, height, 0, 0);

            // Camera camera = renderingData.cameraData.camera;
            
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Edge detection compute
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            var laplacian = _computeShader.FindKernel("KLaplacian");
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
            cmd.SetComputeTextureParam(_computeShader, laplacian, "source", _sourceIntId, 0);
            cmd.SetComputeTextureParam(_computeShader, laplacian, "result", _outlineIntId, 0);
            cmd.DispatchCompute(_computeShader, laplacian, Mathf.CeilToInt(width / 32f), Mathf.CeilToInt(height / 32f), 1);

            // Set global texture _OutlineTexture with Computed edge data.
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            cmd.SetGlobalTexture(_outlineIntId, outlineTargetId);
            
            // Blit render feature camera color target to _combinedHandle to be combined with outline texture in blit _material's shader
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // cmd.SetGlobalTexture(_combinedIntId, -1);
            // Blit(cmd, destinationTargetId, _combinedIntId, _material, blitIndex);
            Blit(cmd, destinationTargetId, _combinedIntId, _material, 0);
            
            // Copy CombinedTexture to active camera color target
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            Blit(cmd, _combinedIntId, destinationTargetId);
            
            // cmd.SetGlobalTexture("_MainTex", combinedTargetHandle.Identifier());
            // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
            // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            // Copy outline depth to camera depth target for use in other features, like a transparent pass.
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // if (_hasDepth) cmd.Blit(outlineDepthTargetId, renderingData.cameraData.renderer.cameraDepthTarget);
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (_sourceIntId != -1) cmd.ReleaseTemporaryRT(_sourceIntId);
            if (_outlineIntId != -1) cmd.ReleaseTemporaryRT(_outlineIntId);
            if (_outlineDepthIntId != -1) cmd.ReleaseTemporaryRT(_outlineDepthIntId);
            if (_combinedIntId != -1) cmd.ReleaseTemporaryRT(_combinedIntId);
        }
    }
}