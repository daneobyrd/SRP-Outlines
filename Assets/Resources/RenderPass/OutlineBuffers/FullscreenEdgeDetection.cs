// This is a rewritten version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderPass.OutlineBuffers
{
    public class FullscreenEdgeDetection : ScriptableRenderPass
    {
        private Material _material;

        private bool _hasDepth;
        private ComputeShader _computeShader;

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int _sourceId; // _BlurResults or _OutlineOpaque (Debug)
        private RenderTargetIdentifier _sourceTarget;// => new(_sourceIntId);

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int outlineId => Shader.PropertyToID("_OutlineTexture");
        private RenderTargetIdentifier outlineTarget => new(outlineId);

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        private int outlineDepthId => Shader.PropertyToID("_OutlineDepth");
        private RenderTargetIdentifier outlineDepthTarget => new(outlineDepthId);

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        // private int combinedId => Shader.PropertyToID("_CombinedTexture");
        // private RenderTargetIdentifier combinedTarget => new(combinedId, 0, CubemapFace.Unknown, -1);

        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        // private static int destinationIntId => -1;
        private static RenderTargetIdentifier _cameraTarget;

        public FullscreenEdgeDetection(RenderPassEvent evt, string name)
        {
            base.profilingSampler = new ProfilingSampler(name);
            renderPassEvent = evt;
        }

        public void Setup(Material initMaterial, string sourceTexture, RenderTargetIdentifier cameraColor, ComputeShader computeShader, bool hasDepth)
        {
            _material = initMaterial;
            _sourceId = Shader.PropertyToID(sourceTexture);
            _sourceTarget = new RenderTargetIdentifier(_sourceId);
            _cameraTarget = new RenderTargetIdentifier(cameraColor, 0, CubemapFace.Unknown, -1);
            _hasDepth = hasDepth;
            _computeShader = computeShader;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
            var width = camTexDesc.width;
            var height = camTexDesc.height;
            camTexDesc.msaaSamples = 1;
            camTexDesc.depthBufferBits = 0;
            camTexDesc.enableRandomWrite = true;

            cmd.GetTemporaryRT(_sourceId, camTexDesc, FilterMode.Point);

            // cmd.GetTemporaryRT(combinedIntId, camTexDesc, FilterMode.Point);

            if (_hasDepth)
            {
                cmd.GetTemporaryRT(outlineDepthId, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth);
            }

            cmd.GetTemporaryRT(outlineId, camTexDesc, FilterMode.Point);

            // if (_hasDepth)
            // {
                // ConfigureTarget(combinedTargetId, outlineDepthTargetId);
            // }
            // else
            // {
                // ConfigureTarget(combinedTargetId);
            // }

            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            ref CameraData cameraData = ref renderingData.cameraData;

            var camera = cameraData.camera;
            // if (camera.cameraType != CameraType.Game)
            //     return;
            if (_material == null)
                return;
            
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, base.profilingSampler))
            {
                RenderTextureDescriptor textureDescriptor = cameraData.cameraTargetDescriptor;
                var width = textureDescriptor.width;
                var height = textureDescriptor.height;
                textureDescriptor.depthBufferBits = 0;
                textureDescriptor.enableRandomWrite = true;
                var camSize = new Vector4(width, height, 0, 0);

                #region Compute Edges

                // NOTE: True as of 2021.2.3f1
                // If you use init RenderTargetHandle with a RenderTargetIdentifier instead of a string, the id is -2 and rtid is the value of the RenderTargetIdentifier.
                // A RenderTargetHandle.id of -2 may be outside of the range of kShaderTexEnvCount (pretty sure this is some graphics backend texture parameter).
                // According to this forum post ↓ an id of -2 cannot be used for CommandBuffer.SetComputeTextureParam().
                // https://forum.unity.com/threads/access-a-temporary-rendertexture-allocated-from-previous-frame.1018573/

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region Edge detection compute

                // ---------------------------------------------------------------------------------------------------------------------------------------

                var laplacian = _computeShader.FindKernel("KLaplacian");
                cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
                cmd.SetComputeTextureParam(_computeShader, laplacian, "Source", _sourceTarget, 0);
                cmd.SetComputeTextureParam(_computeShader, laplacian, "Result", outlineTarget, 0);
                cmd.DispatchCompute(_computeShader, laplacian, Mathf.CeilToInt(width / 32f), Mathf.CeilToInt(height / 32f), 1);

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                // Set global texture _OutlineTexture with Computed edge data.
                // ---------------------------------------------------------------------------------------------------------------------------------------
                cmd.SetGlobalTexture(outlineId, outlineTarget, RenderTextureSubElement.Color);

                #endregion

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

                #region Composite _OutlineTexture and Camera color target in compute shader

                // ---------------------------------------------------------------------------------------------------------------------------------------

                /*
                var composite = _computeShader.FindKernel("KComposite");
                cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
                cmd.SetComputeTextureParam(_computeShader, composite, "Source", outlineTargetId);
                cmd.SetComputeTextureParam(_computeShader, composite, "CameraTex", _cameraTexCopy);
                cmd.SetComputeTextureParam(_computeShader, composite, "Result", combinedTargetId);
                cmd.DispatchCompute(_computeShader, composite, Mathf.CeilToInt(width / 8f), Mathf.CeilToInt(height / 8f), 1);
                */

                #endregion

                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                // Blit _OutlineTexture to the screen
                // ---------------------------------------------------------------------------------------------------------------------------------------
                // RenderTargetIdentifier opaqueColorRT = _destinationTargetId;

                // Blit(cmd, combinedTargetId, -1, _material);

                #endregion

                // ↓ Currently commented out for simplicity during testing; test with color only rather than dealing with depth at the same time.
                // Copy outline depth to camera depth target for use in other features, like a transparent pass.
                // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
                // if (_hasDepth) Blit(cmd, outlineDepthTargetId, renderingData.cameraData.renderer.cameraDepthTarget);
                
                Blit(cmd, ref renderingData, _material);
                
                // cmd.SetRenderTarget(_cameraTarget);
                // cmd.SetViewport(camera.rect);
                // The RenderingUtils.fullscreenMesh argument specifies that the mesh to draw is a quad.
                // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
                // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (_sourceId != -1) cmd.ReleaseTemporaryRT(_sourceId);
            if (outlineId != -1) cmd.ReleaseTemporaryRT(outlineId);
            if (outlineDepthId != -1) cmd.ReleaseTemporaryRT(outlineDepthId);
            // if (combinedIntId != -1) cmd.ReleaseTemporaryRT(combinedIntId);
        }
    }
}