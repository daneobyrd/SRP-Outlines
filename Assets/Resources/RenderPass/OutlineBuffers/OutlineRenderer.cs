// This code was inspired by a Renderer Feature written by Harry Heath.
//      Twitter: https://twitter.com/harryh___h/status/1328024692431540224
//      Pastebin: https://pastebin.com/rvju9psM
// The original code was used in a recreation of a Mako illustration:
//      https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RenderPass.OutlineBuffers
{
    public enum RenderQueueType
    {
        Opaque = 0,
        Transparent = 1
    }

    public enum DebugTargetView
    {
        None = 0,
        ColorBuffer = 1,
        Depth = 2,
        BlurResults = 3,
        OutlinesOnly = 4
    }

    [System.Serializable]
    public class OutlineSettings
    {
        [HideInInspector] public string profilerTag = nameof(OutlineRenderer);

        // NOTE: Leftover from when I thought about using structured buffers and then decided to put that off for now. 
        // struct outline_obj
        // {
        //     private Vector3 position;
        //     private uint unity_InstanceID;
        // }

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public DebugTargetView debugTargetView;
        public FilterSettings filterSettings = new();
        public LineworkSettings lineworkSettings = new();

        public EdgeDetectionSettings edgeSettings = new();
        public OutlineShaderProperties outlineProperties = new();
    }

    [System.Serializable]
    public class FilterSettings
    {
        public RenderQueueType renderQueueType;
        public LayerMask layerMask;
        [Range(-1, 7)] public int lightLayerMask;

        // public readonly List<string> passNames;

        public FilterSettings()
        {
            renderQueueType = RenderQueueType.Opaque;
            layerMask = 0;
            lightLayerMask = -1;
        }
    }

    [System.Serializable]
    public class LineworkSettings
    {
        public PassSubTarget colorSubTarget;
        public PassSubTarget depthSubTarget;

        public LineworkSettings()
        {
            colorSubTarget = new PassSubTarget(new List<string> {"Outline"}, "_OutlineOpaque", SubTargetType.Color,
                true, RenderTextureFormat.ARGBFloat);
            depthSubTarget =
                new PassSubTarget(new List<string> {"Outline"}, "_OutlineDepth", SubTargetType.Depth, true);
        }

    }

    // -----------------------------------------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public class EdgeDetectionSettings
    {
        [Header("Blur")] public ComputeShader computeBlur;
        [Range(3, 8)] public int pyramidLevels = 8;

        [Space(10)] public ComputeShader computeLines;

        [Header("Blit to Screen")] public Material blitMaterial;
        public Shader outlineEncoder;
    }

    // -----------------------------------------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public class OutlineShaderProperties
    {
        [Tooltip("Object Threshold.")] public float outerThreshold = 1.0f;
        [Tooltip("Inner Threshold.")] public float innerThreshold = 1.0f;
        [Tooltip("Rotations.")] public int rotations = 8;
        [Tooltip("Depth Push.")] public float depthPush = 1e-6f;
        [Tooltip("Object LUT.")] public Texture2D outerLUT;
        [Tooltip("Inner LUT.")] public Texture2D innerLUT;
    }

    public class OutlineRenderer : ScriptableRendererFeature
    {
        public OutlineSettings settings = new();
        public FilterSettings filter => settings.filterSettings;
        private LineworkSettings linework => settings.lineworkSettings;

        private EdgeDetectionSettings edge => settings.edgeSettings;
        private OutlineShaderProperties shaderProps => settings.outlineProperties;

        private ShaderPassToRT _lineworkPass = null;
        private GaussianBlurPass _blurPass;
        private FullscreenEdgeDetection _computeLines;

        private Shader outlineEncoderShader => settings.edgeSettings.outlineEncoder;

        private Material outlineEncoderMaterial
        {
            get => settings.edgeSettings.blitMaterial;
            set => settings.edgeSettings.blitMaterial = value;
        }
        
        private static readonly int OuterThreshold = Shader.PropertyToID("_OuterThreshold");
        private static readonly int InnerThreshold = Shader.PropertyToID("_InnerThreshold");
        private static readonly int Rotations = Shader.PropertyToID("_Rotations");
        private static readonly int DepthPush = Shader.PropertyToID("_DepthPush");
        private static readonly int OuterLut = Shader.PropertyToID("_OuterLUT");
        private static readonly int InnerLut = Shader.PropertyToID("_InnerLUT");
        
        public override void Create()
        {
            _lineworkPass = new ShaderPassToRT(settings, "Linework Pass", settings.renderPassEvent, 24);
            _blurPass = new GaussianBlurPass("Blur Pass");
            _computeLines = new FullscreenEdgeDetection(RenderPassEvent.BeforeRenderingTransparents, "Outline Encoder");

            GetMaterial();
        }


        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // if (renderingData.cameraData.cameraType == CameraType.Game)
            // {
                if (!GetMaterial())
                {
                    Debug.LogErrorFormat(
                        "{0}.AddRenderPasses(): Missing material. Make sure to add a blit material, or make sure {1} exists.",
                        GetType().Name, outlineEncoderShader);
                    return;
                }
                Shader.SetGlobalFloat(OuterThreshold, shaderProps.outerThreshold);
                Shader.SetGlobalFloat(InnerThreshold, shaderProps.innerThreshold);
                // Shader.SetGlobalInt(Rotations, shaderProps.rotations);
                // Shader.SetGlobalFloat(DepthPush, shaderProps.depthPush);
                // Shader.SetGlobalTexture(OuterLut, shaderProps.outerLUT);
                // Shader.SetGlobalTexture(InnerLut, shaderProps.innerLUT);


                var hasDepth = linework.depthSubTarget.createTexture;
                _lineworkPass.ShaderTagSetup(hasDepth);
                // _lineworkPass.ConfigureInput(ScriptableRenderPassInput.Depth);
                // _lineworkPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(_lineworkPass);

                _blurPass.Init(linework.colorSubTarget.textureName, edge.computeBlur, edge.pyramidLevels);
                renderer.EnqueuePass(_blurPass);

                _computeLines.Setup(outlineEncoderMaterial, "_BlurUpsampleTex", renderer.cameraColorTarget,
                    settings.edgeSettings.computeLines, hasDepth);
                renderer.EnqueuePass(_computeLines);
            // }
        }

        private bool GetMaterial()
            {
                if (outlineEncoderMaterial && settings.edgeSettings.blitMaterial)
                {
                    return true;
                }

                if (outlineEncoderShader == null || !settings.edgeSettings.blitMaterial) return false;
                outlineEncoderMaterial = CoreUtils.CreateEngineMaterial(outlineEncoderShader);
                return true;
            }
            /*
            private void GetMaterial()
            {
                if (outlineEncoderMaterial && settings.edgeSettings.blitMaterial)
                {
                    return;
                }
        
                if (settings.edgeSettings.blitMaterial == null) return;
        
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. Make sure to add a blit material, or make sure {1} exists.",
                    GetType().Name, outlineEncoderShader);
                
                outlineEncoderMaterial = Load(outlineEncoderShader);
            }
        
            // Copied from universal/PostProcessPass.cs
            private Material Load(Shader shader)
            {
                if (shader == null)
                {
                    var declaringType = GetType().DeclaringType;
                    if (declaringType != null)
                        Debug.LogErrorFormat(
                            $"Missing shader. {declaringType.Name} render pass will not execute. Check for missing reference in the renderer feature settings.");
                    return null;
                }
                else if (!shader.isSupported)
                {
                    return null;
                }
        
                return CoreUtils.CreateEngineMaterial(shader);
            }
        */
        
    }
}