// This code was inspired by a Renderer Feature written by Harry Heath.
//      Twitter: https://twitter.com/harryh___h/status/1328024692431540224
//      Pastebin: https://pastebin.com/rvju9psM
// The original code was used in a recreation of a Mako illustration:
//      https://twitter.com/harryh___h/status/1328006632102526976

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Shaders.OutlineBuffers
{
    public enum RenderQueueType
    {
        Opaque = 0,
        Transparent = 1
    }

    public enum DebugTargetView
    {
        None,
        ColorTarget_1,
        Depth,
        BlurResults,
        EdgeResults
    }

    [System.Serializable]
    public class OutlineSettings
    {
        [HideInInspector] public string profilerTag = nameof(OutlineRenderer);

        struct outline_obj
        {
            private Vector3 position;
            private uint unity_InstanceID;
        }

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
        // public readonly List<string> passNames;

        public FilterSettings()
        {
            renderQueueType = RenderQueueType.Opaque;
            layerMask = 0;
        }
    }

    [System.Serializable]
    public class LineworkSettings
    {
        public PassSubTarget colorSubTarget = new(new List<string> { "Outline" }, "_OutlineOpaque", true, false, RenderTextureFormat.ARGBFloat);
        public PassSubTarget depthSubTarget = new(new List<string> { "Outline" }, "_OutlineDepth", true, true, RenderTextureFormat.Depth);
    }

    // -----------------------------------------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public class EdgeDetectionSettings
    {
        public ComputeShader computeBlur;
        public Material blitMaterial;
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
        public OutlineSettings outlineSettings = new();
        public FilterSettings filter => outlineSettings.filterSettings;
        public LineworkSettings linework => outlineSettings.lineworkSettings;
        public EdgeDetectionSettings edge => outlineSettings.edgeSettings;
        private OutlineShaderProperties shaderProps => outlineSettings.outlineProperties;


        private ShaderPassToRT _lineworkPass;
        private FullscreenEdgeDetectionBlit _computeLinesAndBlitPass;

        private Material _outlineEncoderMaterial;
        private Shader outlineEncoderShader => outlineSettings.edgeSettings.outlineEncoder;

        private static readonly int OuterThreshold = Shader.PropertyToID("_OuterThreshold");
        private static readonly int InnerThreshold = Shader.PropertyToID("_InnerThreshold");
        private static readonly int Rotations = Shader.PropertyToID("_Rotations");
        private static readonly int DepthPush = Shader.PropertyToID("_DepthPush");
        private static readonly int OuterLut = Shader.PropertyToID("_OuterLUT");
        private static readonly int InnerLut = Shader.PropertyToID("_InnerLUT");

        public override void Create()
        {
            _lineworkPass = new ShaderPassToRT(outlineSettings, "LineworkPass", outlineSettings.renderPassEvent, 24);
            _computeLinesAndBlitPass = new FullscreenEdgeDetectionBlit("Outline Encoder");
            GetMaterial();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. Make sure to add a blit material, or make sure {1} exists.",
                    GetType().Name, outlineEncoderShader);
                return;
            }

            Shader.SetGlobalFloat(OuterThreshold, shaderProps.outerThreshold);
            Shader.SetGlobalFloat(InnerThreshold, shaderProps.innerThreshold);
            Shader.SetGlobalInt(Rotations, shaderProps.rotations);
            Shader.SetGlobalFloat(DepthPush, shaderProps.depthPush);
            Shader.SetGlobalTexture(OuterLut, shaderProps.outerLUT);
            Shader.SetGlobalTexture(InnerLut, shaderProps.innerLUT);

            _lineworkPass.Init(true);
            renderer.EnqueuePass(_lineworkPass);
            _computeLinesAndBlitPass.Init(_outlineEncoderMaterial, "_OutlineTexture", true);
            renderer.EnqueuePass(_computeLinesAndBlitPass);
        }


        private bool GetMaterial()
        {
            if (_outlineEncoderMaterial && outlineSettings.edgeSettings.blitMaterial)
            {
                return true;
            }

            if (outlineEncoderShader == null || !outlineSettings.edgeSettings.blitMaterial) return false;
            _outlineEncoderMaterial = new Material(outlineEncoderShader);
            return true;
        }

        /*private bool GetComputeShader()
        {
            if (outlineSettings.edgeSettings.computeBlur != null) return true;
            outlineSettings.edgeSettings.computeBlur = (ComputeShader)Resources.Load("ColorPyramid.compute");
            return true;
        }
        private void OnValidate()
        {
            // GetComputeShader();
            // GetMaterial();
        }*/
    }
}