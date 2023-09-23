using System;
using Veldrid;

namespace ABEngine.ABERuntime.Pipelines
{
    public class ToonLitPipeline : PipelineAsset
    {
        public ToonLitPipeline() : base(null, false, false, Game.mainRenderSystem.GetMainFramebuffer)
        {
            resourceLayouts.Add(GraphicsManager.sharedPipelineLayout);
            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_VS);
            shaderOptimised = false;
            defaultMatName = "ToonLit";

            PipelineAsset.ParseAsset(ToonLitPipelineAsset, this);

            resourceLayouts.Add(GraphicsManager.sharedMeshUniform_FS);

            GraphicsPipelineDescription toonLitDesc = new GraphicsPipelineDescription(
                new BlendStateDescription(RgbaFloat.Black, BlendAttachmentDescription.AlphaBlend, BlendAttachmentDescription.AlphaBlend),
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
                base.framebuffer.OutputDescription);
            pipeline = rf.CreateGraphicsPipeline(ref toonLitDesc);
        }

        string ToonLitPipelineAsset = @"
Properties
{
	PropPad:vec4
    AlbedoTex:texture2d
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

   layout(location = 0) out vec2 outUV;
   layout(location = 1) out vec3 outNormal_WS;

   void main()
   {
       gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);
       outUV = texCoord;
       outNormal_WS = normalize(mat3(transformationMatrix) * vertexNormal);
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

    layout (set = 3, binding = 0) uniform texture2D AlbedoTex;
    layout (set = 3, binding = 1) uniform sampler AlbedoTexSampler;

    // Lighting
    struct Light
    {
        vec3 Position;
        float Range;
        vec3 Color;
        float Intensity;
    };

    layout (set = 4, binding = 0) uniform SharedMeshFragment
    {
        Light Lights[4];
        vec3 camPos;
        float _padding_0;
        int NumDirectionalLights;
        int NumPointLights;
        float _padding_2;
        float _padding_3;
    };

    layout(location = 0) in vec2 UV;
    layout(location = 1) in vec3 Normal_WS;

    layout(location = 0) out vec4 outputColor;
   
    void main()
    {
        float dummy = Time - Time;
        dummy += (dummyMatrix[0][0] * vec3(1)).x - (dummyMatrix[0][0] * vec3(1)).x;
        dummy += PropPad.x - PropPad.x;
        dummy += camPos.x - camPos.x;

        Light dirLight = Lights[0];
        float NdotL = dot(Normal_WS, -dirLight.Position);

        vec4 light = clamp(floor(NdotL * 3) / (2 - 0.5), 0.0, 1.0) * vec4(dirLight.Color * dirLight.Intensity, 1);

        vec4 col = texture(sampler2D(AlbedoTex, AlbedoTexSampler), UV);
        outputColor = (col) * (light + vec4(vec3(1) * 0.5, 1));
    }
}
"
;
    }
}

