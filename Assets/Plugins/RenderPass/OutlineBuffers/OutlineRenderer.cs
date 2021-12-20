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

#region Enums

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

#endregion

#region Settings Fields

[Serializable]
public class OutlineSettings
{
    [HideInInspector] public string profilerTag = nameof(OutlineRenderer);
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    public DebugTargetView debugTargetView;
    public FilterSettings filterSettings = new();
    public LineworkSettings lineworkSettings = new();
    public EdgeDetectionSettings edgeSettings = new();
    public BlurSettings blurSettings = new();
    public OutlineShaderProperties outlineProperties = new();
}

[Serializable]
public class FilterSettings
{
    public RenderQueueType renderQueueType;
    public LayerMask layerMask;
    [Range(-1, 7)] public int lightLayerMask;

    public FilterSettings()
    {
        renderQueueType = RenderQueueType.Opaque;
        layerMask       = 0;
        lightLayerMask  = -1;
    }
}

[Serializable]
public class LineworkSettings
{
    public PassSubTarget colorSubTarget;
    public PassSubTarget depthSubTarget;

    public LineworkSettings()
    {
        colorSubTarget =
            new PassSubTarget(new List<string> {"Outline"}, "_OutlineOpaque", SubTargetType.Color, true,
                              RenderTextureFormat.ARGBFloat);
        depthSubTarget =
            new PassSubTarget(new List<string> {"Outline"}, "_OutlineDepth", SubTargetType.Depth, false);
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
    [Range(2, 5)] public int pyramidLevels = 5;
}

[Serializable]
public class OutlineShaderProperties
{
    [Tooltip("Object Threshold."), Range(0.0000001f, 1f)]
    public float outerThreshold = 0.25f;

    [Tooltip("Inner Threshold."), Range(0.0000001f, 1f)]
    public float innerThreshold = 0.25f;

    [Tooltip("Rotations.")] public int rotations = 8;

    [Tooltip("Depth Push.")] public float depthPush = 1e-6f;
    // [Tooltip("Object LUT.")] public Texture2D outerLUT;
    // [Tooltip("Inner LUT.")] public Texture2D innerLUT;
}

#endregion

public class OutlineRenderer : ScriptableRendererFeature
{
    public OutlineSettings settings = new();

    #region Private Settings

    private FilterSettings          filter      => settings.filterSettings;
    private LineworkSettings        linework    => settings.lineworkSettings;
    private EdgeDetectionSettings   edge        => settings.edgeSettings;
    private BlurSettings            blur        => settings.blurSettings;
    private OutlineShaderProperties shaderProps => settings.outlineProperties;

    #endregion

    private ShaderPassToRT _lineworkPass;
    private BlurPass _blurPass;
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
        _blurPass     = new BlurPass("Blur Pass");
        _computeLines = new FullscreenEdgeDetection(RenderPassEvent.BeforeRenderingTransparents, "Outline Encoder");

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

        #region Set Global Outline Properties

        Shader.SetGlobalFloat(OuterThreshold, shaderProps.outerThreshold);
        Shader.SetGlobalFloat(InnerThreshold, shaderProps.innerThreshold);
        // Shader.SetGlobalInt(Rotations, shaderProps.rotations);
        Shader.SetGlobalFloat(DepthPush, shaderProps.depthPush);
        // Shader.SetGlobalTexture(OuterLut, shaderProps.outerLUT);
        // Shader.SetGlobalTexture(InnerLut, shaderProps.innerLUT);

        #endregion

        ComputeShader edgeCompute = null;
        string outlineSource = null;

        _lineworkPass.ShaderTagSetup(linework.depthSubTarget.createTexture);
        renderer.EnqueuePass(_lineworkPass);
        
        switch (edge.edgeMethod)
        {
            case EdgeDetectionMethod.Laplacian:
                var blurCompute = blur.type switch
                {
                    BlurType.Gaussian => blur.gaussianCompute,
                    BlurType.Kawase   => blur.kawaseCompute,
                    _                 => throw new ArgumentOutOfRangeException()
                };
                _blurPass.Init(linework.colorSubTarget.textureName, blur.type, blurCompute, blur.pyramidLevels);
                renderer.EnqueuePass(_blurPass);

                outlineSource = "_FinalBlur";
                edgeCompute   = edge.laplacianCompute;
                break;
            case EdgeDetectionMethod.FreiChen:
                outlineSource = linework.colorSubTarget.textureName;
                edgeCompute   = edge.freiChenCompute;
                break;
            case EdgeDetectionMethod.Sobel:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _computeLines.Setup(outlineEncoderMaterial, outlineSource, renderer.cameraColorTarget, edgeCompute,
                            edge.edgeMethod);
        renderer.EnqueuePass(_computeLines);
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