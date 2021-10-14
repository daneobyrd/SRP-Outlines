// This is a modified version of a Renderer Feature written by Harry Heath.
//      Twitter: https://twitter.com/harryh___h/status/1328024692431540224
//      Pastebin: https://pastebin.com/rvju9psM
// The original code was used in a recreation of a Mako illustration:
//      https://twitter.com/harryh___h/status/1328006632102526976

using Shaders.CustomColorBufferOutlines;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// ReSharper disable MemberInitializerValueIgnored

namespace Shaders.New
{
    public enum RenderQueueType
    {
        Opaque = 0,
        Transparent = 1
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

        public RenderPassEvent renderPassEvent;
        public RenderQueueType renderQueueType;

        [Space(10)] [Header("Linework Settings")]
        public PassSubTarget colorTarget = new("Outline", true, RenderTextureFormat.ARGBFloat);

        public PassSubTarget depthTarget = new("Outline", true, isDepth: true);

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        [Space(10)] [Header("ComputeLines")]
        public ComputeShader computeBlur;

        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        [Space(10)] [Header("Blit")]
        public Material blitMaterial;
        public Shader outlineEncoder;


        // -----------------------------------------------------------------------------------------------------------------------------------------------------

        [Space(10)] [Header("Shader Properties")] [Tooltip("Object Threshold.")]
        public float outerThreshold;

        [Tooltip("Inner Threshold.")] public float innerThreshold;

        [Tooltip("Rotations.")] public int rotations;
        [Tooltip("Depth Push.")] public float depthPush;

        [Tooltip("Object LUT.")] public Texture2D outerLUT;
        [Tooltip("Inner LUT.")] public Texture2D innerLUT;

        // Initialize outline settings.
        private OutlineSettings(string name, PassSubTarget color, PassSubTarget depth, ComputeShader gaussianBlur, RenderQueueType type,
            RenderPassEvent passEvent,
            Material blitMat, Shader outlineEncode)
        {
            profilerTag = name;
            colorTarget = color;
            depthTarget = depth;
            computeBlur = gaussianBlur;
            renderQueueType = type;
            renderPassEvent = passEvent;
            blitMaterial = blitMat;
            outlineEncoder = outlineEncode;
            outerThreshold = 1.0f;
            innerThreshold = 1.0f;
            rotations = 8;
            depthPush = 1e-6f;
        }
    }

    public class OutlineRenderer : ScriptableRendererFeature
    {
        public OutlineSettings outlineSettings;

        private ShaderPassToRT _lineworkPass;

        private FullscreenQuadRenderer _computeLinesAndBlitPass;

        private Material _outlineEncoderMaterial;
        private Shader outlineEncoderShader => outlineSettings.outlineEncoder;

        private static readonly int HhoOuterThreshold = Shader.PropertyToID("_HHO_OuterThreshold");

        private static readonly int HhoInnerThreshold = Shader.PropertyToID("_HHO_InnerThreshold");

        private static readonly int HhoRotations = Shader.PropertyToID("_HHO_Rotations");
        private static readonly int HhoDepthPush = Shader.PropertyToID("_HHO_DepthPush");
        private static readonly int HhoOuterLut = Shader.PropertyToID("_HHO_OuterLUT");
        private static readonly int HhoInnerLut = Shader.PropertyToID("_HHO_InnerLUT");

        public OutlineRenderer()
        {
        }


        public override void Create()
        {
            var s = outlineSettings;

            _lineworkPass = new ShaderPassToRT(s.profilerTag, s.colorTarget, s.depthTarget, s.renderPassEvent, s.renderQueueType, 24);
            _computeLinesAndBlitPass = new FullscreenQuadRenderer("Outline Encoder");
            GetMaterial();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var s = outlineSettings;
            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. Make sure to add a blit material, or make sure {1} exists.",
                    GetType().Name, outlineEncoderShader);
                return;
            }

            Shader.SetGlobalFloat(HhoOuterThreshold, s.outerThreshold);
            Shader.SetGlobalFloat(HhoInnerThreshold, s.innerThreshold);
            Shader.SetGlobalInt(HhoRotations, s.rotations);
            Shader.SetGlobalFloat(HhoDepthPush, s.depthPush);
            Shader.SetGlobalTexture(HhoOuterLut, s.outerLUT);
            Shader.SetGlobalTexture(HhoInnerLut, s.innerLUT);

            renderer.EnqueuePass(_lineworkPass);
            _computeLinesAndBlitPass.Init(_outlineEncoderMaterial, "_OutlineTexture", true);
            renderer.EnqueuePass(_computeLinesAndBlitPass);
        }

        private bool GetMaterial()
        {
            if (_outlineEncoderMaterial && outlineSettings.blitMaterial)
            {
                return true;
            }

            if (outlineEncoderShader == null || !outlineSettings.blitMaterial) return false;
            _outlineEncoderMaterial = new Material(outlineEncoderShader);
            return true;
        }
    }
}