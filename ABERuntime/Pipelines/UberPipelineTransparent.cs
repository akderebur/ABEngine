using System;
using System.Collections.Generic;
using System.Text;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime.Pipelines
{
    public class UberPipelineTransparent : PipelineAsset
    {
        public UberPipelineTransparent() : base()
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedSpriteNormalLayout);
            defaultMatName = "UberTransparent";

            base.ParseAsset(Shaders.UberPipelineAsset, false);

            renderOrder = Rendering.RenderOrder.Transparent;
            renderType = Rendering.RenderType.Transparent;

            vertexLayout.VertexStepMode = VertexStepMode.Instance;
            var uberTransPipeDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[]
                {
                    BlendState.AlphaBlend,
                    BlendState.AlphaBlend
                },
                DepthStencilState = new DepthStencilState()
                {
                    DepthTestEnabled = true,
                    DepthWriteEnabled = false,
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
                    DepthFormat = Game.resourceContext.mainDepthView.Format,
                    ColorFormats = new[] { Game.resourceContext.mainRenderView.Format, Game.resourceContext.spriteNormalsView.Format}
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(shaders[0], shaders[1], ref uberTransPipeDesc);
        }
    }
}
