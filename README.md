# SRP-Outlines

This is a **Unity 2021.2.2f1** project using the **Universal Render Pipeline** and utilizing features of the Scriptable Render Pipeline to create a fullscreen outline effect.

The important files can be found in `Assets/Resources/`. You can find different compute shaders I am using and my SRP feature/passes there. Currently I have additional reference and draft files within the compute folder.

Most recent work is in the `dev` branch.

An older *incomplete* attempt is currently in the `OldDraft` branch for preservation/reference only. Do not try to use it, it doesn't work.

[v3.0](https://github.com/UnityTechnologies/PhotoMode/releases/tag/v3.0) of Unity's [PhotoMode](https://github.com/UnityTechnologies/PhotoMode) package can be found in `Assets/Plugins/PhotoMode`.

I've included the Unity's outdated Blit, DrawFullscreenFeature, from Unity's [Universal Rendering Examples](https://github.com/Unity-Technologies/UniversalRenderingExamples/tree/master/Assets/Scripts/Runtime/RenderPasses) repo in `Assets/Plugins/DrawFullscreenFeature`.
I have been using the RenderObjectsPass for reference here and there but my scriptable render passes are not 1:1 in structure or implementation to any of the reference scripts.

_The original file and related files can be found at the following paths:_

    RenderObjects.cs
    ---------- \Library\PackageCache\com.unity.render-pipelines.universal@12.0.0\Runtime\RendererFeatures
    RenderObjectsPass.cs 
    ---------- \Library\PackageCache\com.unity.render-pipelines.universal@12.0.0\Runtime\Passes
    RenderObjectsPassFeatureEditor.cs 
    ---------- \Library\PackageCache\com.unity.render-pipelines.universal@12.0.0\Editor\RendererFeatures
