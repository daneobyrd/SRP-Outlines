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
        // private OutlineSettings _settings;

        private string _profilerTag;
        private Material _material;
        // private DebugTargetView debugTargetView => _settings.debugTargetView;

        private bool _hasDepth;
        private ComputeShader _computeShader;
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _sourceIntId;   // _BlurResults or _OutlineOpaque (Debug)
        private RenderTargetIdentifier sourceId => new (_sourceIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _outlineIntId = Shader.PropertyToID("_OutlineTexture");
        private RenderTargetIdentifier outlineTargetId => new (_outlineIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _outlineDepthIntId = Shader.PropertyToID("_OutlineDepth");
        private RenderTargetIdentifier outlineDepthTargetId => new (_outlineDepthIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _combinedIntId;// = Shader.PropertyToID("_CombinedTexture");
        private RenderTargetIdentifier combinedTargetId => new (_combinedIntId);
        
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private RenderTargetIdentifier _tempColor;

        private static int destinationIntId => -1;
        private static RenderTargetIdentifier destinationTargetId => new (destinationIntId);
        
        public void Setup( string sourceTextureName, RenderTargetIdentifier cameraTempColor )
        {
            _sourceIntId = Shader.PropertyToID(sourceTextureName);
            _tempColor = cameraTempColor;
        }

        public FullscreenEdgeDetectionCompute(string name)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _profilerTag = name;
        }

        public void Init( Material initMaterial, ComputeShader computeShader,  bool hasDepth)
        {
            _material = initMaterial;
            _hasDepth = hasDepth;
            _computeShader = computeShader;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;
            
            cmd.GetTemporaryRT(_sourceIntId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, 1);
            
            cmd.GetTemporaryRT(_combinedIntId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, 1, true);
            
            if (_hasDepth) cmd.GetTemporaryRT(_outlineDepthIntId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
            
            cmd.GetTemporaryRT(_outlineIntId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, 1, true);
            
            // See CopyColorPass.cs (65)
            cmd.GetTemporaryRT(destinationIntId, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default, 1);

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
            cmd.SetComputeTextureParam(_computeShader, laplacian, "Source", sourceId, 0);
            cmd.SetComputeTextureParam(_computeShader, laplacian, "Result", outlineTargetId, 0);
            cmd.DispatchCompute(_computeShader, laplacian, Mathf.CeilToInt(width / 32f), Mathf.CeilToInt(height / 32f), 1);

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Set global texture _OutlineTexture with Computed edge data.
            // ---------------------------------------------------------------------------------------------------------------------------------------
            cmd.SetGlobalTexture(_outlineIntId, outlineTargetId);
            #endregion

            // const RenderBufferLoadAction cLoadAction = RenderBufferLoadAction.Load;
            // const RenderBufferStoreAction cStoreAction = RenderBufferStoreAction.StoreAndResolve;
            // const RenderBufferLoadAction dLoadAction = RenderBufferLoadAction.Load;
            // const RenderBufferStoreAction dStoreAction = RenderBufferStoreAction.StoreAndResolve;
            Camera camera = renderingData.cameraData.camera;

            #region Blit Camera Color to Combined Texture

            // ---------------------------------------------------------------------------------------------------------------------------------------
            // Set _tempColor to camera color target.
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // _tempColor = renderingData.cameraData.renderer.cameraColorTarget;
            
            // ---------------------------------------------------------------------------------------------------------------------------------------
            // Blit camera color target to _combinedHandle to be combined with outline texture in blit _material's shader.                           
            //                                                                                       Imitate CopyColorPass.cs / RenderingUtils.Blit()
            /* ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // ScriptableRenderer.SetRenderTarget(cmd, combinedTargetId, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
            CoreUtils.SetRenderTarget(cmd, combinedTargetId, cLoadAction, cStoreAction, dLoadAction, dStoreAction, clearFlag, clearColor);
            // RenderingUtils.Blit(cmd, source, combinedTargetTexture, m_CopyColorMaterial, 0, useDrawProceduralBlit);
            cmd.SetGlobalTexture("_MainTex", _tempColor);
            cmd.SetRenderTarget(combinedTargetId, cLoadAction, cStoreAction, dLoadAction, dStoreAction);
            cmd.Blit( _tempColor, BuiltinRenderTextureType.CurrentActive, _material, 0);
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────*/

            Blit(cmd, _tempColor, combinedTargetId, _material, 0);
            // cmd.SetGlobalTexture("_MainTex", combinedTargetId);
            // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
            // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            #endregion

            #region Blit Combined Texture to Active Camera
            
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Blit CombinedTexture to active camera color target                                    Imitate CopyColorPass.cs / RenderingUtils.Blit()
            // ---------------------------------------------------------------------------------------------------------------------------------------
            RenderTargetIdentifier opaqueColorRT = destinationTargetId;
            // ScriptableRenderer.SetRenderTarget(cmd, opaqueColorRT, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
            /*
            CoreUtils.SetRenderTarget(cmd, opaqueColorRT, cLoadAction, cStoreAction, dLoadAction, dStoreAction, clearFlag, clearColor);
            // RenderingUtils.Blit(cmd, source, combinedTargetTexture, m_CopyColorMaterial, 0, useDrawProceduralBlit);
            cmd.SetGlobalTexture("_MainTex", combinedTargetId);
            cmd.SetRenderTarget(opaqueColorRT, cLoadAction, cStoreAction, dLoadAction, dStoreAction);
            cmd.Blit( combinedTargetId, BuiltinRenderTextureType.CurrentActive, _material, 0);
            */
            
            Blit(cmd, combinedTargetId, opaqueColorRT);
            // cmd.SetGlobalTexture("_SourceTex", combinedTargetId);
            // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
            // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            #endregion
            
            // ↓ Currently commented out for simplicity during testing; test with color only rather than dealing with depth at the same time.
            // Copy outline depth to camera depth target for use in other features, like a transparent pass.
            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            if (_hasDepth) Blit(cmd, outlineDepthTargetId, renderingData.cameraData.renderer.cameraDepthTarget);
            
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
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