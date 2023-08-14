using System;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class ToonWaterPipeline : PipelineAsset
    {
        public ToonWaterPipeline(Framebuffer fb) : base(fb, false, false, Game.mainRenderSystem.GetMainFramebuffer)
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
            shaderOptimised = false;
            defaultMatName = "ToonWater";

            PipelineAsset.ParseAsset(Shaders3D.ToonWaterAsset, this);

            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

            GraphicsPipelineDescription toonWaterDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                //DepthStencilStateDescription.DepthOnlyLessEqual,
                new DepthStencilStateDescription(true, false, ComparisonKind.LessEqual),
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
            pipeline = rf.CreateGraphicsPipeline(ref toonWaterDesc);
        }
    }
}

