using System;
using UnityEngine;

namespace Plugins.ScriptableRenderPass
{
    public static class HelperEnums
    {
    
        // Used in DrawToRT and DrawToMRT. Based on the enum used in Unity's RenderObjects feature.
        [Serializable]
        public enum RenderQueueType
        {
            Opaque = 0,
            Transparent = 1
        }

        // [Serializable]
        // public enum RenderRequestResultTarget
        // {
        //     BuiltInRenderTextureType = 0,    
        // }   
        
        // Used by DrawToMRT. An attempt to decrease the amount of times I checked if a target was enabled.
        [HideInInspector]
        public enum CameraTargetMode
        {
            Color = 0,
            ColorAndDepth = 1
        }
    
        // TODO: Experiment with graphics debugger view.
        public enum DebugTargetView
        {
            None = 0,
            ColorBuffer = 1,
            Depth = 2,
            BlurResults = 3,
            OutlinesOnly = 4
        }
    }
}