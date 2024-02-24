using System;
namespace ABEngine.ABERuntime.Pipelines
{
	internal static class Shaders3D
	{
        internal const string UberPipeline3DAsset = @"
Uber3D
{
    @Pipeline:3D
    @Blend:Alpha
    @Depth:LessEqual

    AlbedoColor:vec4
    MetallicFactor:float
    RoughnessFactor:float
    AOFactor:float
    PropPad:float
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

   layout (set = 0, binding = 2) readonly buffer SharedMeshVertex
   {
        mat4 transformationMatrix;
        mat4 normalMatrix;
   } matrices[];


   layout (set = 0, binding = 3) readonly buffer BoneBuffer
   {
        mat4 boneMatrix;
   } boneMatrices[];


   layout (set = 1, binding = 0) uniform DrawData
   {
        int matrixStartID;
        int boneStartID;
        int meshBoneCount;
   };

    layout (set = 2, binding = 0) uniform ShaderProps
    {
        vec4 AlbedoColor;
        float MetallicFactor;
        float RoughnessFactor;
        float AOFactor;
        float PropPad;
    };

   layout(location = 0) in vec3 position;
   layout(location = 1) in vec3 normal;
   layout(location = 2) in vec2 texCoord;
   layout(location = 3) in vec4 tangent;

   #ifdef HAS_SKIN
   layout(location = 4) in ivec4 boneIDs;
   layout(location = 5) in vec4 boneWeights;
   #endif

   layout(location = 0) out vec2 pass_textureCoordinates;
   layout(location = 1) out vec3 pass_position;
   layout(location = 2) out vec3 pass_normalVector;
   //layout(location = 3) out mat3 tangentBasis;

   void main()
   {
       #ifdef HAS_SKIN
           int boneStart =0;
           vec4 accPos = vec4(0.0);
           vec4 accNormal = vec4(0.0);
   
           for(int i = 0; i < 4; i++) {
                int boneIndex = boneIDs[0];
                mat4 boneTransform = boneMatrices[boneIndex].boneMatrix;
                float weight =  boneWeights[i];

                vec4 posePosition = boneTransform * vec4(position, 1.0);
                accPos += posePosition * weight;

                vec4 poseNormal = boneTransform * vec4(normal, 1.0);
                accNormal += poseNormal * weight;
           }

           gl_Position = Projection * View * vec4(position, 1);
           pass_position = vec3(accPos);
           pass_normalVector = normalize(vec3(accNormal));
       #else
           int index = matrixStartID + int(gl_InstanceIndex);
           mat4 transformationMatrix = matrices[index].transformationMatrix;
           mat4 normalMatrix = matrices[index].normalMatrix;

           gl_Position = Projection * View * transformationMatrix * vec4(position,1.0);
           pass_position = vec3(transformationMatrix * vec4(position, 1));
           pass_normalVector = normalize(mat3(normalMatrix) * normal);
       #endif

       pass_textureCoordinates = texCoord;

       //vec3 N = normalize(vec3(transformationMatrix * vec4(vertexNormal,0)));
       //vec3 T = normalize(vec3(transformationMatrix * vec4(tangent,   0.0)));
       //vec3 B = normalize(cross(N,T));

       //tangentBasis = mat3(T,B,N);
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

    layout (set = 2, binding = 0) uniform ShaderProps
    {
        vec4 AlbedoColor;
        float MetallicFactor;
        float RoughnessFactor;
        float AOFactor;
        float PropPad;
    };
    
    layout (set = 3, binding = 0) uniform texture2D AlbedoTex;
    layout (set = 3, binding = 1) uniform sampler AlbedoSampler;

    layout(location = 0) in vec2 TexCoords;
    layout(location = 1) in vec3 WorldPos;
    layout(location = 2) in vec3 Normal;
    //layout(location = 3) in mat3 tangentBasis;

    layout(location = 0) out vec4 outputColor;

    const float M_PI = 3.141592653589793;
    const float c_MinReflectance = 0.04;

    struct AngularInfo
    {
        float NdotL;                  // cos angle between normal and light direction
        float NdotV;                  // cos angle between normal and view direction
        float NdotH;                  // cos angle between normal and half vector
        float LdotH;                  // cos angle between light direction and half vector

        float VdotH;                  // cos angle between view direction and half vector

        vec3 padding;
    };

    vec3 getNormal()
    {
       return normalize(Normal);
    }

    AngularInfo getAngularInfo(vec3 pointToLight, vec3 normal, vec3 view)
    {
        // Standard one-letter names
        vec3 n = normalize(normal);           // Outward direction of surface point
        vec3 v = normalize(view);             // Direction from surface point to view
        vec3 l = normalize(pointToLight);     // Direction from surface point to light
        vec3 h = normalize(l + v);            // Direction of the vector between l and v

        float NdotL = clamp(dot(n, l), 0.0, 1.0);
        float NdotV = clamp(dot(n, v), 0.0, 1.0);
        float NdotH = clamp(dot(n, h), 0.0, 1.0);
        float LdotH = clamp(dot(l, h), 0.0, 1.0);
        float VdotH = clamp(dot(v, h), 0.0, 1.0);

        return AngularInfo(
            NdotL,
            NdotV,
            NdotH,
            LdotH,
            VdotH,
            vec3(0, 0, 0)
        );
    }

    struct MaterialInfo
    {
        float perceptualRoughness;    // roughness value, as authored by the model creator (input to shader)
        vec3 reflectance0;            // full reflectance color (normal incidence angle)

        float alphaRoughness;         // roughness mapped to a more linear change in the roughness (proposed by [2])
        vec3 diffuseColor;            // color contribution from diffuse lighting

        vec3 reflectance90;           // reflectance color at grazing angle
        vec3 specularColor;           // color contribution from specular lighting
    };

    // Lambert lighting
    // see https://seblagarde.wordpress.com/2012/01/08/pi-or-not-to-pi-in-game-lighting-equation/
    vec3 diffuse(MaterialInfo materialInfo)
    {
        return materialInfo.diffuseColor / M_PI;
    }

    // The following equation models the Fresnel reflectance term of the spec equation (aka F())
    // Implementation of fresnel from [4], Equation 15
    vec3 specularReflection(MaterialInfo materialInfo, AngularInfo angularInfo)
    {
        return materialInfo.reflectance0 + (materialInfo.reflectance90 - materialInfo.reflectance0) * pow(clamp(1.0 - angularInfo.VdotH, 0.0, 1.0), 5.0);
    }

    // Smith Joint GGX
    // Note: Vis = G / (4 * NdotL * NdotV)
    // see Eric Heitz. 2014. Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs. Journal of Computer Graphics Techniques, 3
    // see Real-Time Rendering. Page 331 to 336.
    // see https://google.github.io/filament/Filament.md.html#materialsystem/specularbrdf/geometricshadowing(specularg)
    float visibilityOcclusion(MaterialInfo materialInfo, AngularInfo angularInfo)
    {
        float NdotL = angularInfo.NdotL;
        float NdotV = angularInfo.NdotV;
        float alphaRoughnessSq = materialInfo.alphaRoughness * materialInfo.alphaRoughness;

        float GGXV = NdotL * sqrt(NdotV * NdotV * (1.0 - alphaRoughnessSq) + alphaRoughnessSq);
        float GGXL = NdotV * sqrt(NdotL * NdotL * (1.0 - alphaRoughnessSq) + alphaRoughnessSq);

        float GGX = GGXV + GGXL;
        if (GGX > 0.0)
        {
            return 0.5 / GGX;
        }
        return 0.0;
    }

    // The following equation(s) model the distribution of microfacet normals across the area being drawn (aka D())
    // Implementation from ""Average Irregularity Representation of a Roughened Surface for Ray Reflection"" by T. S. Trowbridge, and K. P. Reitz
    // Follows the distribution function recommended in the SIGGRAPH 2013 course notes from EPIC Games [1], Equation 3.
    float microfacetDistribution(MaterialInfo materialInfo, AngularInfo angularInfo)
    {
        float alphaRoughnessSq = materialInfo.alphaRoughness * materialInfo.alphaRoughness;
        float f = (angularInfo.NdotH * alphaRoughnessSq - angularInfo.NdotH) * angularInfo.NdotH + 1.0;
        return alphaRoughnessSq / (M_PI * f * f);
    }

    vec3 getPointShade(vec3 pointToLight, MaterialInfo materialInfo, vec3 normal, vec3 view)
    {
        AngularInfo angularInfo = getAngularInfo(pointToLight, normal, view);

        if (angularInfo.NdotL > 0.0 || angularInfo.NdotV > 0.0)
        {
            // Calculate the shading terms for the microfacet specular shading model
            vec3 F = specularReflection(materialInfo, angularInfo);
            float Vis = visibilityOcclusion(materialInfo, angularInfo);
            float D = microfacetDistribution(materialInfo, angularInfo);

            // Calculation of analytical lighting contribution
            vec3 diffuseContrib = (1.0 - F) * diffuse(materialInfo);
            vec3 specContrib = F * Vis * D;

            // Obtain final intensity as reflectance (BRDF) scaled by the energy of the light (cosine law)
            return angularInfo.NdotL * (diffuseContrib + specContrib);
        }

        return vec3(0.0, 0.0, 0.0);
    }

    // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_lights_punctual/README.md#range-property
    float getRangeAttenuation(float range, float distance)
    {
        if (range < 0.0)
        {
            // negative range means unlimited
            return 1.0;
        }
        return max(min(1.0 - pow(distance / range, 4.0), 1.0), 0.0) / pow(distance, 2.0);
    }

    // https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Khronos/KHR_lights_punctual/README.md#inner-and-outer-cone-angles
    float getSpotAttenuation(vec3 pointToLight, vec3 spotDirection, float outerConeCos, float innerConeCos)
    {
        float actualCos = dot(normalize(spotDirection), normalize(-pointToLight));
        if (actualCos > outerConeCos)
        {
            if (actualCos < innerConeCos)
            {
                return smoothstep(outerConeCos, innerConeCos, actualCos);
            }
            return 1.0;
        }
        return 0.0;
    }

    vec3 applyDirectionalLight(Light light, MaterialInfo materialInfo, vec3 normal, vec3 view)
    {
        vec3 pointToLight = -light.Position;
        vec3 shade = getPointShade(pointToLight, materialInfo, normal, view);
        return light.Intensity * 10 * light.Color * shade;
    }
   
    void main()
    {
        float dummy = Time - Time;
        dummy += MetallicFactor - MetallicFactor;
        vec3 albedo = vec3(texture(sampler2D(AlbedoTex, AlbedoSampler), TexCoords));

        vec4 baseColor = vec4(albedo, 1.0) * AlbedoColor;
        float perceptualRoughness = RoughnessFactor;
        float metallic = MetallicFactor;
        vec3 diffuseColor = vec3(0.0);
        vec3 specularColor= vec3(0.0);
        vec3 f0 = vec3(0.04);

        diffuseColor = baseColor.rgb * (vec3(1.0) - f0) * (1.0 - metallic);
        specularColor = mix(f0, baseColor.rgb, metallic);
        baseColor.a = 1.0;

        perceptualRoughness = clamp(perceptualRoughness, 0.0, 1.0);
        metallic = clamp(metallic, 0.0, 1.0);

        // Roughness is authored as perceptual roughness; as is convention,
        // convert to material roughness by squaring the perceptual roughness [2].
        float alphaRoughness = perceptualRoughness * perceptualRoughness;

        // Compute reflectance.
        float reflectance = max(max(specularColor.r, specularColor.g), specularColor.b);

        vec3 specularEnvironmentR0 = specularColor.rgb;
        // Anything less than 2% is physically impossible and is instead considered to be shadowing. Compare to ""Real-Time-Rendering"" 4th editon on page 325.
        vec3 specularEnvironmentR90 = vec3(clamp(reflectance * 50.0, 0.0, 1.0));

        MaterialInfo materialInfo = MaterialInfo(
            perceptualRoughness,
            specularEnvironmentR0,
            alphaRoughness,
            diffuseColor,
            specularEnvironmentR90,
            specularColor
        );

        // LIGHTING

        vec3 color = vec3(0.0, 0.0, 0.0);
        vec3 normal = getNormal();
        vec3 view = normalize(camPos - WorldPos);

        for (int i = 0; i < NumDirectionalLights; ++i)
        {
            Light light = Lights[i];
            color += applyDirectionalLight(light, materialInfo, normal, view);
        }
        

        outputColor = vec4(color, 1.0 );
        //outputColor = vec4(normal * 0.5 + 0.5, 1.0);
        //outputColor = vec4(vec3(1), 1.0 );
    }
}
"
;

        internal const string ToonWaterAsset = @"
Properties
{
	PropPad:vec4
    DepthTex:texture2d
    CamNormalTex:texture2d
    DistNoiseTex:texture2d
    SurfNoiseTex:texture2d
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
   layout(location = 1) out vec3 pass_position;
   layout(location = 2) out vec3 pass_normalVector;
   layout(location = 3) out vec4 clipPos;

   void main()
   {
       clipPos = Projection * View * transformationMatrix * vec4(position,1.0);
       pass_textureCoordinates = texCoord;
       pass_position = vec3( transformationMatrix * vec4(position,1));

       mat3 normalMatrix = transpose(inverse(mat3(View * transformationMatrix)));

       pass_normalVector = normalize(normalMatrix * vertexNormal);
       gl_Position = clipPos;
   
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

    layout (set = 3, binding = 0) uniform texture2D DepthTex;
    layout (set = 3, binding = 1) uniform samplerShadow DepthTexSampler;
    layout (set = 3, binding = 2) uniform texture2D CamNormalTex;
    layout (set = 3, binding = 3) uniform sampler CamNormalTexSampler;
    layout (set = 3, binding = 4) uniform texture2D DistNoiseTex;
    layout (set = 3, binding = 5) uniform sampler DistNoiseSampler;
    layout (set = 3, binding = 6) uniform texture2D SurfNoiseTex;
    layout (set = 3, binding = 7) uniform sampler SurfNoiseSampler;


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

    layout(location = 0) in vec2 TexCoords;
    layout(location = 1) in vec3 WorldPos;
    layout(location = 2) in vec3 viewNormal;
    layout(location = 3) in vec4 clipPos;

    layout(location = 0) out vec4 outputColor;

    const float SMOOTHSTEP_AA = 0.01;

    float foam_distance = 0.01;
    float foam_max_distance = 0.07;
    float foam_min_distance = 0.04;
    vec4 foam_color = vec4(1.0);

    vec2 surface_noise_tiling = vec2(1.0, 6.0);
    vec3 surface_noise_scroll = vec3(0.03, 0.03, 0.0);
    float surface_noise_cutoff = 0.777;
    float surface_distortion_amount = 0.27;

    vec4 _DepthGradientShallow = vec4(0.325, 0.807, 0.971, 0.725);
    vec4 _DepthGradientDeep = vec4(0.086, 0.407, 1, 0.749);
    float _DepthMaxDistance = 1.0;
    float _DepthFactor = 1.0;
    
    vec4 alphaBlend(vec4 top, vec4 bottom)
    {
	    vec3 color = (top.rgb * top.a) + (bottom.rgb * (1.0 - top.a));
	    float alpha = top.a + bottom.a * (1.0 - top.a);
	
	    return vec4(color, alpha);
    }

    float rand(vec2 coord) {
	    return fract(sin(dot(coord, vec2(12.9898, 78.233))) * 43758.5453);
    }


    float perlin_noise(vec2 coord) {
	        vec2 i = floor(coord);
	        vec2 f = fract(coord);
	
	        float t_l = rand(i) * 6.283;
	        float t_r = rand(i + vec2(1, 0)) * 6.283;
	        float b_l = rand(i + vec2(0, 1)) * 6.283;
	        float b_r = rand(i + vec2(1)) * 6.283;
	
	        vec2 t_l_vec = vec2(-sin(t_l), cos(t_l));
	        vec2 t_r_vec = vec2(-sin(t_r), cos(t_r));
	        vec2 b_l_vec = vec2(-sin(b_l), cos(b_l));
	        vec2 b_r_vec = vec2(-sin(b_r), cos(b_r));
	
	        float t_l_dot = dot(t_l_vec, f);
	        float t_r_dot = dot(t_r_vec, f - vec2(1, 0));
	        float b_l_dot = dot(b_l_vec, f - vec2(0, 1));
	        float b_r_dot = dot(b_r_vec, f - vec2(1));
	
	        vec2 cubic = f * f * (3.0 - 2.0 * f);
	
	        float top_mix = mix(t_l_dot, t_r_dot, cubic.x);
	        float bot_mix = mix(b_l_dot, b_r_dot, cubic.x);
	        float whole_mix = mix(top_mix, bot_mix, cubic.y);
	
	        return whole_mix + 0.5;
        }

    float LinearEyeDepth( float z )
    {
        float far = 1000;
        float near = 0.1;
        float paramZ = (1-far/near)/far;
        float paramW = far/near/far;
        return 1.0 / (paramZ * z + paramW);
    }

    float LinearizeDepth(float depth) 
    {
        float far = 1000;
        float near = 0.1;
        float z = depth * 2.0 - 1.0; // back to NDC
        return (2.0 * near * far) / (far + near - z * (far - near));	
    }


    float beer_factor = 0.8;

   
    void main()
    {
        //float dummy = Time - Time;
        //dummy += (dummyMatrix[0][0] * vec3(1)).x - (dummyMatrix[0][0] * vec3(1)).x;
        //dummy += PropPad.x - PropPad.x;
        //dummy += camPos.x - camPos.x;

        vec2 noiseUV = (TexCoords) * surface_noise_tiling;
        vec2 distortUV = TexCoords;

        //float depthVal =  texture(sampler2D(DepthTex, DepthTexSampler), gl_FragCoord.xy / Resolution).r;
        //float depth = Projection[3][2] / (depthVal + Projection[2][2]);
	    //depth = depth + WorldPos.z;
	    //depth = exp(-depth * beer_factor);
	    //depth = 1.0 - depth;

        //vec3 projCoord = clipPos.xyz / clipPos.w;
        float existingDepth01 =  texture(sampler2DShadow(DepthTex, DepthTexSampler), vec3(gl_FragCoord.xy/Resolution, 1)).r;
        float existingDepthLinear = LinearizeDepth(existingDepth01);

        float depth =  existingDepthLinear - clipPos.w;

        //depth = 1.0 - depth;
        //depth = 1.0 - perlin_noise(TexCoords * 7);

		vec3 existingNormal = texture(sampler2D(CamNormalTex, CamNormalTexSampler), gl_FragCoord.xy/Resolution).xyz;
        //existingNormal = vec3(dFdx(depth), dFdy(depth), 0);

	    float normalDot = clamp(dot(existingNormal, viewNormal), 0.0, 1.0);
	    float foamDistance = mix(foam_max_distance, foam_min_distance, normalDot);
	
	    float foamDepth = clamp(depth / foamDistance, 0.0, 1.0);
	    float surfaceNoiseCutoff = foamDepth * surface_noise_cutoff;
	
	    vec4 distortNoiseSample = texture(sampler2D(DistNoiseTex, DistNoiseSampler), distortUV);
	    vec2 distortAmount = (distortNoiseSample.xy * 2.0 -1.0) * surface_distortion_amount;
        
	    vec2 noise_uv = vec2(
		    (noiseUV.x + Time * surface_noise_scroll.x) + distortAmount.x , 
		    (noiseUV.y + Time * surface_noise_scroll.y + distortAmount.y)
	    );
	    float surfaceNoiseSample = texture(sampler2D(SurfNoiseTex, SurfNoiseSampler), noise_uv).r;
	    float surfaceNoiseAmount = smoothstep(surfaceNoiseCutoff - SMOOTHSTEP_AA, surfaceNoiseCutoff + SMOOTHSTEP_AA, surfaceNoiseSample);
	
	    float waterDepth = clamp(depth / _DepthMaxDistance, 0.0, 1.0) * _DepthFactor;
	    vec4 waterColor = mix(_DepthGradientShallow, _DepthGradientDeep, waterDepth);

	    vec4 surfaceNoiseColor = foam_color;
        surfaceNoiseColor.a *= surfaceNoiseAmount;
	    vec4 color = alphaBlend(surfaceNoiseColor, waterColor);

        outputColor = color;
        //outputColor = vec4(vec3(1) * existingDepthLinear, 1);
    }
}
"
;
    }
}

