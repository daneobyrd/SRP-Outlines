﻿// Imitation of CopyColorPass.cs / RenderingUtils.Blit()
// ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// const RenderBufferLoadAction cLoadAction = RenderBufferLoadAction.Load;
// const RenderBufferStoreAction cStoreAction = RenderBufferStoreAction.Store;
// const RenderBufferLoadAction dLoadAction = RenderBufferLoadAction.Load;
// const RenderBufferStoreAction dStoreAction = RenderBufferStoreAction.Store;

// ScriptableRenderer.SetRenderTarget(cmd, opaqueColorRT, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
/*
CoreUtils.SetRenderTarget(cmd, opaqueColorRT, cLoadAction, cStoreAction, dLoadAction, dStoreAction, clearFlag, clearColor);
// RenderingUtils.Blit(cmd, source, combinedTargetTexture, m_CopyColorMaterial, 0, useDrawProceduralBlit);
cmd.SetGlobalTexture("_MainTex", combinedTargetId);
cmd.SetRenderTarget(opaqueColorRT, cLoadAction, cStoreAction, dLoadAction, dStoreAction);
cmd.Blit( combinedTargetId, BuiltinRenderTextureType.CurrentActive, _material, 0);
*/

// A whole bunch of ways to get the camera texture for use as a base texture to "paste" my outline texture on top.
// ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // When either input int, RenderTargetIdentifier or RenderTargetHandle.id = -1 returns BuiltinRenderTextureType.CameraTarget;
    // -----------------------------------------------------------------------------------------------------------------------------------
        // cmd.SetGlobalTexture(_combinedIntId, -1);
        // Blit(cmd, -1, _combinedIntId, _material, blitIndex);
        // Blit(cmd, BuiltinRenderTextureType.CameraTarget, _combinedIntId, _material, blitIndex);
        //
        // RenderTargetHandle _exampleDestinationHandle = RenderTargetHandle.CameraTarget;
        // Blit(cmd, _exampleDestinationHandle.Identifier(), _combinedIntId, _material, blitIndex);
        // Blit(cmd, _exampleDestinationHandle.id, _combinedIntId, _material, blitIndex);
        //
        // Blit(cmd, renderingData.cameraData.targetTexture, _combinedIntId, _material, blitIndex);
        // Blit(cmd, renderingData.cameraData.camera.activeTexture, _combinedIntId, _material, blitIndex);
        // Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget, _combinedIntId, _material, blitIndex);
    // -----------------------------------------------------------------------------------------------------------------------------------
    // Pass in the RenderFeature's ScriptableRenderer in Init(): _renderer => newRenderer;
    // -----------------------------------------------------------------------------------------------------------------------------------
        // Blit(cmd, _renderer.cameraColorTarget, _combinedIntId, _material, blitIndex);
        // _renderer.ConfigureCameraTarget(colorTarget: , depthTarget: ); ???
    // -----------------------------------------------------------------------------------------------------------------------------------
    // Manually recreate actions of blit function.
    // This ↓ would blit the data to the screen (the data being whatever combinedTargetHandle.Identifier() points to).
    // -----------------------------------------------------------------------------------------------------------------------------------
        // Camera camera = renderingData.cameraData.camera;
        // cmd.SetGlobalTexture("_MainTex", combinedTargetHandle.Identifier());
        // cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
        // cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _material);
        // cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
// ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

/*[System.Serializable]
public class OutlineShaderProperties
{
    [Tooltip("Object Threshold.")] public float outerThreshold = 1.0f;
    [Tooltip("Inner Threshold.")] public float innerThreshold = 1.0f;
    [Tooltip("Rotations.")] public int rotations = 8;
    [Tooltip("Depth Push.")] public float depthPush = 1e-6f;
    [Tooltip("Object LUT.")] public Texture2D outerLUT;
    [Tooltip("Inner LUT.")] public Texture2D innerLUT;
}*/


// private static readonly int OuterThreshold = Shader.PropertyToID("_OuterThreshold");
// private static readonly int InnerThreshold = Shader.PropertyToID("_InnerThreshold");
// private static readonly int Rotations = Shader.PropertyToID("_Rotations");
// private static readonly int DepthPush = Shader.PropertyToID("_DepthPush");
// private static readonly int OuterLut = Shader.PropertyToID("_OuterLUT");
// private static readonly int InnerLut = Shader.PropertyToID("_InnerLUT");