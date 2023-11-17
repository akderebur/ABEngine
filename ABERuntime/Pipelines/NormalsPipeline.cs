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
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
            defaultMatName = "NormalsPass";

            base.ParseAsset(NormalsPipelineAsset, false);

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
                VertexAttributes = GraphicsManager.sharedMeshVertexLayout,
                BindGroupLayouts = resourceLayouts.ToArray(),
                AttachmentDescription = new AttachmentDescription()
                {
                    DepthFormat = TextureFormat.Depth32Float,
                    ColorFormats = new[] { TextureFormat.Rgba8Unorm }
                }
            };

            pipeline = Game.wgil.CreateRenderPipeline(shaders[0], shaders[1], ref normalsPipeDesc);
        }

        string NormalsPipelineAsset = @"
NormalsPass
{
	PropPad:vec4
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

   layout (set = 1, binding = 0) uniform SharedMeshVertex
   {
       mat4 transformationMatrix;
   };

   layout (set = 2, binding = 0) uniform ShaderProps
   {
    vec4 PropPad;
   };


   layout(location = 0) in vec3 position;
   layout(location = 1) in vec3 vertexNormal;
   layout(location = 2) in vec2 texCoord;
   layout(location = 3) in vec3 tangent;

   layout(location = 0) out vec3 outNormal_VS;

   void main()
   {
       gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);

        //mat3 normalMatrix = transpose(inverse(mat3(View * transformationMatrix)));
       mat3 normalMatrix = transpose(mat3(View * transformationMatrix));

       outNormal_VS = normalize(normalMatrix * vertexNormal);
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

    layout (set = 2, binding = 0) uniform ShaderProps
    {
        vec4 PropPad;
    };

    layout(location = 0) in vec3 Normal_VS;

    layout(location = 0) out vec4 outputColor;
   
    void main()
    {
        float dummy = Time - Time;
        dummy += PropPad.x - PropPad.x;
     
        outputColor = vec4(Normal_VS, 1.0);
    }
}
"
;
    }
}

