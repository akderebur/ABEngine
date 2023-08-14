using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipelineAdditive : PipelineAsset
    {
        public UberPipelineAdditive(Framebuffer fb) : base(fb, false, false)
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedTextureLayout);
            shaderOptimised = false;
            defaultMatName = "UberAdditive";

            PipelineAsset.ParseAsset(Shaders.UberPipelineAsset, this);

            GraphicsPipelineDescription uberPipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAdditiveBlend,
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
