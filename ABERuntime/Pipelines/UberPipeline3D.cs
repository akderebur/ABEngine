using System;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipeline3D : PipelineAsset
    {
        public UberPipeline3D(Framebuffer fb) : base(fb, false, false)
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
            shaderOptimised = false;
            defaultMatName = "Uber3D";

            PipelineAsset.ParseAsset(Shaders3D.UberPipeline3DAsset, this);

            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

            GraphicsPipelineDescription uber3DDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                GraphicsManager.gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[]
                    {
                      GraphicsManager.sharedMeshVertexLayout
                    },
                    shaders),
                resourceLayouts.ToArray(),
                fb.OutputDescription);
            pipeline = rf.CreateGraphicsPipeline(ref uber3DDesc);
        }

        public override void BindPipeline()
        {
            base.BindPipeline();

            // Resource sets
            cl.SetGraphicsResourceSet(0, Game.pipelineSet);
        }
    }
}

