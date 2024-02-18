using System;
using System.Collections.Generic;
using System.Text;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipelineAdditive : PipelineAsset
    {
        public UberPipelineAdditive() : base()
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedSpriteNormalLayout);
            defaultMatName = "UberAdditive";

            base.ParseAsset(Shaders.UberPipelineAsset, false);
            vertexLayout.VertexStepMode = VertexStepMode.Instance;
            var uberAddPipeDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[]
                {
                    BlendState.AdditiveBlend,
                    BlendState.AdditiveBlend
                },
                DepthStencilState = new DepthStencilState()
                {
                    DepthTestEnabled = true,
                    DepthWriteEnabled = true,
                    DepthComparison = CompareFunction.LessEqual
                },
                PrimitiveState = new PrimitiveState()
                {
                    Topology = PrimitiveTopology.TriangleList,
                    PolygonMode = PolygonMode.Fill,
                    CullFace = CullFace.None,
                    FrontFace = FrontFace.Cw
                },
                VertexLayouts = new[] { vertexLayout },
                BindGroupLayouts = resourceLayouts.ToArray(),
                AttachmentDescription = new AttachmentDescription()
                {
                    DepthFormat = TextureFormat.Depth32Float,
                    ColorFormats = new[] { GraphicsManager.surfaceFormat, TextureFormat.Rgba8Unorm }
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(shaders[0], shaders[1], ref uberAddPipeDesc);
        }
    }
}
