﻿// This is a modified version of a Renderer Feature written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/rvju9psM

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Shaders.CustomColorBufferOutlines
{
    public enum RenderQueueType
    {
        Opaque = 0,
        Transparent = 1
    }

    [System.Serializable]
    public class OutlineSettings
    {
        public string profilerTag;

        /// <summary>
        /// List containing all ShaderPassToTextureSubClass scriptable objects.
        /// </summary>
        public List<ShaderPassToTextureSubPass> outlinePassSubPassList;

        public RenderQueueType renderQueueType;
        public RenderPassEvent renderPassEvent;

        [Header("Blit Settings")] public Material blitMaterial;
        public Shader outlineEncoder;

        [Space(10)] [Header("Shader Properties")] [Tooltip("Object Threshold.")]
        public float outerThreshold;

        [Tooltip("Inner Threshold.")] public float innerThreshold;

        [Tooltip("Rotations.")] public int rotations;
        [Tooltip("Depth Push.")] public float depthPush;

        [Tooltip("Object LUT.")] public Texture2D outerLUT;
        [Tooltip("Inner LUT.")] public Texture2D innerLUT;

        // Initialize outline settings.
        public OutlineSettings(List<ShaderPassToTextureSubPass> list, RenderQueueType type, RenderPassEvent passEvent,
            Material blitMat, Shader outlineEncode)
        {
            this.profilerTag = nameof(OutlineFeature);
            this.outlinePassSubPassList = list;
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

    public class OutlineFeature : ScriptableRendererFeature
    {
        public OutlineSettings outlineSettings;

        ShaderPassToTextureRenderer LineworkPass;

        FullscreenQuadRenderer ComputeLinesAndBlitPass;

        private Material outlineEncoderMaterial;
        private Shader OutlineEncoderShader => outlineSettings.outlineEncoder;

        private static readonly int HhoOuterThreshold = Shader.PropertyToID("_HHO_OuterThreshold");

        private static readonly int HhoInnerThreshold = Shader.PropertyToID("_HHO_InnerThreshold");

        private static readonly int HhoRotations = Shader.PropertyToID("_HHO_Rotations");
        private static readonly int HhoDepthPush = Shader.PropertyToID("_HHO_DepthPush");
        private static readonly int HhoOuterLut = Shader.PropertyToID("_HHO_OuterLUT");
        private static readonly int HhoInnerLut = Shader.PropertyToID("_HHO_InnerLUT");

        public override void Create()
        {
            var s = outlineSettings;
            if (s.outlinePassSubPassList is null or { Count: 0 })
            {
                s.outlinePassSubPassList = new List<ShaderPassToTextureSubPass> { CreateInstance<ShaderPassToTextureSubPass>() };
            }

            LineworkPass = new ShaderPassToTextureRenderer(s.profilerTag, s.outlinePassSubPassList, s.renderQueueType, s.renderPassEvent);
            ComputeLinesAndBlitPass = new FullscreenQuadRenderer("Outline Encoder");
            GetMaterial();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. Make sure to add a blit material, or make sure {1} exists.",
                    GetType().Name, OutlineEncoderShader);
                return;
            }

            Shader.SetGlobalFloat(HhoOuterThreshold, outlineSettings.outerThreshold);
            Shader.SetGlobalFloat(HhoInnerThreshold, outlineSettings.innerThreshold);
            Shader.SetGlobalInt(HhoRotations, outlineSettings.rotations);
            Shader.SetGlobalFloat(HhoDepthPush, outlineSettings.depthPush);
            Shader.SetGlobalTexture(HhoOuterLut, outlineSettings.outerLUT);
            Shader.SetGlobalTexture(HhoInnerLut, outlineSettings.innerLUT);

            renderer.EnqueuePass(LineworkPass);
            ComputeLinesAndBlitPass.Init(outlineEncoderMaterial, "_OutlineTexture", true);
            renderer.EnqueuePass(ComputeLinesAndBlitPass);
        }

        private bool GetMaterial()
        {
            if (outlineEncoderMaterial && outlineSettings.blitMaterial)
            {
                return true;
            }

            if (OutlineEncoderShader == null || !outlineSettings.blitMaterial) return false;
            outlineEncoderMaterial = new Material(OutlineEncoderShader);
            return true;
        }
    }
}