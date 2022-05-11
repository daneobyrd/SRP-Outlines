// This was originally inspired by a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a ma-ko illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

namespace Plugins.ScriptableRenderPass
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [Serializable]
    public class DrawToRTSettings
    {
        public bool   enabled     = false;
        public string profilerTag;
    
        public FilteringSettings FilteringSettings; // Not serializable.

        [Header("Filters")]
        public HelperEnums.RenderQueueType renderQueueType;

        public LayerMask layerMask = -1;
        public LightLayerEnum lightLayerMask = LightLayerEnum.Everything;
    
        public CustomColorTarget customColorTarget;
        public CustomDepthTarget customDepthTarget;
        public bool AllocateDepth => customDepthTarget.enabled;

        public DrawToRTSettings(string name, HelperEnums.RenderQueueType queueType)
        {
            profilerTag     = name;
            renderQueueType = queueType;

            customColorTarget = new CustomColorTarget();
            customDepthTarget = new CustomDepthTarget();
        }
    }

    [Serializable]
    public class DrawToRT : ScriptableRenderPass
    {
        private DrawToRTSettings _settings;
        private List<ShaderTagId> _shaderTagIdList = new();
    
        public DrawToRT(DrawToRTSettings passSettings)
        {
            _settings = passSettings;
        
            var renderQueueRange = _settings.renderQueueType == HelperEnums.RenderQueueType.Opaque ? RenderQueueRange.opaque : RenderQueueRange.transparent;

            _settings.FilteringSettings = new FilteringSettings(renderQueueRange, _settings.layerMask, (uint)_settings.lightLayerMask);
        
            ShaderTagSetup(_settings.customColorTarget);
        }
    
        /// <summary>
        /// Adds all non-duplicate (string) lightModeTags in colorTarget to _shaderTagIdList.
        /// </summary>
        public void ShaderTagSetup(CustomColorTarget colorTarget)
        {
            // Duplicate tags might not be an issue... but I'm checking for duplicates as a precaution.
            foreach (var colorTagId in colorTarget.lightModeTags
                                                  .Select(tempTag => new ShaderTagId(tempTag))
                                                  .Where(colorTagID => !_shaderTagIdList.Contains(colorTagID)))
            {
                _shaderTagIdList.Add(colorTagId);
            }
        }
    
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;
            cameraTextureDescriptor.msaaSamples = 1;
        
            var colorTarget = _settings.customColorTarget;
            var depthTarget = _settings.customDepthTarget;

            /*
            Alternatively you could use the constructor that uses RenderTextureDescriptor (or any other).
            I'm only using this one because I am controlling some parameters with exposed fields.
            */
            if (colorTarget.renderTextureType == BuiltinRenderTextureType.PropertyName)
            {
                cmd.GetTemporaryRT(colorTarget.GetNameID(), width, height, (int)colorTarget.depthBits, FilterMode.Point, colorTarget.renderTextureFormat);
            }
        
            if (_settings.AllocateDepth)
            {
                if (depthTarget.renderTextureType == BuiltinRenderTextureType.PropertyName)
                {
                    cmd.GetTemporaryRT(depthTarget.GetNameID(), width, height, (int)depthTarget.depthBits, FilterMode.Point, RenderTextureFormat.Depth);
                }
            
                ConfigureTarget(colorTarget.GetRTID(), depthTarget.GetRTID());
                ConfigureClear(ClearFlag.All, clearColor);
            }
            else
            {
                ConfigureTarget(colorTarget.GetRTID());
                ConfigureClear(ClearFlag.Color, clearColor);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_settings.profilerTag);

            var cameraData = renderingData.cameraData;
        
            SortingCriteria sortingCriteria = _settings.renderQueueType == HelperEnums.RenderQueueType.Opaque ? cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;

            DrawingSettings drawingSettings = CreateDrawingSettings( _shaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _settings.FilteringSettings);
        
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // or FrameCleanup? In most situations it doesn't matter.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // cmd.ReleaseTemporaryRT(colorTarget.NameID);
            cmd.ReleaseTemporaryRT(_settings.customColorTarget.GetNameID());

            if (_settings.AllocateDepth)
            {
                // cmd.ReleaseTemporaryRT(_settings.customDepthTarget.NameID);
                cmd.ReleaseTemporaryRT(_settings.customDepthTarget.GetNameID());
            }
        }
    }
}