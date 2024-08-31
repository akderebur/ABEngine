using System;
using System.Text;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime.Pipelines
{
	public class SkyboxPipeline : PipelineAsset
    {
        private static BindGroupLayout _skyboxLayout;
        public static BindGroupLayout SkyboxBindLayout
        {
            get
            {
                if(_skyboxLayout == null)
                    CreateSharedResources();
                return _skyboxLayout;
            }
        }

        private static void CreateSharedResources()
        {
            var skyLayoutDesc = new BindGroupLayoutDescriptor()
            {
                Entries = new[]
                {
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Buffer,
                        ShaderStages = ShaderStages.VERTEX
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Texture,
                        TextureSampleType = TextureSampleType.Cube,
                        ShaderStages = ShaderStages.FRAGMENT
                    },
                    new BindGroupLayoutEntry()
                    {
                        BindingType = BindingType.Sampler,
                        ShaderStages = ShaderStages.FRAGMENT
                    }
                }
            };

            _skyboxLayout = Game.wgil.CreateBindGroupLayout(ref skyLayoutDesc).SetManualDispose(true);
        }
            
        public SkyboxPipeline() : base()
        {
            defaultMatName = "Skybox";

            base.ParseAsset(SkyboxPipelineAsset, false);

            resourceLayouts.Clear();
            resourceLayouts.Add(Graphics.sharedPipelineLayout);
            resourceLayouts.Add(SkyboxBindLayout);
            
            var skyboxPipeDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[] { BlendState.OverrideBlend, BlendState.OverrideBlend,  },
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
                    CullFace = CullFace.Back,
                    FrontFace = FrontFace.Ccw
                },
                VertexLayouts = new[] { Graphics.sharedMeshVertexLayout },
                BindGroupLayouts = resourceLayouts.ToArray(),
                AttachmentDescription = new AttachmentDescription()
                {
                    DepthFormat = TextureFormat.Depth32Float,
                    ColorFormats = new[] { Game.resourceContext.mainRenderView.Format, Game.resourceContext.cameraNormalView.Format }
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(shaders[0], shaders[1], ref skyboxPipeDesc);
        }

        string SkyboxPipelineAsset = @"
SkyboxPipeline
{
}
Vertex
{
   #version 450
       
   layout (set = 0, binding = 0) uniform PipelineData
   {
       mat4 Projection;
       mat4 View;
       vec2 Resolution;
       float Time;
       float Padding;
   };

   layout (set = 1, binding = 0) uniform MeshTransform
   {
        mat4 Model;
   };

   layout(location = 0) in vec3 position;
   layout(location = 0) out vec3 outUV;

   void main()
   {
        outUV = position;
        gl_Position = Projection * View * Model * vec4(position, 1);
   }
}
Fragment
{
    #version 450

    layout(set = 1, binding = 1) uniform textureCube t_CubeMap;
    layout(set = 1, binding = 2) uniform sampler s_CubeMap;

    layout(location = 0) in vec3 v_Uv;
    layout(location = 0) out vec4 f_Color;
    layout(location = 1) out vec4 n_Color;

    void main() {
        f_Color = texture(samplerCube(t_CubeMap, s_CubeMap), v_Uv);
        //f_Color = vec4(1);
    }
}
"
;
    }
}

