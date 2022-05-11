// This was originally inspired by a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Plugins.ScriptableRenderPass
{
    [Serializable]
    public class DrawRenderRequestSettings
    {
        public bool   enabled     = false;
        public string profilerTag;
        
        [Header("Filters")]
        public HelperEnums.RenderQueueType renderQueueType;

        public LayerMask layerMask = -1;
        public LightLayerEnum lightLayerMask = LightLayerEnum.Everything;

        [HideInInspector]
        public HelperEnums.CameraTargetMode cameraTargetMode;

        [SerializeField]
        public List<Camera.RenderRequest> renderRequests;

        public DrawRenderRequestSettings(string name, HelperEnums.RenderQueueType queueType)
        {
            profilerTag     = name;
            renderQueueType = queueType;
            renderRequests  = new List<Camera.RenderRequest>();
        }
    }

    [Serializable]
    public class DrawRenderRequest : UnityEngine.Rendering.Universal.ScriptableRenderPass
    {
        private DrawRenderRequestSettings _settings;
        private List<ShaderTagId> _ShaderTagIdList = new();
    
        public DrawRenderRequest(DrawRenderRequestSettings passSettings)
        {
            _settings = passSettings;
        
            var renderQueueRange = _settings.renderQueueType == HelperEnums.RenderQueueType.Opaque
                ? RenderQueueRange.opaque
                : RenderQueueRange.transparent;

        }
    
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            base.OnCameraSetup(cmd, ref renderingData);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;
            cameraTextureDescriptor.msaaSamples = 1;
        
            #region Create Temporary Render Textures

            // List<RenderTargetIdentifier> enabledColorAttachments = new();
        
            #endregion
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_settings.profilerTag);

            var cameraData = renderingData.cameraData;
            cameraData.camera.SubmitRenderRequests(_settings.renderRequests);
        
            SortingCriteria sortingCriteria =
                _settings.renderQueueType == HelperEnums.RenderQueueType.Opaque ? cameraData.defaultOpaqueSortFlags
                    : SortingCriteria.CommonTransparent;

            DrawingSettings drawingSettings = CreateDrawingSettings( _ShaderTagIdList, ref renderingData, sortingCriteria);

            // context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _settings.FilteringSettings);
        
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}