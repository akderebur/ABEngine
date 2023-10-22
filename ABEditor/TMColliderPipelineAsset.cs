using System;
using System.Text;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Pipelines;
using Veldrid;
using Veldrid.SPIRV;

namespace ABEditor.Debug
{
    public class TMColliderPipelineAsset : PipelineAsset
    {
        public TMColliderPipelineAsset() : base()
        {
            // Line Pipeline

            // Line shaders
            ShaderDescription lineVS = new ShaderDescription(
               ShaderStages.Vertex,
               Encoding.UTF8.GetBytes(Shaders.LineDebugVertex),
               "main");

            ShaderDescription lineFS = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(Shaders.LineDebugFragment),
                "main");

            var lightShaders = rf.CreateFromSpirv(lineVS, lineFS);

            var vertLayout = new VertexLayoutDescription(
                            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3));

            GraphicsPipelineDescription linePipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                GraphicsManager.gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.LineList,
                new ShaderSetDescription(
                    new[]
                    {
                        vertLayout
                    },
                    lightShaders),
                new ResourceLayout[] { GraphicsManager.sharedPipelineLayout },
                Game.resourceContext.lightRenderFB.OutputDescription);

            pipeline = rf.CreateGraphicsPipeline(ref linePipelineDesc);
        }

        public override void BindPipeline()
        {
            base.BindPipeline();

            // Resource sets
            cl.SetGraphicsResourceSet(0, Game.pipelineSet);
        }
    }
}

