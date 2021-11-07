// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resources.RenderPass.OutlineBuffers
{
    public class FullscreenEdgeDetectionCompute : ScriptableRenderPass
    {
        private OutlineSettings _settings;

        private string _profilerTag;
        private Material _material;
        // private DebugTargetView debugTargetView => _settings.debugTargetView;

        private bool _hasDepth;
        private ComputeShader _computeShader;
        private int _index;
        private int[] _kernelType = { 330, 331, 332, 333, 550, 551, 770 };
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _sourceIntId;   // _BlurResults or _OutlineOpaque (Debug)
        private RenderTargetIdentifier sourceId => new (_sourceIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int outlineIntId => Shader.PropertyToID("_OutlineTexture");
        private RenderTargetIdentifier outlineTargetId => new (outlineIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _outlineDepthIntId = Shader.PropertyToID("_OutlineDepth");
        private RenderTargetIdentifier outlineDepthTargetId => new (_outlineDepthIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _combinedIntId;// = Shader.PropertyToID("_CombinedTexture");
        private RenderTargetIdentifier combinedTargetId => new (_combinedIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private RenderTargetIdentifier _tempColor;

        // private static int destinationIntId => -1;
        private static RenderTargetIdentifier _destinationTargetId;// => new (destinationIntId);
        
        public FullscreenEdgeDetectionCompute(string name, int kernelIndex)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _profilerTag = name;
            _index = kernelIndex;
        }

        public void Init(OutlineSettings settings, Material initMaterial, string sourceTextureName, RenderTargetIdentifier cameraTempColor, ComputeShader computeShader,  bool hasDepth)
        {
            _settings = settings;
            _material = initMaterial;
            _sourceIntId = Shader.PropertyToID(sourceTextureName);
            _destinationTargetId = cameraTempColor;
            _hasDepth = hasDepth;
            _computeShader = computeShader;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;
            var msaa = cameraTextureDescriptor.msaaSamples;
            int depthBuffer = cameraTextureDescriptor.depthBufferBits = 0;
            
            cmd.GetTemporaryRT(_sourceIntId, width, height, depthBuffer, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, msaa);
            
            cmd.GetTemporaryRT(_combinedIntId, width, height, depthBuffer, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, msaa, true);
            
            if (_hasDepth) cmd.GetTemporaryRT(_outlineDepthIntId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
            
            cmd.GetTemporaryRT(outlineIntId, width, height, depthBuffer, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, msaa, true);
            
            // See CopyColorPass.cs (65)
            // cmd.GetTemporaryRT(destinationIntId, width, height, depthBuffer, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, msaa);

            // if (_hasDepth)
            // {
            //     ConfigureTarget(combinedTargetId, depthAttachment: outlineDepthTargetId);
            // }
            // else
            // {
            // ConfigureTarget(combinedTargetId);
            // }

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
            
            #region Compute Edges
            // NOTE: If you use RenderTargetHandle for cmd.SetComputeTextureParam() it may not work as RenderTargetHandle.id may be outside of the range kShaderTexEnvCount.
            // Sometimes RenderTargetHandle.id or RenderTargetHandle.Identifier() may return the same value regardless of RenderTargetHandle.Init()'s input.
            // https://forum.unity.com/threads/access-a-temporary-rendertexture-allocated-from-previous-frame.1018573/
            
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Edge detection compute
            // ---------------------------------------------------------------------------------------------------------------------------------------
            var laplacian = _computeShader.FindKernel("KLaplacian");
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
            cmd.SetComputeIntParam(_computeShader, "_KernelType", _kernelType[_index]);
            cmd.SetComputeTextureParam(_computeShader, laplacian, "Source", sourceId, 0);
            cmd.SetComputeTextureParam(_computeShader, laplacian, "Result", outlineTargetId, 0);
            cmd.DispatchCompute(_computeShader, laplacian, Mathf.CeilToInt(width / 32f), Mathf.CeilToInt(height / 32f), 1);
            
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Set global texture _OutlineTexture with Computed edge data.
            // ---------------------------------------------------------------------------------------------------------------------------------------
            cmd.SetGlobalTexture(outlineIntId, outlineTargetId);

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Composite _OutlineTexture and Camera color target in compute shader
            // ---------------------------------------------------------------------------------------------------------------------------------------
            var composite = _computeShader.FindKernel("KComposite");
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
            cmd.SetComputeTextureParam(_computeShader, composite, "Source", outlineTargetId, 0);
            cmd.SetComputeTextureParam(_computeShader, composite, "CameraTex", _destinationTargetId, 0);
            cmd.SetComputeTextureParam(_computeShader, composite, "Final", combinedTargetId, 0);
            cmd.DispatchCompute(_computeShader, composite, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
            
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Blit _OutlineTexture to the screen
            // ---------------------------------------------------------------------------------------------------------------------------------------
            RenderTargetIdentifier opaqueColorRT = _destinationTargetId;
            
            Blit(cmd, combinedTargetId, opaqueColorRT);

            #endregion
            
            // ↓ Currently commented out for simplicity during testing; test with color only rather than dealing with depth at the same time.
            // Copy outline depth to camera depth target for use in other features, like a transparent pass.
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // if (_hasDepth) Blit(cmd, outlineDepthTargetId, renderingData.cameraData.renderer.cameraDepthTarget);
            
            context.ExecuteCommandBuffer(cmd);
            // cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (_sourceIntId != -1) cmd.ReleaseTemporaryRT(_sourceIntId);
            if (outlineIntId != -1) cmd.ReleaseTemporaryRT(outlineIntId);
            if (_outlineDepthIntId != -1) cmd.ReleaseTemporaryRT(_outlineDepthIntId);
            if (_combinedIntId != -1) cmd.ReleaseTemporaryRT(_combinedIntId);
        }
    }
}