using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class WaterPipelineAsset : PipelineAsset
    {
        public WaterPipelineAsset() : base()
        {
            resourceLayouts.Add(GraphicsManager.sharedTextureLayout);
            defaultMatName = "WaterMat";

            base.ParseAsset(Shaders.WaterPipelineAsset, false);

            GraphicsPipelineDescription waterPipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                GraphicsManager.gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
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
            pipeline = rf.CreateGraphicsPipeline(ref waterPipelineDesc);
        }
    }
}
