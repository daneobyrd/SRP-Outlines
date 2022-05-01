using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
public enum EdgeDetectionMethod
{
    Laplacian = 0,
    Sobel = 1,
    FreiChen = 2,
    AltFreiChen = 3
}

/// <inheritdoc />
public class FullscreenEdgeDetection : ScriptableRenderPass
{
    private string _profilerTag;
    private Material _material;
    private int _passIndex;

    private ComputeShader _computeShader;
    private EdgeDetectionMethod _method;

    private int _sourceId;                        // _BlurredUpsampleResults or _OutlineOpaqueColor
    private RenderTargetIdentifier _sourceTarget;

    private static int outlineId => Shader.PropertyToID("_OutlineTexture");
    private static RenderTargetIdentifier outlineTarget => new(outlineId);
    
    public FullscreenEdgeDetection(RenderPassEvent evt, string name)
    {
        // base.profilingSampler = new ProfilingSampler(name);
        _profilerTag = name;
        renderPassEvent = evt;
    }

    public void Setup( string sourceTexture, ComputeShader computeShader, EdgeDetectionMethod method)
    {
        _sourceId          = Shader.PropertyToID(sourceTexture);
        _sourceTarget      = new RenderTargetIdentifier(_sourceId);
        _computeShader     = computeShader;
        _method            = method;
    }
    
    public void Setup( int sourceID, ComputeShader computeShader, EdgeDetectionMethod method)
    {
        _sourceId          = sourceID;
        _sourceTarget      = new RenderTargetIdentifier(_sourceId);
        _computeShader     = computeShader;
        _method            = method;
    }

    public void InitMaterial(Material initMaterial, int passIndex)
    {
        _material  = initMaterial;
        _passIndex = passIndex;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        RenderTextureDescriptor camTexDesc = cameraTextureDescriptor;
        var width = camTexDesc.width;
        var height = camTexDesc.height;
        camTexDesc.msaaSamples       = 1;
        camTexDesc.depthBufferBits   = 0;
        camTexDesc.enableRandomWrite = true;
        // camTexDesc.sRGB              = _sourceColorSpace == RenderTextureReadWrite.sRGB;

        cmd.GetTemporaryRT(_sourceId, camTexDesc, FilterMode.Point);

        cmd.GetTemporaryRT(outlineId, camTexDesc, FilterMode.Point);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        ref CameraData cameraData = ref renderingData.cameraData;
        // if (cameraData.camera.cameraType != CameraType.Game)
        //     return;
        if (_material == null)
            return;

        // CommandBuffer cmd = CommandBufferPool.Get();
        CommandBuffer cmd = CommandBufferPool.Get(_profilerTag);

        // using (new ProfilingScope(cmd, base.profilingSampler))
        // {
            RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            var width = cameraTargetDescriptor.width;
            var height = cameraTargetDescriptor.height;
            cameraTargetDescriptor.depthBufferBits   = 0;
            cameraTargetDescriptor.enableRandomWrite = true;
            var camSize = new Vector4(width, height, 0, 0);
            
            
            #region Compute Edges

            string methodString = null;
            Vector2Int numthreads = default;
            
            switch (_method)
            {
                case EdgeDetectionMethod.Laplacian:
                {
                    methodString = "KLaplacian";
                    numthreads.x = Mathf.CeilToInt(width / 32f);
                    numthreads.y = Mathf.CeilToInt(height / 32f);
                    break;
                }
                case EdgeDetectionMethod.FreiChen:
                {
                    methodString = "KFreiChen";
                    numthreads.x = Mathf.CeilToInt(width / 8f);
                    numthreads.y = Mathf.CeilToInt(height / 8f);
                    break;
                }
                case EdgeDetectionMethod.Sobel:
                    methodString = "KSobel";
                    break;
                case EdgeDetectionMethod.AltFreiChen:
                    methodString = "KAltFreiChen";
                    numthreads.x = Mathf.CeilToInt(width / 8f);
                    numthreads.y = Mathf.CeilToInt(height / 8f);
                    break;
                // default:
                //     throw new ArgumentOutOfRangeException();
            }
            
            var edgeKernel = _computeShader.FindKernel(methodString);
            // var colorspace = _sourceColorSpace == RenderTextureReadWrite.sRGB ? 2 : 1;
            
            cmd.SetComputeVectorParam(_computeShader, "_Size", camSize);
            // cmd.SetComputeIntParam(_computeShader, "_TextureColorMode", colorspace );
            cmd.SetComputeTextureParam(_computeShader, edgeKernel, "Source", _sourceTarget, 0);
            cmd.SetComputeTextureParam(_computeShader, edgeKernel, "Result", outlineTarget, 0);
            cmd.DispatchCompute(_computeShader, edgeKernel, numthreads.x, numthreads.y, 1);


            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Set global texture _OutlineTexture with Computed edge data.
            // ---------------------------------------------------------------------------------------------------------------------------------------
            cmd.SetGlobalTexture(outlineId, outlineTarget, RenderTextureSubElement.Color);

            #endregion

            // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
            // Draw _OutlineTexture to the screen
            // ---------------------------------------------------------------------------------------------------------------------------------------
            cmd.SetRenderTarget(outlineTarget);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // The RenderingUtils.fullscreenMesh argument specifies that the mesh to draw is a quad.
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
        // }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (_sourceId != -1) cmd.ReleaseTemporaryRT(_sourceId);
        if (outlineId != -1) cmd.ReleaseTemporaryRT(outlineId);
    }
}