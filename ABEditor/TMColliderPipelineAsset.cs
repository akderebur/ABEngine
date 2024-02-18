using System;
using System.Text;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Debug;
using ABEngine.ABERuntime.Pipelines;
using WGIL;

namespace ABEditor.Debug
{
    public class TMColliderPipelineAsset : PipelineAsset
    {
        public TMColliderPipelineAsset() : base()
        {
            // Line Pipeline
            var lineVertLayout = WGILUtils.GetVertexLayout<LinePoint>(VertexStepMode.Vertex, out _);

            var linePipeDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[]
                {
                    BlendState.OverrideBlend
                },
                //DepthStencilState = new DepthStencilState()
                //{
                //    DepthTestEnabled = true,
                //    DepthWriteEnabled = false,
                //    DepthComparison = CompareFunction.LessEqual
                //},
                PrimitiveState = new PrimitiveState()
                {
                    Topology = PrimitiveTopology.LineList,
                    PolygonMode = PolygonMode.Fill,
                    CullFace = CullFace.None,
                    FrontFace = FrontFace.Cw
                },
                VertexLayouts = new[] { lineVertLayout },
                BindGroupLayouts = new[] { GraphicsManager.sharedPipelineLayout },
                AttachmentDescription = new AttachmentDescription()
                {
                    ColorFormats = new[] { GraphicsManager.surfaceFormat }
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(Shaders.LineDebugVertex, Shaders.LineDebugFragment, ref linePipeDesc);
        }
    }
}

