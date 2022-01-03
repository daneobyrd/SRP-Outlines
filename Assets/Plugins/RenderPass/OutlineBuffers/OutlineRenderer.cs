// This code was inspired by a Renderer Feature written by Harry Heath.
//      Twitter: https://twitter.com/harryh___h/status/1328024692431540224
//      Pastebin: https://pastebin.com/rvju9psM
// The original code was used in a recreation of a Mako illustration:
//      https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum DebugTargetView
{
    None = 0,
    ColorBuffer = 1,
    Depth = 2,
    BlurResults = 3,
    OutlinesOnly = 4
}

#region Settings Fields

[Serializable]
public class CustomPassSettings
{
    public ShaderPassToRTSettings opaquePassSettings;
    public ShaderPassToRTSettings transparentPassSettings;

    public CustomPassSettings()
    {
        opaquePassSettings = new ShaderPassToRTSettings("Linework Opaque Pass", RenderQueueType.Opaque);
        transparentPassSettings = new ShaderPassToRTSettings("Linework Transparent Pass", RenderQueueType.Transparent);
    }
}

[Serializable]
public class EdgeDetectionSettings
{
    public EdgeDetectionMethod edgeMethod;
    public ComputeShader laplacianCompute;
    public ComputeShader freiChenCompute;

    [Header("Blit to Screen")] public Material blitMaterial;
    public Shader outlineEncoder;
}

[Serializable]
public class BlurSettings
{
    public BlurType type;
    public ComputeShader gaussianCompute;
    public ComputeShader kawaseCompute;

    [Range(3, 5)] public int blurPasses = 5;
    public float threshold;
    public float intensity;
}

[Serializable]
public class OutlineShaderProperties
{
    [Tooltip("Object Threshold."), Range(0.0000001f, 1.4f)]
    public float outerThreshold = 0.25f;

    [Tooltip("Inner Threshold."), Range(0.0000001f, 1.4f)]
    public float innerThreshold = 0.25f;

    [Tooltip("Rotations.")] public int rotations = 8;

    [Tooltip("Depth Push.")] public float depthPush = 1e-6f;
    // [Tooltip("Object LUT.")] public Texture2D outerLUT;
    // [Tooltip("Inner LUT.")] public Texture2D innerLUT;
}

#endregion

public class OutlineRenderer : ScriptableRendererFeature
{
    [HideInInspector] public string profilerTag = nameof(OutlineRenderer);
    public DebugTargetView debugTargetView;

    public CustomPassSettings customPasses = new();
    public BlurSettings blur = new();
    public EdgeDetectionSettings edge = new();
    public OutlineShaderProperties shaderProps = new();

    private ShaderPassToRT _lineworkOpaquePass;
    private ShaderPassToRT _lineworkTransparentPass;
    private BlurPass _blurPass;
    private FullscreenEdgeDetection _computeLines;

    private Shader OutlineEncoderShader => edge.outlineEncoder;

    private Material OutlineEncoderMaterial
    {
        get => edge.blitMaterial;
        set => edge.blitMaterial = value;
    }

    private static readonly int OuterThreshold = Shader.PropertyToID("_OuterThreshold");
    private static readonly int InnerThreshold = Shader.PropertyToID("_InnerThreshold");
    private static readonly int Rotations = Shader.PropertyToID("_Rotations");
    private static readonly int DepthPush = Shader.PropertyToID("_DepthPush");
    private static readonly int OuterLut = Shader.PropertyToID("_OuterLUT");
    private static readonly int InnerLut = Shader.PropertyToID("_InnerLUT");

    public override void Create()
    {
        _lineworkOpaquePass      = new ShaderPassToRT(customPasses.opaquePassSettings);

        _lineworkTransparentPass = new ShaderPassToRT(customPasses.transparentPassSettings);

        _blurPass     = new BlurPass("Blur Pass");
        _computeLines = new FullscreenEdgeDetection(RenderPassEvent.AfterRenderingPostProcessing, "Outline Encoder");

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

        #region Set Global Outline Properties

        Shader.SetGlobalFloat(OuterThreshold, shaderProps.outerThreshold);
        Shader.SetGlobalFloat(InnerThreshold, shaderProps.innerThreshold);
        // Shader.SetGlobalInt(Rotations, shaderProps.rotations);
        Shader.SetGlobalFloat(DepthPush, shaderProps.depthPush);
        // Shader.SetGlobalTexture(OuterLut, shaderProps.outerLUT);
        // Shader.SetGlobalTexture(InnerLut, shaderProps.innerLUT);

        #endregion

        ComputeShader blurCompute;
        ComputeShader edgeCompute = null;
        
        var opaqueSettings = customPasses.opaquePassSettings;
        if (opaqueSettings.enabled)
        {
            _lineworkOpaquePass.ShaderTagSetup();
            renderer.EnqueuePass(_lineworkOpaquePass);
        }

        var transparentSettings = customPasses.transparentPassSettings;
        if (transparentSettings.enabled)
        {
            _lineworkTransparentPass.ShaderTagSetup();
            renderer.EnqueuePass(_lineworkTransparentPass);
        }

        string outlineSource = null;
        switch (edge.edgeMethod)
        {
            // Only use Blur Pass for Laplacian
            case EdgeDetectionMethod.Laplacian:
                blurCompute = blur.type switch
                {
                    BlurType.Gaussian => blur.gaussianCompute,
                    BlurType.Kawase   => blur.kawaseCompute,
                    _                 => throw new ArgumentOutOfRangeException()
                };

                _blurPass.Init(opaqueSettings.customColorTargets[0].textureName,
                               blur.type,
                               blurCompute,
                               blur.blurPasses,
                               blur.threshold,
                               blur.intensity);

                renderer.EnqueuePass(_blurPass);

                outlineSource = "_FinalBlur";
                edgeCompute   = edge.laplacianCompute;
                break;
            case EdgeDetectionMethod.FreiChen:
                outlineSource = opaqueSettings.customColorTargets[0].textureName;
                edgeCompute   = edge.freiChenCompute;
                break;
            case EdgeDetectionMethod.Sobel:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _computeLines.Setup(OutlineEncoderMaterial, outlineSource, renderer.cameraColorTarget, edgeCompute, edge.edgeMethod);
        renderer.EnqueuePass(_computeLines);
    }

    private bool GetMaterial()
    {
        if (OutlineEncoderMaterial && edge.blitMaterial)
        {
            return true;
        }

        if (OutlineEncoderShader == null || !edge.blitMaterial) return false;
        OutlineEncoderMaterial = CoreUtils.CreateEngineMaterial(OutlineEncoderShader);
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