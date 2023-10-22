using System;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipeline3D : PipelineAsset
    {
        public UberPipeline3D() : base()
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
            shaderOptimised = false;
            defaultMatName = "Uber3D";

            PipelineAsset.ParseAsset(Shaders3D.UberPipeline3DAsset, this);

            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

            GraphicsPipelineDescription uber3DDesc = new GraphicsPipelineDescription(
                new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.AlphaBlend, BlendAttachmentDescription.AlphaBlend),
                DepthStencilStateDescription.DepthOnlyLessEqual,
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
            pipeline = rf.CreateGraphicsPipeline(ref uber3DDesc);
        }
    }
}

