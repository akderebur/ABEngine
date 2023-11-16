using System;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class ToonWaterPipeline : PipelineAsset
    {
        public ToonWaterPipeline() : base()
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
            defaultMatName = "ToonWater";

            base.ParseAsset(Shaders3D.ToonWaterAsset, false);

            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

            GraphicsPipelineDescription toonWaterDesc = new GraphicsPipelineDescription(
                new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.AlphaBlend, BlendAttachmentDescription.AlphaBlend),
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
                Game.resourceContext.mainRenderFB.OutputDescription);
            pipeline = rf.CreateGraphicsPipeline(ref toonWaterDesc);
        }
    }
}

