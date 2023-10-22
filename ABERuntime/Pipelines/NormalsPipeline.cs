using System;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace ABEngine.ABERuntime.Pipelines
{
	public class NormalsPipeline : PipelineAsset
    {
        public NormalsPipeline() : base()
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
            shaderOptimised = false;
            defaultMatName = "NormalsPass";

            PipelineAsset.ParseAsset(NormalsPipelineAsset, this);

            GraphicsPipelineDescription toonLitDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[]
                    {
                      GraphicsManager.sharedMeshVertexLayout
                    },
                    shaders),
                resourceLayouts.ToArray(),
                Game.resourceContext.normalsRenderFB.OutputDescription);
            pipeline = rf.CreateGraphicsPipeline(ref toonLitDesc);
        }

        string NormalsPipelineAsset = @"
Properties
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

   layout(location = 0) in vec3 position;
   layout(location = 1) in vec3 vertexNormal;
   layout(location = 2) in vec2 texCoord;
   layout(location = 3) in vec3 tangent;

   layout(location = 0) out vec3 outNormal_VS;

   void main()
   {
       gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);

        mat3 normalMatrix = transpose(inverse(mat3(View * transformationMatrix)));

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

    layout (set = 1, binding = 0) uniform DummyVertex
    {
        mat4 dummyMatrix;
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
        dummy += (dummyMatrix[0][0] * vec3(1)).x - (dummyMatrix[0][0] * vec3(1)).x;
        dummy += PropPad.x - PropPad.x;
     
        outputColor = vec4(Normal_VS, 1.0);
    }
}
"
;
    }
}

