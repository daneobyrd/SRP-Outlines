using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum EdgeDetectionMethod
{
    Laplacian = 0,
    Sobel = 1,
    FreiChen = 2
}

public class FullscreenEdgeDetection : ScriptableRenderPass
{
    private Material _material;

    private ComputeShader _computeShader;
    private EdgeDetectionMethod _method;

    private int _sourceId;                        // _BlurredUpsampleResults or _OutlineOpaque (Debug)
    private RenderTargetIdentifier _sourceTarget; // => new(_sourceIntId);

    private static int                    outlineId     => Shader.PropertyToID("_OutlineTexture");
    private static RenderTargetIdentifier outlineTarget => new(outlineId);

    private static int blitId => Shader.PropertyToID("_SourceTexture");
    private static RenderTargetIdentifier _blitTempTarget;

    private RenderTargetIdentifier _cameraTarget; // = BuiltinRenderTextureType.CameraTarget;

    public FullscreenEdgeDetection(RenderPassEvent evt, string name)
    {
        base.profilingSampler = new ProfilingSampler(name);
        renderPassEvent       = evt;
    }

    public void Setup(Material initMaterial, string sourceTexture, RenderTargetIdentifier cameraColor,
                      ComputeShader computeShader, EdgeDetectionMethod method)
    {
        _material      = initMaterial;
        _sourceId      = Shader.PropertyToID(sourceTexture);
        _sourceTarget  = new RenderTargetIdentifier(_sourceId);
        _cameraTarget  = new RenderTargetIdentifier(cameraColor, 0, CubemapFace.Unknown, -1);
        _computeShader = computeShader;
        _method        = method;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
        var width = camTexDesc.width;
        var height = camTexDesc.height;
        camTexDesc.msaaSamples       = 1;
        camTexDesc.depthBufferBits   = 0;
        camTexDesc.enableRandomWrite = true;

        cmd.GetTemporaryRT(_sourceId, camTexDesc, FilterMode.Point);

        cmd.GetTemporaryRT(outlineId, camTexDesc, FilterMode.Point);

        cmd.GetTemporaryRT(blitId, camTexDesc);
        cmd.GetTemporaryRT(-1, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        ref CameraData cameraData = ref renderingData.cameraData;

        var camera = cameraData.camera;
        // if (camera.cameraType != CameraType.Game)
        //     return;
        if (_material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, base.profilingSampler))
        {
            RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            var width = cameraTargetDescriptor.width;
            var height = cameraTargetDescriptor.height;
            cameraTargetDescriptor.depthBufferBits   = 0;
            cameraTargetDescriptor.enableRandomWrite = true;
            var camSize = new Vector4(width, height, 0, 0);

            #region Compute Edges

            var kLaplacian = _computeShader.FindKernel("KLaplacian");
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
            cmd.SetComputeTextureParam(_computeShader, kLaplacian, "Source", _sourceTarget, 0);
            cmd.SetComputeTextureParam(_computeShader, kLaplacian, "Result", outlineTarget, 0);
            cmd.DispatchCompute(_computeShader, kLaplacian, Mathf.CeilToInt(width / 32f),
                                Mathf.CeilToInt(height / 32f), 1);
            /*
            switch (_method)
            {
                case EdgeDetectionMethod.Laplacian:
                {
                    var kLaplacian = _computeShader.FindKernel("KLaplacian");
                    cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
                    cmd.SetComputeTextureParam(_computeShader, kLaplacian, "Source", _sourceTarget, 0);
                    cmd.SetComputeTextureParam(_computeShader, kLaplacian, "Result", outlineTarget, 0);
                    cmd.DispatchCompute(_computeShader, kLaplacian, Mathf.CeilToInt(width / 32f),
                                        Mathf.CeilToInt(height / 32f), 1);
                    break;
                }
                case EdgeDetectionMethod.FreiChen:
                {
                    var kFreiChen = _computeShader.FindKernel("KFreiChen");
                    cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
                    cmd.SetComputeTextureParam(_computeShader, kFreiChen, "Source", "_CameraColorAttachmentA", 0);
                    cmd.SetComputeTextureParam(_computeShader, kFreiChen, "Result", outlineTarget, 0);
                    cmd.DispatchCompute(_computeShader, kFreiChen, Mathf.CeilToInt(width / 8f),
                                        Mathf.CeilToInt(height / 8f), 1);
                    break;
                }
                case EdgeDetectionMethod.Sobel:
                    break;
            }
            */

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Set global texture _OutlineTexture with Computed edge data.
            // ---------------------------------------------------------------------------------------------------------------------------------------
            cmd.SetGlobalTexture(outlineId, outlineTarget, RenderTextureSubElement.Color);

            #endregion

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Blit _OutlineTexture to the screen
            // ---------------------------------------------------------------------------------------------------------------------------------------


            cmd.SetGlobalTexture(blitId, _cameraTarget);
            // cmd.SetRenderTarget(_cameraTarget);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // The RenderingUtils.fullscreenMesh argument specifies that the mesh to draw is a quad.
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        if (_sourceId != -1) cmd.ReleaseTemporaryRT(_sourceId);
        if (outlineId != -1) cmd.ReleaseTemporaryRT(outlineId);
        if (blitId != -1) cmd.ReleaseTemporaryRT(blitId);
    }
}