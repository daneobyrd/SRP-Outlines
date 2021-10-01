﻿// This is a modified and more generic version of a Scriptable Render Pass written by Harry Heath.
// Twitter: https://twitter.com/harryh___h/status/1328024692431540224
// Pastebin: https://pastebin.com/LstBHRZF

// The original code was used in a recreation of a Mako illustration:
// https://twitter.com/harryh___h/status/1328006632102526976

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Shaders.CustomColorBufferOutlines
{
    ///<summary>
    /// Enum to control which version of ConfigureTarget() to use in ScriptableRenderPass Configure. Changes in response to SubClassTargetType. 
    /// </summary> 
    public enum ConfigureTargetEnum
    {
        Color = 0,
        Colors = 1,
        ColorAndDepth = 2,
        ColorsAndDepth = 3
    }

    public class ShaderPassToTextureRenderer : ScriptableRenderPass
    {
        private List<ShaderPassToTextureSubClass> m_OutlinePassSubClass = new();

        RenderQueueType m_RenderQueueType;
        FilteringSettings m_FilteringSettings;
        string m_ProfilerTag;
        ProfilingSampler m_ProfilingSampler;

        private List<ShaderTagId> m_ShaderTagIdList = new();

        private int[] m_TextureId = { };
        private string[] _textureNames = { };

        private RenderTargetIdentifier[] m_ColorAttachments = { };
        private RenderTargetIdentifier m_DepthAttachment;

        private bool[] m_CreateTexture = { };
        private int[] m_TextureDepthBits = { };
        private RenderTextureFormat[] m_TextureFormats = { };

        private List<SubClassTargetType> m_TargetType = new();
        private ConfigureTargetEnum configureTargetEnum;

        public ShaderPassToTextureRenderer(string profilerTag, List<ShaderPassToTextureSubClass> outlinePassSubClass, RenderQueueType renderQueueType,
            RenderPassEvent renderPassEvent)
        {
            bool hasDepth = false;
            profilingSampler = new ProfilingSampler(nameof(ShaderPassToTextureRenderer));

            m_ProfilerTag = profilerTag;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            this.renderPassEvent = renderPassEvent;
            this.m_RenderQueueType = renderQueueType;
            var renderQueueRange = (renderQueueType == RenderQueueType.Opaque)
                ? RenderQueueRange.opaque
                : RenderQueueRange.transparent;
            m_FilteringSettings = new FilteringSettings(renderQueueRange);

            while (outlinePassSubClass is { Count: > 0 })
            {
                for (var i = 0; i < outlinePassSubClass.Count; i++)
                {
                    m_OutlinePassSubClass[i] = outlinePassSubClass[i];
                    m_ShaderTagIdList[i] = new ShaderTagId(outlinePassSubClass[i].shaderName);
                    m_TextureId[i] = Shader.PropertyToID(outlinePassSubClass[i].textureName);
                    m_TargetType[i] = outlinePassSubClass[i].targetType;
                    switch (m_TargetType[i])
                    {
                        case SubClassTargetType.Color:
                            // TODO switch to using List.Add? 
                            m_ColorAttachments[i] = new RenderTargetIdentifier(m_TextureId[i]);
                            m_ColorAttachments[i] = colorAttachments[i];
                            m_ColorAttachments[0] = colorAttachment;
                            break;
                        case SubClassTargetType.Depth:
                            if (hasDepth) break;
                            m_DepthAttachment = new RenderTargetIdentifier(m_TextureId[i]);
                            m_DepthAttachment = depthAttachment;
                            hasDepth = true;
                            break;
                        default:
                            m_ColorAttachments[0] = colorAttachment;
                            break;
                    }

                    m_CreateTexture[i] = outlinePassSubClass[i].createTexture;
                    m_TextureDepthBits[i] = outlinePassSubClass[i].texDepthBits;
                    m_TextureFormats[i] = outlinePassSubClass[i].format;
                }
            }

            hasDepth = m_DepthAttachment.Equals(depthAttachment);
            configureTargetEnum = m_ColorAttachments switch
            {
                { Length: > 1 } => hasDepth ? ConfigureTargetEnum.ColorsAndDepth : ConfigureTargetEnum.Colors,
                not { Length: > 1 } => hasDepth ? ConfigureTargetEnum.ColorAndDepth : ConfigureTargetEnum.Color,
            };
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var texName = _textureNames.Length - 1;
            int i = 0;
            for (; i < m_CreateTexture.Length; i++)
            {
                for (; texName >= 0; texName--)
                {
                    cmd.GetTemporaryRT(m_TextureId[texName], cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                        m_TextureDepthBits[texName],
                        FilterMode.Point, m_TextureFormats[texName]);
                }
            }


            switch (configureTargetEnum)
            {
                case ConfigureTargetEnum.Color:
                    ConfigureTarget(colorAttachment);
                    break;
                case ConfigureTargetEnum.Colors:
                    ConfigureTarget(colorAttachments);
                    break;
                case ConfigureTargetEnum.ColorAndDepth:
                    ConfigureTarget(colorAttachments, depthAttachment);
                    break;
                case ConfigureTargetEnum.ColorsAndDepth:
                    ConfigureTarget(colorAttachments, depthAttachment);
                    break;
                default:
                    ConfigureTarget(colorAttachment, depthAttachment);
                    break;
            }

            for (; i < m_CreateTexture.Length; i++)
            {
                if (m_CreateTexture[i]) ConfigureClear(ClearFlag.All, Color.black);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var sortingCriteria = (m_RenderQueueType == RenderQueueType.Opaque)
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;

            var drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            foreach (var createTexBool in m_CreateTexture)
            {
                for (var texName = 0; texName < _textureNames.Length; texName++)
                {
                    if (createTexBool) cmd.ReleaseTemporaryRT(m_TextureId[texName]);
                }
            }
        }
    }
}