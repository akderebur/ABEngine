using System;
namespace ABEngine.ABERuntime.Pipelines
{
	internal static class Shaders3D
	{
        // PBR
        // Adapted from https://github.com/iweinbau/PBR-shader/
        internal const string PBRVertex = @"
		#version 450
       
        layout (set = 0, binding = 0) uniform PipelineData3D
        {
            mat4 projectionMatrix;
            mat4 viewMatrix;
            mat4 transformationMatrix;
            vec2 Resolution;
            float Time;
            float Padding;
        };

        layout(location = 0) in vec3 position;
        layout(location = 1) in vec2 texCoord;
        layout(location = 2) in vec3 vertexNormal;
        layout(location = 3) in vec3 tangent;

        layout(location = 0) out vec2 pass_textureCoordinates;
        layout(location = 1) out vec3 pass_normalVector;
        layout(location = 2) out vec3 pass_position;
        layout(location = 3) out mat3 TBNMatrix;
        layout(location = 4) out mat4 view_matrix;

        void main()
        {
            gl_Position = projectionMatrix * viewMatrix * transformationMatrix * vec4(position,1.0);
            pass_textureCoordinates = texCoord;
            pass_normalVector = mat3( transformationMatrix) * vertexNormal;
            pass_position = vec3( transformationMatrix * vec4(position,1));

            vec3 N = normalize(vec3(transformationMatrix * vec4(vertexNormal,0)));
            vec3 T = normalize(vec3(transformationMatrix * vec4(tangent,   0.0)));
            vec3 B = normalize(cross(N,T));

            TBNMatrix = mat3(T,B,N);
            view_matrix = viewMatrix;
        }
		";

        // PBR - Learn OpenGL
        // Adapted from https://learnopengl.com/PBR/Lighting
        internal const string PBROGLVertex = @"
		#version 450
       
        layout (set = 0, binding = 0) uniform PipelineData3D
        {
            mat4 projectionMatrix;
            mat4 viewMatrix;
            mat4 transformationMatrix;
            vec2 Resolution;
            float Time;
            float Padding;
        };

        layout(location = 0) in vec3 position;
        layout(location = 1) in vec3 vertexNormal;
        layout(location = 1) in vec2 texCoord;
        layout(location = 3) in vec3 tangent;

        layout(location = 0) out vec2 pass_textureCoordinates;
        layout(location = 1) out vec3 pass_normalVector;
        layout(location = 2) out vec3 pass_position;
        layout(location = 3) out mat3 TBNMatrix;
        layout(location = 4) out mat4 view_matrix;

        void main()
        {
            gl_Position = projectionMatrix * viewMatrix * transformationMatrix * vec4(position,1.0);
            pass_textureCoordinates = texCoord;
            pass_normalVector = mat3( transformationMatrix) * vertexNormal;
            pass_position = vec3( transformationMatrix * vec4(position,1));

            vec3 N = normalize(vec3(transformationMatrix * vec4(vertexNormal,0)));
            vec3 T = normalize(vec3(transformationMatrix * vec4(tangent,   0.0)));
            vec3 B = normalize(cross(N,T));

            TBNMatrix = mat3(T,B,N);
            view_matrix = viewMatrix;
        }
		";
    }
}

