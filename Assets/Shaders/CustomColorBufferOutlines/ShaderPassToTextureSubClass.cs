using UnityEngine;
using UnityEngine.Rendering;

using static UnityEngine.RenderTextureFormat;


namespace Shaders.CustomColorBufferOutlines
{
    public enum SubClassTargetType
    {
        Color,
        Depth
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "ShaderPassToTextureSubclass", menuName = "Rendering/Universal Render Pipeline/ShaderPassToTextureSubClass")]
    public class ShaderPassToTextureSubClass : ScriptableObject
    {
        /// <summary>
        /// Settings to be exposed in the inspector for each ShaderPassToTextureSubClass.
        /// </summary>
        [System.Serializable]
        public class ShaderPassToTextureSubclassSettings
        {
            public string shaderName = "Outline Post Process";
            public string textureName = "_OutlineOpaqueTexture";
            public bool createTexture = true;
            public int texDepthBits = 24;
            public RenderTextureFormat format = Default;
            public SubClassTargetType targetType = SubClassTargetType.Color;
        }

        ///<summary>
        /// Create new SubClassSettings instance.
        /// </summary>
        private ShaderPassToTextureSubclassSettings _subclassSettings = new();

        public string shaderName;
        public string textureName;
        public bool createTexture;
        public int texDepthBits;
        public RenderTextureFormat format;
        public SubClassTargetType targetType;

        public ShaderPassToTextureSubClass()
        {
            var sub = _subclassSettings;
            shaderName = sub.shaderName;
            shaderName = sub.shaderName;
            textureName = sub.textureName;
            createTexture = sub.createTexture;
            texDepthBits = sub.texDepthBits;
            format = sub.format;
            targetType = sub.targetType;
        }
    }
}