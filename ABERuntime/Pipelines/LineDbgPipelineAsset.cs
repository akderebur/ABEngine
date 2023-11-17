using System;
using System.Text;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime.Pipelines
{
	public class LineDbgPipelineAsset : PipelineAsset
	{
        public LineDbgPipelineAsset() : base()
        {
            // Line Pipeline
            var linePipelineDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[] { BlendState.OverrideBlend },
                PrimitiveState = new PrimitiveState()
                {
                    Topology = PrimitiveTopology.LineStrip,
                    PolygonMode = PolygonMode.Fill,
                    FrontFace = FrontFace.Cw,
                    CullFace = CullFace.None
                },
                VertexAttributes = new VertexAttribute[]
                {
                    new VertexAttribute() { format = VertexFormat.Float32x4, location = 0, offset = 0 },
                    new VertexAttribute() { format = VertexFormat.Float32x3, location = 1, offset = 16 }
                },
                BindGroupLayouts = new BindGroupLayout[] { GraphicsManager.sharedPipelineLayout },
            };

            pipeline = Game.wgil.CreateRenderPipeline(Shaders.LineDebugVertex, Shaders.LineDebugFragment, ref linePipelineDesc);
        }
    }
}

