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
        BindGroupLayout texLayout;

        public LightPipelineAsset() : base()
        {

            // Light Pipeline
            var vertLayout = WGILUtils.GetVertexLayout<LightInfo>(out _);

            // Tex Layout
            var texLayoutDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
               {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Sampler,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Sampler,
                        ShaderStages = ShaderStages.FRAGMENT
                    }
                }
            };


            texLayout = Game.wgil.CreateBindGroupLayout(ref texLayoutDesc);

            var lightPipeDesc = new PipelineDescriptor()
            {
                VertexStepMode = VertexStepMode.Instance,
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
                BindGroupLayouts = new[] { GraphicsManager.sharedPipelineLayout, texLayout },
                VertexAttributes = vertLayout,
                AttachmentDescription = new AttachmentDescription()
                {
                    ColorFormats = new[] { TextureFormat.Rgba16Float }
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(Shaders.PointLightVertex2, Shaders.PointLightFragment2, ref lightPipeDesc);
        }

        public BindGroupLayout GetTexResourceLayout()
        {
            return texLayout;
        }
    }
}

