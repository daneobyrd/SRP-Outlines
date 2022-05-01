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

//TODO: Experiment with graphics debugger view.
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
public class EdgeDetectionSettings
{
    public EdgeDetectionMethod edgeMethod;
    public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    [Header("Blit to Screen")]
    public Material blitMaterial;
}

[Serializable]
public class BlurSettings
{
    public bool enabled;
    public BlurType type;

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

    // [Tooltip("Rotations.")] public int rotations = 8;

    [Tooltip("Depth Push.")] public float depthPush = 1e-6f;
    // [Tooltip("Object LUT.")] public Texture2D outerLUT;
    // [Tooltip("Inner LUT.")] public Texture2D innerLUT;
}

#endregion

public class OutlineRenderer : ScriptableRendererFeature
{
    [HideInInspector] public string profilerTag = nameof(OutlineRenderer);

    public DrawToRTSettings opaqueSettings = new("Linework Opaque Pass", RenderQueueType.Opaque);
    public DrawToRTSettings transparentSettings = new("Linework Transparent Pass", RenderQueueType.Transparent);
    public BlurSettings blur = new();
    public EdgeDetectionSettings edge = new();
    public OutlineShaderProperties shaderProps = new();
    // public MaterialPropertyBlock outline_PropertyBlock = new MaterialPropertyBlock();

    // Render Passes
    private DrawToRT _lineworkOpaquePass;
    private DrawToRT _lineworkTransparentPass;
    private BlurPass _blurPass;
    private FullscreenEdgeDetection _computeLines;
    
    private Material OutlineEncoderMaterial
    {
        get => edge.blitMaterial;
        set => edge.blitMaterial = value;
    }
    private Shader OutlineEncoderShader
    {
        get => OutlineEncoderMaterial.shader;
        set => OutlineEncoderMaterial.shader = value;
    }

    private static class OutlineShaderIDs
    {
        public static readonly int OuterThreshold = Shader.PropertyToID("_OuterThreshold");
        public static readonly int InnerThreshold = Shader.PropertyToID("_InnerThreshold");
        // public static readonly int Rotations = Shader.PropertyToID("_Rotations");
        public static readonly int DepthPush = Shader.PropertyToID("_DepthPush");
        // public static readonly int OuterLut = Shader.PropertyToID("_OuterLUT");
        // public static readonly int InnerLut = Shader.PropertyToID("_InnerLUT");
    }
    
    private (bool, bool) _enabledPasses;
    
    public override void Create()
    {
        if (_enabledPasses.Item1) { _lineworkOpaquePass      = new DrawToRT(opaqueSettings); }
        if (_enabledPasses.Item2) { _lineworkTransparentPass = new DrawToRT(transparentSettings); }
        
        if (blur.enabled) _blurPass = new BlurPass("Blur Pass");

        _computeLines = new FullscreenEdgeDetection(edge.renderPassEvent, "Outline Encoder");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        GetMaterial();
        if (GetMaterial() == false)
        {
            return;
        }

        #region Set Global Outline Properties

        Shader.SetGlobalFloat(OutlineShaderIDs.OuterThreshold, shaderProps.outerThreshold);
        Shader.SetGlobalFloat(OutlineShaderIDs.InnerThreshold, shaderProps.innerThreshold);
        // Shader.SetGlobalInt(OutlineShaderIDS.Rotations, shaderProps.rotations);
        Shader.SetGlobalFloat(OutlineShaderIDs.DepthPush, shaderProps.depthPush);
        // Shader.SetGlobalTexture(OutlineShaderIDS.OuterLut, shaderProps.outerLUT);
        // Shader.SetGlobalTexture(OutlineShaderIDS.InnerLut, shaderProps.innerLUT);

        #endregion
        
        if (_enabledPasses.Item1) // opaque
        {
            _lineworkOpaquePass.Setup();
            renderer.EnqueuePass(_lineworkOpaquePass);
        }
        if (_enabledPasses.Item2) // transparent
        {
            _lineworkTransparentPass.Setup();
            renderer.EnqueuePass(_lineworkTransparentPass);
        }

        // Blur Compute Shaders
        var gaussian = (ComputeShader) Resources.Load("Compute/Blur/ColorPyramid");
        var kawase = (ComputeShader) Resources.Load("Compute/Blur/KawaseCS");

        // Edge Detection Compute Shaders        
        var laplacian = (ComputeShader) Resources.Load("Compute/EdgeDetection/Laplacian/Laplacian");
        var freiChen = (ComputeShader) Resources.Load("Compute/EdgeDetection/Frei-Chen/FreiChen");
        
        ComputeShader edgeCS = null;
        string outlineSource;
        int outlineID = 0;

        switch (edge.edgeMethod)
        {
            // Only use Blur Pass for Laplacian
            case EdgeDetectionMethod.Laplacian:
                if (blur.enabled)
                {
                    var blurCS = blur.type switch
                    {
                        BlurType.Gaussian => gaussian,
                        BlurType.Kawase   => kawase,
                        _                 => kawase
                    };

                    _blurPass.Init(opaqueSettings.customColorTargets[0].textureName,
                                   blur.type,
                                   blurCS,
                                   blur.blurPasses,
                                   blur.threshold,
                                   blur.intensity);

                    renderer.EnqueuePass(_blurPass);

                    outlineSource = "_FinalBlur";
                    outlineID     = Shader.PropertyToID(outlineSource);
                }
                else
                {
                    outlineSource = opaqueSettings.customColorTargets[0].textureName;
                    outlineID     = opaqueSettings.customColorTargets[0].GetNameID();
                }
                
                edgeCS        = laplacian;
                break;
            case EdgeDetectionMethod.FreiChen or EdgeDetectionMethod.AltFreiChen:
                outlineSource = opaqueSettings.customColorTargets[0].textureName;
                outlineID     = opaqueSettings.customColorTargets[0].GetNameID();
                edgeCS        = freiChen;
                break;
            case EdgeDetectionMethod.Sobel:
                break;
        }
        
        int passIndex = opaqueSettings.customDepthTarget.enabled ? 1 : 0;
        
        _computeLines.Setup(outlineID, edgeCS, edge.edgeMethod);
        _computeLines.InitMaterial(OutlineEncoderMaterial, passIndex);
        renderer.EnqueuePass(_computeLines);
    }
    
    private void OnValidate()
    {
        // Get enabled passes once rather than checking every frame.
        _enabledPasses = (opaqueSettings.enabled, transparentSettings.enabled);
        
        if (_enabledPasses == (false, false))
        {
            Debug.LogWarningFormat(
                "{0}.Create(): No enabled pass. Make sure to enable either the opaque or transparent pass in the renderer feature.",
                GetType().Name);
        }

        GetMaterial();
    }
    
    private bool GetMaterial()
    {
        if (OutlineEncoderMaterial != edge.blitMaterial)
        {
            OutlineEncoderShader = Load(edge.blitMaterial);
            if (!OutlineEncoderMaterial)
            {
                OutlineEncoderMaterial = Load(OutlineEncoderShader);
            }

            return true;
        }
        if (OutlineEncoderShader == null)
        {
            OutlineEncoderShader = Load(edge.blitMaterial);
        }
        return OutlineEncoderShader != null;
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

    private Shader Load(Material material)
    {
        var declaringType = GetType().DeclaringType;
        if (material == null)
        {
            if (declaringType != null)
            {
                Debug.LogErrorFormat(
                    $"Missing material. {declaringType.Name} render pass will not execute. Check for missing reference in the renderer feature settings.");
                return null;
            }
        }
        else if (material.shader == null)
        {
            if (declaringType != null)
            {
                Debug.LogErrorFormat(
                    $"Shader error. {declaringType.Name} render pass will not execute. Check the shader used by the material reference in the renderer feature settings.");
                return null;
            }
        }

        return material.shader;
    }
    
}