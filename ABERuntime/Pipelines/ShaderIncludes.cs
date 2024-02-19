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

        layout (set = 0, binding = 2) buffer readonly SharedMeshVertex
        {
            mat4 matrices[];
        };

        layout (set = 1, binding = 0) uniform DrawData
        {
             int matrixStartID;
        };

        layout(location = 0) in vec3 position;
        layout(location = 1) in vec3 normal;
        layout(location = 2) in vec2 texCoord;
        layout(location = 3) in vec4 tangent;
        ";

        internal static string CalculateMeshCS = @"
        mat4 transformationMatrix = matrices[matrixStartID + gl_InstanceIndex * 2];
        mat4 normalMatrix = matrices[matrixStartID + gl_InstanceIndex * 2 + 1];
        gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);
        ";

    }
}

