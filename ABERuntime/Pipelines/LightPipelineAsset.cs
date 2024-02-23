using System;
using System.Numerics;
using System.Text;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime.Pipelines
{
    public class LightPipelineAsset : PipelineAsset
    {
        public LightPipelineAsset() : base()
        {

            // Light Pipeline
            var vertLayout = WGILUtils.GetVertexLayout<LightInfo>(VertexStepMode.Instance);

           
            var lightPipeDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[]
                {
                    new BlendState()
                    {
                        color = new BlendComponent() { SrcFactor = BlendFactor.One, DstFactor = BlendFactor.One, Operation = BlendOperation.Add },
                        alpha = new BlendComponent() { SrcFactor = BlendFactor.One, DstFactor = BlendFactor.Zero, Operation = BlendOperation.Add }
                    }
                },
                PrimitiveState = new PrimitiveState()
                {
                    Topology = PrimitiveTopology.TriangleList,
                    PolygonMode = PolygonMode.Fill,
                    CullFace = CullFace.Back,
                    FrontFace = FrontFace.Cw
                },
                BindGroupLayouts = new[] { GraphicsManager.sharedPipelineLayout, GraphicsManager.sharedLightTexLayout },
                VertexLayouts = new[] { vertLayout },
                AttachmentDescription = new AttachmentDescription()
                {
                    ColorFormats = new[] { TextureFormat.Rgba16Float }
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(Shaders.PointLightVertex2, Shaders.PointLightFragment2, ref lightPipeDesc);
        }
    }
}

