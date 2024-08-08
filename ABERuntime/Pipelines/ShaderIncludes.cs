using System;
using System.Collections.Generic;
using System.Reflection;

namespace ABEngine.ABERuntime.Pipelines
{
	public static class ShaderIncludes
	{
        static Dictionary<string, string> includeMap = new Dictionary<string, string>();
        static ShaderIncludes()
        {
            Type type = typeof(ShaderIncludes);

            // Get all fields of the class
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                // Check if the field is of type string
                if (field.FieldType == typeof(string))
                {
                    string value = (string)field.GetValue(null);
                    includeMap.Add(field.Name, value);
                }
            }
        }

        public static string GetShaderInclude(string includeName)
        {
            if (includeMap.TryGetValue(includeName, out string include))
                return include;
            return "";
        }

        internal static string VertexInput3D = @"
        layout (set = 0, binding = 0) uniform PipelineData
        {
            mat4 Projection;
            mat4 View;
            vec2 Resolution;
            float Time;
            float Padding;
        };

        layout (set = 0, binding = 2) readonly buffer SharedMeshVertex
        {
            mat4 transformationMatrix;
            mat4 normalMatrix;
        } matrices[];

        layout (set = 1, binding = 0) uniform DrawData
        {
             int matrixStartID;
        };

        layout(location = 0) in vec3 position;
        layout(location = 1) in vec3 normal;
        layout(location = 2) in vec2 texCoord;
        layout(location = 3) in vec4 tangent;
        ";
        
        internal static string FragmentInput3D = @"
        layout (set = 0, binding = 0) uniform PipelineData
        {
            mat4 Projection;
            mat4 View;
            vec2 Resolution;
            float Time;
            float Padding;
        };

        // Lighting
        struct Light
        {
            vec3 Position;
            float Range;
            vec3 Color;
            float Intensity;
        };

        layout (set = 0, binding = 1) uniform SharedMeshFragment
        {
            Light Lights[4];
            vec3 camPos;
            float _padding_0;
            int NumDirectionalLights;
            int NumPointLights;
            float _padding_2;
            float _padding_3;
        };
        ";

        internal static string CalculateMeshCS = @"
        int index = matrixStartID + int(gl_InstanceIndex);
        mat4 transformationMatrix = matrices[index].transformationMatrix;
        mat4 normalMatrix = matrices[index].normalMatrix;
        gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);
        ";


        internal static string VertexInput2D = @"
        layout (set = 0, binding = 0) uniform PipelineData
        {
            mat4 Projection;
            mat4 View;
            vec2 Resolution;
            float Time;
            float Padding;
        };

        layout(location = 0) in vec3 Position;
        layout(location = 1) in vec2 Scale;
        layout(location = 2) in vec3 WorldScale;
        layout(location = 3) in vec4 Tint;
        layout(location = 4) in float ZRotation;
        layout(location = 5) in vec2 uvStart;
        layout(location = 6) in vec2 uvScale;
        layout(location = 7) in vec2 Pivot;

        
        vec2 rotate(vec2 v, float a)
        {
            float s = sin(a);
            float c = cos(a);
            mat2 m = mat2(c, -s, s, c);
            return m * v;
        }

        const vec4 Quads[6]= vec4[6](
        vec4(-0.5, -0.5, 0, 1),
        vec4(-0.5, 0.5, 0, 0),
        vec4(0.5, 0.5, 1, 0),
        vec4(-0.5, -0.5, 0, 1),
        vec4(0.5, 0.5, 1, 0),
        vec4(0.5, -0.5, 1, 1)
        );
        ";

        internal static string CalculateSpriteCS = @"
        vec4 unit_quad = Quads[gl_VertexIndex];
        vec2 unit_pos = unit_quad.xy;
        vec2 uv_pos = unit_quad.zw;

        vec2 pos = ((unit_pos + Pivot) * Scale.xy);
        pos *= WorldScale.xy;
        pos = rotate(pos, ZRotation);
        pos += Position.xy;

        gl_Position = Projection * View * vec4(pos, Position.z, 1);

        vec2 uv_sample = uv_pos * uvScale + uvStart;
        ";
    }
}

