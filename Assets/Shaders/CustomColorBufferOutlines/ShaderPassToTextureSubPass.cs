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
        /// <summary>
        /// Settings to be exposed in the inspector for each ShaderPassToTextureSubClass.
        /// </summary>
        [System.Serializable]
        public class ShaderPassToTextureSubPassSettings
        {
            public string shaderName = "Outline Post Process";
            public string textureName = "_OutlineOpaqueTexture";
            public bool createTexture = true;
            public int texDepthBits = 24;
            public RenderTextureFormat format = Default;
            public SubPassTargetType targetType = SubPassTargetType.Color;
        }

        ///<summary>
        /// Create new SubPassSettings instance.
        /// </summary>
        private ShaderPassToTextureSubPassSettings _subpassSettings = new();

        public string shaderName;
        public string textureName;
        public bool createTexture;
        public int texDepthBits;
        public RenderTextureFormat format;
        public SubPassTargetType targetType;

        public ShaderPassToTextureSubPass()
        {
            var sub = _subpassSettings;
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