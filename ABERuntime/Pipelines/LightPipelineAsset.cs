using System;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace ABEngine.ABERuntime.Pipelines
{
    public class LightPipelineAsset : PipelineAsset
    {
        ResourceLayout texLayout;

        public LightPipelineAsset() : base()
        {

            // Light Pipeline

            // Light shaders
            ShaderDescription lightVS = new ShaderDescription(
               ShaderStages.Vertex,
               Encoding.UTF8.GetBytes(Shaders.PointLightVertex2),
               "main");

            ShaderDescription lightFS = new ShaderDescription(
                ShaderStages.Fragment,
                Encoding.UTF8.GetBytes(Shaders.PointLightFragment2),
                "main");

            var lightShaders = rf.CreateFromSpirv(lightVS, lightFS);

            var vertLayout = new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                            new VertexElementDescription("Radius", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                            new VertexElementDescription("Intensity", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                            new VertexElementDescription("Volume", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                            new VertexElementDescription("Layer", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1),
                            new VertexElementDescription("Global", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1));
            vertLayout.InstanceStepRate = 1;

            // Tex Layout
            texLayout = rf.CreateResourceLayout(
               new ResourceLayoutDescription(
                   new ResourceLayoutElementDescription("MainTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                   new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                   new ResourceLayoutElementDescription("NormalTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                   new ResourceLayoutElementDescription("NormalSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            ));

            GraphicsPipelineDescription lightPipelineDesc = new GraphicsPipelineDescription(
               new BlendStateDescription(RgbaFloat.White, false,
               new BlendAttachmentDescription
               (
                   true,
                   BlendFactor.One,
                   BlendFactor.One,
                   BlendFunction.Add,
                   BlendFactor.One,
                   BlendFactor.Zero,
                   BlendFunction.Add
               )),
                DepthStencilStateDescription.Disabled,
                //GraphicsManager.gd.IsDepthRangeZeroToOne ? DepthStencilStateDescription.DepthOnlyGreaterEqual : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[]
                    {
                         //new VertexLayoutDescription(
                         //   new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                         //   new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))

                        vertLayout
                    },
                    lightShaders),
                new ResourceLayout[] { GraphicsManager.sharedPipelineLayout, texLayout },
                Game.resourceContext.lightRenderFB.OutputDescription);

            pipeline = rf.CreateGraphicsPipeline(ref lightPipelineDesc);

            //LightInfo lightVertTest = new LightInfo(new Vector4(1f, 0f, 0f, 1f), 1f, 1f);

            //lightTestVB = _gd.ResourceFactory.CreateBuffer(new BufferDescription(LightInfo.VertexSize, BufferUsage.VertexBuffer));
            //_gd.UpdateBuffer(lightTestVB, 0, lightVertTest);
        }

        public override void BindPipeline()
        {
            base.BindPipeline();

            // Resource sets
            cl.SetGraphicsResourceSet(0, Game.pipelineSet);
        }

        public ResourceLayout GetTexResourceLayout()
        {
            return texLayout;
        }
    }
}

