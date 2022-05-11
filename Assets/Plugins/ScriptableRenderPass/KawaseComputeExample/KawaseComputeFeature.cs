namespace Plugins.ScriptableRenderPass
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    [Serializable]
    public class KawaseBlurSettings
    {
        public bool enabled;
        public Material blurMaterial;

        [Range(3, 5)] public int blurPasses = 5;
        public float threshold;
        public float intensity;
    }

    public sealed class KawaseComputeFeature : ScriptableRendererFeature
    {
        public KawaseBlurSettings kawaseSettings;
        private KawaseBlurPass _kawaseBlur;

        public override void Create()
        {
            var kawaseCS = (ComputeShader) Resources.Load("Compute/Blur/KawaseCS");
            var ks = kawaseSettings;

            if (!kawaseSettings.enabled) return;
            
            _kawaseBlur = new KawaseBlurPass("Kawase Compute Render Pass");
            _kawaseBlur.Setup(ks.blurMaterial, kawaseCS, ks.blurPasses, ks.threshold, ks.intensity);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
        {
            if (!kawaseSettings.enabled) return;
            renderer.EnqueuePass(_kawaseBlur);
        }
    }
}