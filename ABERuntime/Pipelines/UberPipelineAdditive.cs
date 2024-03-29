﻿using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipelineAdditive : PipelineAsset
    {
        public UberPipelineAdditive() : base()
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedTextureLayout);
            defaultMatName = "UberAdditive";

            base.ParseAsset(Shaders.UberPipelineAsset, false);

            GraphicsPipelineDescription uberPipelineDesc = new GraphicsPipelineDescription(
                new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.AdditiveBlend, BlendAttachmentDescription.AdditiveBlend),
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
                Game.resourceContext.mainRenderFB.OutputDescription);
            pipeline = rf.CreateGraphicsPipeline(ref uberPipelineDesc);
        }
    }
}
