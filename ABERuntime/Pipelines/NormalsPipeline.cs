using System;
using System.Text;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime.Pipelines
{
	public class NormalsPipeline : PipelineAsset
    {
        public NormalsPipeline() : base()
        {
            defaultMatName = "NormalsPass";

            base.ParseAsset(NormalsPipelineAsset, false);

            resourceLayouts.Clear();

            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);

            var normalsPipeDesc = new PipelineDescriptor()
            {
                BlendStates = new BlendState[] { BlendState.AlphaBlend },
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
                    CullFace = CullFace.Back,
                    FrontFace = FrontFace.Cw
                },
                VertexLayouts = new[] { GraphicsManager.sharedMeshVertexLayout },
                BindGroupLayouts = resourceLayouts.ToArray(),
                AttachmentDescription = new AttachmentDescription()
                {
                    DepthFormat = TextureFormat.Depth32Float,
                    ColorFormats = new[] { Game.resourceContext.cameraNormalView.Format }
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(shaders[0], shaders[1], ref normalsPipeDesc);
        }

        string NormalsPipelineAsset = @"
NormalsPass
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

   //layout (set = 1, binding = 0) uniform SharedMeshVertex
   //{
   //    mat4 transformationMatrix;
   //    mat4 normalMatrix;
   //};

    layout (set = 1, binding = 0) buffer readonly SharedMeshVertex
    {
        mat4 transformationMatrix;
        mat4 normalMatrix;
    };

   layout(location = 0) in vec3 position;
   layout(location = 1) in vec3 vertexNormal;
   layout(location = 2) in vec2 texCoord;
   layout(location = 3) in vec3 tangent;

   layout(location = 0) out vec3 outNormal_VS;

   void main()
   {
       gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);

       outNormal_VS = normalize(mat3(normalMatrix) * vertexNormal);
   }
}
Fragment
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


    layout(location = 0) in vec3 Normal_VS;

    layout(location = 0) out vec4 outputColor;
   
    void main()
    {
        vec3 normal = normalize(Normal_VS);
        normal = (normal + 1) * 0.5;
        outputColor = vec4(normal, 1.0);
    }
}
"
;
    }
}

