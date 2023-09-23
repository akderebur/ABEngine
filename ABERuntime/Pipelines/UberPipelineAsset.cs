﻿using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipelineAsset : PipelineAsset
    {
        public UberPipelineAsset(Framebuffer fb) : base(fb, false, false)
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedSpriteNormalLayout);
            shaderOptimised = false;
            defaultMatName = "UberStandard";

            PipelineAsset.ParseAsset(Shaders.UberPipelineAsset, this);

            GraphicsPipelineDescription uberPipelineDesc = new GraphicsPipelineDescription(
                new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.AlphaBlend, BlendAttachmentDescription.AlphaBlend),
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[]
                    {
                      GraphicsManager.sharedVertexLayout
                    },
                    shaders),
                resourceLayouts.ToArray(),
                fb.OutputDescription);

            pipeline = rf.CreateGraphicsPipeline(ref uberPipelineDesc);
        }

        public override void BindPipeline()
        {
            base.BindPipeline();

            // Resource sets
            cl.SetGraphicsResourceSet(0, Game.pipelineSet);
        }
    }
}
