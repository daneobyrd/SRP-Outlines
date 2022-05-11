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
    using static HelperEnums;

    [Serializable]
    public class DrawToMRTSettings
    {
        public bool enabled = false;
        public string profilerTag;

        public FilteringSettings FilteringSettings; // Not serializable.

        [Header("Filters")] public RenderQueueType renderQueueType;

        public LayerMask layerMask = -1;
        public LightLayerEnum lightLayerMask = LightLayerEnum.Everything;

        [HideInInspector] public CameraTargetMode cameraTargetMode;

        public CustomColorTarget[] customColorTargets;
        public CustomDepthTarget customDepthTarget;

        public DrawToMRTSettings(string name, RenderQueueType queueType)
        {
            profilerTag     = name;
            renderQueueType = queueType;

            customColorTargets = Array.Empty<CustomColorTarget>();
            customDepthTarget  = new CustomDepthTarget();
        }
    }

    [Serializable]
    public class DrawToMRT : ScriptableRenderPass
    {
        private DrawToMRTSettings _settings;
        private List<ShaderTagId> _shaderTagIdList = new();

        public DrawToMRT(DrawToMRTSettings passSettings)
        {
            _settings = passSettings;

            var renderQueueRange = _settings.renderQueueType == RenderQueueType.Opaque
                ? RenderQueueRange.opaque
                : RenderQueueRange.transparent;

            _settings.FilteringSettings =
                new FilteringSettings(renderQueueRange, _settings.layerMask, (uint) _settings.lightLayerMask);
        }

        /// <summary>
        /// Adds all non-duplicate (string) lightModeTags in colorTarget to _shaderTagIdList.
        /// </summary>
        public void ShaderTagSetup(List<CustomColorTarget> colorTargets)
        {
            foreach (var colorTagId in colorTargets.SelectMany(colorTarget => colorTarget.lightModeTags
                                                                   .Select(tempTag => new ShaderTagId(tempTag))
                                                                   .Where(colorTagID => !_shaderTagIdList.Contains(colorTagID))))
            {
                _shaderTagIdList.Add(colorTagId);
            }
            // Duplicate tags might not be an issue... but I'm checking for duplicates as a precaution.
        }

        /// <summary>
        /// Sets enum representing which custom targets are enabled.
        /// </summary>
        /// <param name="passSettings"></param>
        private void CheckCameraTargetMode(DrawToMRTSettings passSettings)
        {
            switch (passSettings.customDepthTarget.enabled)
            {
                // Currently no setting for DepthOnly pass.
                case true:
                    passSettings.cameraTargetMode = CameraTargetMode.ColorAndDepth;
                    break;
                case false:
                {
                    if (passSettings.customColorTargets.Any(target => target.enabled = true))
                    {
                        passSettings.cameraTargetMode = CameraTargetMode.Color;
                    }
                    break;
                }
            }
        }

        public void Setup()
        {
            CheckCameraTargetMode(_settings);
            ShaderTagSetup(new List<CustomColorTarget>(_settings.customColorTargets));
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var width = cameraTextureDescriptor.width;
            var height = cameraTextureDescriptor.height;
            cameraTextureDescriptor.msaaSamples = 1;

            #region Create Temporary Render Textures
            
            List<RenderTargetIdentifier> enabledColorAttachments = new();

            // Configure all enabled color attachments.
            foreach (var colorTarget in _settings.customColorTargets.Where(target => target.enabled = true))
            {
            
            /*
            Alternatively you could use the constructor that uses RenderTextureDescriptor.
            I'm only using this one because I am controlling some parameters with exposed fields.
            */
                cmd.GetTemporaryRT(colorTarget.GetNameID(), width, height, (int) colorTarget.depthBits, FilterMode.Point,
                                   colorTarget.renderTextureFormat);
                // enabledColorAttachments.Add(colorTarget.RTID);
                enabledColorAttachments.Add(colorTarget.GetRTID());
            }

            if (_settings.customDepthTarget.enabled)
            {
                var depthTarget = _settings.customDepthTarget;

                cmd.GetTemporaryRT(depthTarget.GetNameID(), width, height, (int) depthTarget.depthBits, FilterMode.Point,
                                   RenderTextureFormat.Depth);
            }

            #endregion

            // Checking for edge-case where all targets are disabled in the editor. There's probably a simpler way to check for this earlier.
            if (enabledColorAttachments.Count == 0 && !_settings.customDepthTarget.enabled) return;

            #region ConfigureTarget

            var mrt = enabledColorAttachments.ToArray();

            if (_settings.cameraTargetMode == CameraTargetMode.ColorAndDepth)
            {
                var depthTarget = _settings.customDepthTarget;
                // ConfigureTarget(mrt, _settings.customDepthTarget.RTID);
                ConfigureTarget(mrt, depthTarget.GetRTID());
                ConfigureClear(ClearFlag.All, clearColor);
            }
            else //if (_settings.cameraTargetMode == CameraTargetMode.Color)
            {
                ConfigureTarget(mrt);
                ConfigureClear(ClearFlag.Color, clearColor);
            }

            #endregion
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(_settings.profilerTag);

            var cameraData = renderingData.cameraData;

            SortingCriteria sortingCriteria =
                _settings.renderQueueType == RenderQueueType.Opaque
                    ? cameraData.defaultOpaqueSortFlags
                    : SortingCriteria.CommonTransparent;

            DrawingSettings drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _settings.FilteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // or FrameCleanup? In most situations it doesn't matter.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            foreach (var colorTarget in _settings.customColorTargets.Where(target => target.enabled = true))
            {
                // Commented out in favor of linq usage in line 186
                // if (!colorTarget.enabled) continue;

                // cmd.ReleaseTemporaryRT(colorTarget.NameID);
                cmd.ReleaseTemporaryRT(colorTarget.GetNameID());
            }

            if (_settings.customDepthTarget.enabled)
            {
                // cmd.ReleaseTemporaryRT(_settings.customDepthTarget.NameID);
                cmd.ReleaseTemporaryRT(_settings.customDepthTarget.GetNameID());
            }
        }
    }
}