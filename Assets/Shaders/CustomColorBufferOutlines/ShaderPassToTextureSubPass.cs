using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.RenderTextureFormat;


namespace Shaders.CustomColorBufferOutlines
{
    public enum SubPassTargetType
    {
        Color,
        Depth
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "ShaderPassToTextureSubclass", menuName = "Rendering/Universal Render Pipeline/ShaderPassToTextureSubClass")]
    public class ShaderPassToTextureSubPass : ScriptableObject
    {
        public string shaderName;
        public string textureName;
        public bool createTexture;
        public int texDepthBits;
        public RenderTextureFormat format;
        public SubPassTargetType targetType;

        [Serializable]
        private class ShaderPassToTextureSubPassSettings
        {
            public string _shaderName = "Outline Post Process";
            public string _textureName = "_OutlineOpaqueTexture";
            public bool _createTexture = true;
            public int _texDepthBits = 24;
            public RenderTextureFormat _format = ARGBFloat;
            [ReadOnly] public SubPassTargetType _targetType = SubPassTargetType.Color;
        }

        ///<summary>
        /// Create new SubPassSettings instance.
        /// </summary>
        private ShaderPassToTextureSubPassSettings _subpassSettings = new();


        // public ShaderPassToTextureSubPass()
        // {
        //     var sub = _subpassSettings;
        // shaderName = sub.shaderName;
        // textureName = sub.textureName;
        // createTexture = sub.createTexture;
        // texDepthBits = sub.texDepthBits;
        // format = sub.format;
        // targetType = sub.targetType;
        // }

        private void OnValidate()
        {
            if (format is Depth)
            {
                targetType = SubPassTargetType.Depth;
            }
            else if (format is not Shadowmap)
            {
                targetType = SubPassTargetType.Color;
            }
        }
    }
}