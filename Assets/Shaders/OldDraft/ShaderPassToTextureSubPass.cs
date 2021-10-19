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
    public class SubPassData
    {
        public string _shaderName = "Outline Post Process";
        public string _textureName = "_OutlineOpaqueTexture";
        public bool _createTexture = true;
        public int _texDepthBits = 24;
        public RenderTextureFormat _format = ARGBFloat;
        [ReadOnly] public SubPassTargetType _targetType = SubPassTargetType.Color;
    }

    [System.Serializable]
    [CreateAssetMenu(fileName = "ShaderPassToTextureSubPass", menuName = "Rendering/Universal Render Pipeline/ShaderPassToTextureSubPass")]
    public class ShaderPassToTextureSubPass : ScriptableObject
    {
        ///<summary>
        /// Create new SubPassSettings instance.
        /// </summary>
        public SubPassData _SubPassData { get; set; } = new();

        public string shaderName => _SubPassData._shaderName;
        public string textureName => _SubPassData._textureName;
        public bool createTexture => _SubPassData._createTexture;
        public int texDepthBits => _SubPassData._texDepthBits;
        public RenderTextureFormat RTFormat => _SubPassData._format;
        public SubPassTargetType targetType;
        
        private void OnValidate()
        {
            if (RTFormat is Depth)
            {
                targetType = SubPassTargetType.Depth;
            }
            else if (RTFormat is not Shadowmap)
            {
                targetType = SubPassTargetType.Color;
            }
        }
    }
}