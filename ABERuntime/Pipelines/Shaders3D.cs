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

        internal const string UberPipeline3DAsset = @"
Properties
{
	albedo:vec4
    metallic:float
    roughness:float
    ao:float
    propPad:float
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

   layout(location = 0) out vec2 pass_textureCoordinates;
   layout(location = 1) out vec3 pass_normalVector;
   layout(location = 2) out vec3 pass_position;
   //layout(location = 3) out mat3 TBNMatrix;
   //layout(location = 4) out mat4 view_matrix;

   void main()
   {
       gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);
       pass_textureCoordinates = texCoord;
       pass_position = vec3( transformationMatrix * vec4(position,1));
       pass_normalVector = mat3(transformationMatrix) * vertexNormal;

       //pass_textureCoordinates = texCoord;
       //pass_normalVector = mat3( transformationMatrix) * vertexNormal;
       //pass_position = vec3( transformationMatrix * vec4(position,1));

       //vec3 N = normalize(vec3(transformationMatrix * vec4(vertexNormal,0)));
       //vec3 T = normalize(vec3(transformationMatrix * vec4(tangent,   0.0)));
       //vec3 B = normalize(cross(N,T));

       //TBNMatrix = mat3(T,B,N);
       //view_matrix = viewMatrix;
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
        vec4 albedo;
        float metallic;
        float roughness;
        float ao;
        float propPad;
    };
    
    layout (set = 3, binding = 0) uniform texture2D AlbedoTex;
    layout (set = 3, binding = 1) uniform sampler AlbedoSampler;

    // Lighting
    struct PointLightInfo
    {
        vec3 Position;
        float _padding0;
        vec3 Color;
        float _padding1;
    };

    layout (set = 4, binding = 0) uniform SharedMeshFragment
    {
        PointLightInfo PointLights[4];
        vec3 camPos;
        float _padding_0;
        int NumActiveLights;
        float _padding_1;
        float _padding_2;
        float _padding_3;
    };

    layout(location = 0) in vec2 TexCoords;
    layout(location = 1) in vec3 WorldPos;
    layout(location = 2) in vec3 Normal;

    layout(location = 0) out vec4 outputColor;

    const float PI = 3.14159265359;
    // ----------------------------------------------------------------------------
    float DistributionGGX(vec3 N, vec3 H, float roughness)
    {
        float a = roughness*roughness;
        float a2 = a*a;
        float NdotH = max(dot(N, H), 0.0);
        float NdotH2 = NdotH*NdotH;

        float nom   = a2;
        float denom = (NdotH2 * (a2 - 1.0) + 1.0);
        denom = PI * denom * denom;

        return nom / denom;
    }
    // ----------------------------------------------------------------------------
    float GeometrySchlickGGX(float NdotV, float roughness)
    {
        float r = (roughness + 1.0);
        float k = (r*r) / 8.0;

        float nom   = NdotV;
        float denom = NdotV * (1.0 - k) + k;

        return nom / denom;
    }
    // ----------------------------------------------------------------------------
    float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
    {
        float NdotV = max(dot(N, V), 0.0);
        float NdotL = max(dot(N, L), 0.0);
        float ggx2 = GeometrySchlickGGX(NdotV, roughness);
        float ggx1 = GeometrySchlickGGX(NdotL, roughness);

        return ggx1 * ggx2;
    }
    // ----------------------------------------------------------------------------
    vec3 fresnelSchlick(float cosTheta, vec3 F0)
    {
        return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
    }
    // ----------------------------------------------------------------------------
   
    void main()
    {
        float dummy = Time - Time;
        dummy += (dummyMatrix[0][0] * vec3(1)).x - (dummyMatrix[0][0] * vec3(1)).x;
        dummy += metallic - metallic;
        vec3 dummySample = vec3(texture(sampler2D(AlbedoTex, AlbedoSampler), TexCoords));

        //vec3 camPos = vec3(camPosT);
        vec3 N = normalize(Normal);
        vec3 V = normalize(camPos - WorldPos);

        float metallicTest = 0.3;
        float roughnessTest = 0.2;
        float aoTest = 1;

        // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0 
        // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow)    
        vec3 F0 = vec3(0.04);
        vec3 albedoV3 = dummySample;
        F0 = mix(F0, albedoV3, metallicTest);

        // reflectance equation
        vec3 Lo = vec3(0.0);
        for(int i = 0; i < NumActiveLights; ++i) 
        {
            // calculate per-light radiance
            vec3 lightPos = PointLights[i].Position;
            //vec3 lightPos = vec3(lightPosT);

            vec3 L = normalize(lightPos - WorldPos);
            vec3 H = normalize(V + L);
            float distance = length(lightPos - WorldPos);
            float attenuation = 1.0 / (distance * distance);
            vec3 lightColor = PointLights[i].Color;
            //vec3 lightColor = vec3(lightColorT);

            vec3 radiance = lightColor * attenuation;

            // Cook-Torrance BRDF
            float NDF = DistributionGGX(N, H, roughnessTest);   
            float G   = GeometrySmith(N, V, L, roughnessTest);      
            vec3 F    = fresnelSchlick(clamp(dot(H, V), 0.0, 1.0), F0);
           
            vec3 numerator    = NDF * G * F; 
            float denominator = 4.0 * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001; // + 0.0001 to prevent divide by zero
            vec3 specular = numerator / denominator;
        
            // kS is equal to Fresnel
            vec3 kS = F;
            // for energy conservation, the diffuse and specular light can't
            // be above 1.0 (unless the surface emits light); to preserve this
            // relationship the diffuse component (kD) should equal 1.0 - kS.
            vec3 kD = vec3(1.0) - kS;
            // multiply kD by the inverse metalness such that only non-metals 
            // have diffuse lighting, or a linear blend if partly metal (pure metals
            // have no diffuse light).
            kD *= 1.0 - metallicTest;	  

            // scale light by NdotL
            float NdotL = max(dot(N, L), 0.0) + dummy;        

            // add to outgoing radiance Lo
            Lo += (kD * albedoV3 / PI + specular) * radiance * NdotL;  // note that we already multiplied the BRDF by the Fresnel (kS) so we won't multiply by kS again
        }   
    
        // ambient lighting (note that the next IBL tutorial will replace 
        // this ambient lighting with environment lighting).
        vec3 ambient = vec3(0.03) * albedoV3 * aoTest;

        vec3 color = ambient + Lo;

        // HDR tonemapping
        color = color / (color + vec3(1.0));
        // gamma correct
        color = pow(color, vec3(1.0/2.2)); 

        //color = vec3(texture(sampler2D(AlbedoTex, AlbedoSampler), TexCoords));
        //color = mix(color, lightColorT ,1);
        outputColor = vec4(color, 1.0);
    }
}
"
;
    }
}

