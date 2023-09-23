using System;
namespace ABEngine.ABERuntime.Pipelines
{
    internal static class Shaders
    {
        internal const string WaterPipelineAsset = @"
Properties
{
	Padding2:vec4
    NoiseTex:texture2d
    ScreenTex:texture2d
}
Vertex
{
    #version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Scale;
    layout(location = 2) in vec4 Tint;
    layout(location = 3) in float ZRotation;
    layout(location = 4) in vec2 uvStart;
    layout(location = 5) in vec2 uvScale;

    layout(location = 0) out vec2 fsin_TexCoords;
    layout(location = 1) out vec4 fsin_Tint;
    layout(location = 2) out vec2 fsin_UnitUV;
    layout(location = 3) out vec2 fsin_UVScale;

    //layout(constant_id = 0) const bool InvertedY = false;


    //   B____C
    //   |   /| 
    //   |  / |
    //   | /  |
    //   |/___|
    //  A     D

      const vec4 Quads[6]= vec4[6](
        vec4(-0.5, -0.5, 0, 1),
        vec4(-0.5, 0.5, 0, 0),
        vec4(0.5, 0.5, 1, 0),
        vec4(-0.5, -0.5, 0, 1),
        vec4(0.5, 0.5, 1, 0),
        vec4(0.5, -0.5, 1, 1)
    );

    vec2 rotate(vec2 v, float a)
    {
        float s = sin(a);
        float c = cos(a);
        mat2 m = mat2(c, -s, s, c);
        return m * v;
    }

    void main()
    {
        vec4 unit_quad = Quads[gl_VertexIndex];
        vec2 unit_pos = unit_quad.xy;
        vec2 uv_pos = unit_quad.zw;

        //vec2 srcPos = src.xy;
        vec2 pos = unit_pos * Scale.xy;
        pos = rotate(pos, ZRotation);
        pos += Position.xy;

        gl_Position = VP * vec4(pos, 0, 1);

        vec2 uv_sample = uv_pos * uvScale + uvStart;
        //uv_sample = vec2(1, 1) + FlipScale * uv_pos;
    
        fsin_TexCoords = uv_sample ;
        fsin_Tint = Tint;
        fsin_UnitUV = uv_pos;
        fsin_UVScale = uvScale;
    }
}
Fragment
{
    #version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
    layout (set = 1, binding = 1) uniform sampler SpriteSampler;

    layout (set = 2, binding = 0) uniform SpriteProps
    {
        vec4 Padding2;
    };

    layout (set = 3, binding = 0) uniform texture2D NoiseTex;
    layout (set = 3, binding = 1) uniform sampler NoiseTexSampler;

    layout (set = 3, binding = 2) uniform texture2D ScreenTex;
    layout (set = 3, binding = 3) uniform sampler ScreenTexSampler;

    //const float fade = 0.5;

    layout(location = 0) in vec2 fsin_TexCoords;
    layout(location = 1) in vec4 fsin_Tint;
    layout(location = 2) in vec2 fsin_UnitUV;
    layout(location = 3) in vec2 fsin_UVScale;

    layout(location = 0) out vec4 outputColor;

    // Water properties

    const float reflectionOffset = 0.65; // allows player to control reflection position
    const float reflectionBlur = 0.0; // works only if projec's driver is set to GLES3, more information here https://docs.godotengine.org/ru/stable/tutorials/shading/screen-reading_shaders.html
    const float calculatedOffset = 0.0; // this is controlled by script, it takes into account camera position and water object position, that way reflection stays in the same place when camera is moving
    const float calculatedAspect = 1.0; // is controlled by script, ensures that noise is not affected by object scale

    const vec2 distortionScale = vec2(1, 1);
    const vec2 distortionSpeed = vec2(0.02, 0.03);
    const vec2 distortionStrength = vec2(0.4, 0.5);

    const float waveSmoothing = 0.02;

    const float mainWaveSpeed = 1.5;
    const float mainWaveFrequency = 20.0;
    const float mainWaveAmplitude = 0.005;

    const float secondWaveSpeed = 2.5;
    const float secondWaveFrequency = 30.0;
    const float secondWaveAmplitude = 0.015;

    const float thirdWaveSpeed = 3.5;
    const float thirdWaveFrequency = 40.0;
    const float thirdWaveAmplitude = 0.01;

    const float squashing = 1.5;

    const vec4 shorelineColor  = vec4(1.0, 1.0, 1.0, 1.0);
    const float shorelineSize = 0.01;
    const float foamSize  = 0.25;
    const float foamStrength = 0.025;
    const float foamSpeed = 1.0;
    const vec2 foamScale = vec2(1.0, 1.0);

    void main()
    {
        vec4 dummyColor = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);
        vec4 noiseSample = texture(sampler2D(NoiseTex, NoiseTexSampler), fsin_TexCoords * foamScale + Time * foamSpeed);

        vec2 fragPos = gl_FragCoord.xy;
        vec2 uv = fragPos / Resolution;
	    uv.y = 1. - uv.y; // turning screen uvs upside down
	    uv.y *= squashing;
	    uv.y -= calculatedOffset;
	    uv.y += reflectionOffset;
	
	    vec2 noiseTextureUV = fsin_TexCoords * distortionScale; 
	    noiseTextureUV.y *= 1;
	    noiseTextureUV += Time * distortionSpeed; // scroll noise over time

        vec2 noiseDistort = texture(sampler2D(NoiseTex, NoiseTexSampler), noiseTextureUV).rg;
	
	    vec2 waterDistortion = noiseDistort;
	    waterDistortion.rg *= distortionStrength.xy;
	    waterDistortion.xy = smoothstep(0.0, 5., waterDistortion.xy); 
	    uv += waterDistortion;

        vec4 screenSample = texture(sampler2D(ScreenTex, ScreenTexSampler), uv);
	
	    vec4 color = screenSample;
        //color = mix(color, dummyColor, 0.1f);
        //color.a = max(dummyColor.a, 1);
	    //vec4 color = vec4(0.,0.,0.7,1.);
        //vec4 noiseColor = texture(sampler2D(NoiseTex, NoiseTexSampler), fsin_TexCoords);
        //vec4 color = mix(dummyColor, noiseColor, 0.1f);
        //vec4 color = dummyColor;

    
	    //adding the wave amplitude at the end to offset it enough so it doesn't go outside the sprite's bounds
	    float distFromTop = mainWaveAmplitude * sin(fsin_TexCoords.x * mainWaveFrequency + Time * mainWaveSpeed) + mainWaveAmplitude
	 			    + secondWaveAmplitude * sin(fsin_TexCoords.x * secondWaveFrequency + Time * secondWaveSpeed) + secondWaveAmplitude
				    + thirdWaveAmplitude * cos(fsin_TexCoords.x * thirdWaveFrequency - Time * thirdWaveSpeed) + thirdWaveAmplitude;

	    float waveArea = fsin_TexCoords.y - distFromTop;
	
	    waveArea = smoothstep(0., 1. * waveSmoothing, waveArea);
	
	    color.a *= waveArea;

	    float shorelineBottom = fsin_TexCoords.y - distFromTop - shorelineSize;
	    shorelineBottom = smoothstep(0., 1. * waveSmoothing,  shorelineBottom);
	
	    float shoreline = waveArea - shorelineBottom;
	    color.rgb += shoreline * shorelineColor.rgb;
	
	    //this approach allows smoother blendign between shoreline and foam
	    /*
	    float shorelineTest1 = UV.y - distFromTop;
	    shorelineTest1 = smoothstep(0.0, shorelineTest1, shorelineSize);
	    color.rgb += shorelineTest1 * shorelineColor.rgb;
	    */
	
	    vec4 foamNoise = noiseSample;
        //foamNoise.a = dummyColor.a;
	    foamNoise.r = smoothstep(0.0, foamNoise.r, foamStrength); 
	
	    float shorelineFoam = fsin_TexCoords.y - distFromTop;
	    shorelineFoam = smoothstep(0.0, shorelineFoam, foamSize);
	
	    shorelineFoam *= foamNoise.r;
        color.rgb = mix(color.rgb, vec3(0.0, 0.4, 0.9), 0.3);
	    color.rgb += shorelineFoam * shorelineColor.rgb;
	
	    outputColor = color;
    }
}
"
;

        internal const string UberPipelineAsset = @"
Properties
{
	DissolveFade:float
    EnableFade:float
    EnableOutline:float
	OutlineThickness:float
	OutlineColor:vec4
    ReplaceColor:vec4
    EnableReplace:float
    EnableShine:float
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

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec2 Scale;
    layout(location = 2) in vec3 WorldScale;
    layout(location = 3) in vec4 Tint;
    layout(location = 4) in float ZRotation;
    layout(location = 5) in vec2 uvStart;
    layout(location = 6) in vec2 uvScale;
    layout(location = 7) in vec2 Pivot;

    layout(location = 0) out vec2 fsin_TexCoords;
    layout(location = 1) out vec4 fsin_Tint;
    layout(location = 2) out vec2 fsin_UnitUV;
    layout(location = 3) out vec2 fsin_UVScale;

    //vec2 Pivot = vec2(-0.2, 0);


    //   B____C
    //   |   /| 
    //   |  / |
    //   | /  |
    //   |/___|
    //  A     D

    const vec4 Quads[6]= vec4[6](
        vec4(-0.5, -0.5, 0, 1),
        vec4(-0.5, 0.5, 0, 0),
        vec4(0.5, 0.5, 1, 0),
        vec4(-0.5, -0.5, 0, 1),
        vec4(0.5, 0.5, 1, 0),
        vec4(0.5, -0.5, 1, 1)
    );

    vec2 rotate(vec2 v, float a)
    {
        float s = sin(a);
        float c = cos(a);
        mat2 m = mat2(c, -s, s, c);
        return m * v;
    }

    void main()
    {
        vec4 unit_quad = Quads[gl_VertexIndex];
        vec2 unit_pos = unit_quad.xy;
        vec2 uv_pos = unit_quad.zw;

        //vec2 srcPos = src.xy;
        //vec2 pos = unit_pos * Scale.xy;
        //pos = rotate(pos, ZRotation);
        //pos += Position.xy + Pivot*Scale.xy;

        vec2 pos = ((unit_pos + Pivot) * Scale.xy);
        pos *= WorldScale.xy;
        pos = rotate(pos, ZRotation);
        pos += Position.xy;

        //if(Tint.r == 1 && Tint.g == 0 )
        //{
        //    float normTime = fract(Time / 2.0) * 2.0;
        //    if(unit_quad.x > 0)
        //        pos += vec2(normTime * 4.0, 0);
        //    else
        //        pos -= vec2(normTime , 0);
        //}

        gl_Position = Projection * View * vec4(pos, Position.z, 1);

        vec2 uv_sample = uv_pos * uvScale + uvStart;
        //uv_sample = vec2(1, 1) + FlipScale * uv_pos;
    
        fsin_TexCoords = uv_sample ;
        fsin_Tint = Tint;
        fsin_UnitUV = uv_pos;
        fsin_UVScale = uvScale;
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

    layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
    layout (set = 1, binding = 1) uniform sampler SpriteSampler;
    layout (set = 1, binding = 2) uniform texture2D NormalTex; 
    layout (set = 1, binding = 3) uniform sampler NormalSampler;

    layout (set = 2, binding = 0) uniform ShaderProps
    {
        float DissolveFade;
        float EnableFade;
        float EnableOutline;
        float OutlineThickness;
        vec4 OutlineColor;
        vec4 ReplaceColor;
        float EnableReplace;
        float EnableShine;
    };


    //const float fade = 0.5;

    layout(location = 0) in vec2 fsin_TexCoords;
    layout(location = 1) in vec4 fsin_Tint;
    layout(location = 2) in vec2 fsin_UnitUV;
    layout(location = 3) in vec2 fsin_UVScale;

    layout(location = 0) out vec4 outputColor;
    layout(location = 1) out vec4 outputNormal;

    // Shine

    const vec4 shine_color = vec4( 1.0, 1.0, 1.0, 1.0 );
    const float shine_interval = 3.0;
    const float shine_speed = 3.0;
    const float shine_width  = 3.0;

    // Glitch

    
    float rand1(vec2 co, float random_seed)
    {
        return fract(sin(dot(co.xy * random_seed, vec2(12.,85.5))) * 120.01);
    }


    float rand2(vec2 co, float random_seed)
    {
        float r1 = fract(sin(dot(co.xy * random_seed ,vec2(12.9898, 78.233))) * 43758.5453);
        return fract(sin(dot(vec2(r1 + co.xy * 1.562) ,vec2(12.9898, 78.233))) * 43758.5453);
    }


    vec4 glitch(float glitch_size, float glitch_amount, float _speed, float value)
    {
        vec2 uv = fsin_TexCoords;
	    float seed = floor(value * _speed * 10.0) / 10.0;
	    vec2 blockS = floor(uv * vec2 (24., 19.) * glitch_size) * 4.0;
	    vec2  blockL = floor(uv * vec2 (38., 14.) * glitch_size) * 4.0;

	    float line_noise = pow(rand2(blockS, seed), 3.0) * glitch_amount * pow(rand2(blockL, seed), 3.0);
	    vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), uv + vec2 (line_noise * 0.02 * rand1(vec2(2.0), seed), 0) * fsin_UVScale);
	    return color;
    }

    vec4 blend_color(vec4 txt, vec4 color, float value)
    {
	    vec3 tint = vec3(dot(txt.rgb, vec3(.22, .7, .07)));
	    tint.rgb *= color.rgb;
	    txt.rgb = mix(txt.rgb, tint.rgb, value);
	    return txt;
    }
    

    // Dissolve
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

    const vec2 OFFSETS[8] = {
	    vec2(-1, -1), vec2(-1, 0), vec2(-1, 1), vec2(0, -1), vec2(0, 1), 
	    vec2(1, -1), vec2(1, 0), vec2(1, 1)
    };


    vec2 rotate(vec2 v, float a)
    {
        float s = sin(a);
        float c = cos(a);
        mat2 m = mat2(c, -s, s, c);
        return m * v;
    }

    vec4 getColor(vec2 texCoord) {
        return texture(sampler2D(SpriteTex, SpriteSampler), texCoord);
    }


    const vec2 velocity = vec2(1, 0);
    void main()
    {
        vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);

        float dummy = Time - Time;
        //vec3 dummy2 = DummyProp - DummyProp;

        // Sprite Color Tint
        color *= fsin_Tint;
      
        // OUTLINE

        if(EnableOutline != 0)
        {
            vec2 size = fsin_UVScale * OutlineThickness;
	        float outline = 0.0;
	        for (int i = 0; i < OFFSETS.length(); i++) {
		        outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + size * OFFSETS[i]).a;
	        }
	        outline = min(outline, 1.0);
        
	        color = mix(color, OutlineColor, outline - step(0.1, color.a));
        }


        //float glitchVal = 20.0 + Time * 1.0;
	    //color = glitch(0.0, 10.0, 1.0, glitchVal);

        // DISSOLVE FADE

        if(EnableFade != 0)
        {
            vec2 coord = fsin_UnitUV * 10;
	        float value = perlin_noise(coord) + dummy;
            color.a *= floor(DissolveFade + min(1, value));
        }


        if(color.a < 0.1f)
            discard;

        // SHINE
        if(EnableShine == 1)
        {
            float freq = clamp(fract(Time / 5.0) * 5.0, 0, 1);
            freq /= 1.0;
            freq = freq * 0.8 + 0.0;
            //freq = freq * -1 + 1; // Reverse dir
        

            vec2 rotUV = rotate(fsin_UnitUV, -0.3);
            
            color = mix(vec4(1.0), color, step(0.01, abs(freq - rotUV.x)));
        }


        // Replace Color
        if(EnableReplace != 0)
        {
            color.rgb = ReplaceColor.rgb;
            //color = vec4(1.0);
        }

       
	    //vec4 tint = blend_color(color, vec4(1, 1, 1, 1), 0.0);
        outputColor = color;
        vec4 normalSample = texture(sampler2D(NormalTex, NormalSampler), fsin_TexCoords);
        normalSample.xy *= vec2(sign(normalSample.x), sign(normalSample.y));
        outputNormal = normalSample;
    }
}
"
;

        internal const string PointLightVertex = @"
#version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;
layout(location = 2) in float Radius;
layout(location = 3) in float Intensity;
layout(location = 4) in float Volume;


layout(location = 0) out vec4 fs_LightColor;
layout(location = 1) out float fs_Intensity;
layout(location = 2) out float fs_Volume;


void main()
{
    gl_PointSize = 100 * Radius;
    gl_Position = VP * vec4(Position.xyz, 1);

    fs_LightColor = Color;
    fs_Intensity = Intensity;
    fs_Volume = Volume;
}
";

        internal const string PointLightFragment = @"
#version 450

   layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

layout(set = 1, binding = 0) uniform texture2D SpriteTex;
layout(set = 1, binding = 1) uniform sampler SpriteSampler;

layout(location = 0) in vec4 fs_LightColor;
layout(location = 1) in float fs_Intensity;
layout(location = 2) in float fs_Volume;


layout(location = 0) out vec4 OutputColor;

void main()
{

  // Light1
  // Gets the distance from the light's position and the fragment coord
  vec2 circCoord = 2.0 * gl_PointCoord - 1.0;

  vec2 screenUV = gl_FragCoord.xy / Resolution;

  vec4 sampleColor = texture(sampler2D(SpriteTex, SpriteSampler), screenUV);

  float distance = distance(vec2(0.0), circCoord);
  // Calculates the amount of light for the fragment

   
    //float radialFalloff = pow(1 - distance, 2);
    float radialFalloff = 1.0 - smoothstep(-0.2, 1, distance / fs_Volume);
    //if(distance > 1)
    //    radialFalloff = 0;

  
 
    float finalInt = fs_Intensity * radialFalloff;
    vec3 endColor = fs_LightColor.rgb * finalInt;

    float volInt = 0.0;

    //endColor = clamp(vec3(0.3) + endColor, vec3(0), vec3(1));
    //vec3 color = sampleColor.rgb + sampleColor.rgb * (1.0 / 0.3) * endColor;
    vec3 color = sampleColor.rgb * endColor;
    color += fs_LightColor.rgb * volInt * radialFalloff;
    //color.a = clamp(radialFalloff, 0, 1);
    //color.rgb = sampleColor.rgb;
    
    //vec4 color = mix(sampleColor, sampleColor + sampleColor * fs_LightColor * fs_Intensity, clamp(value, 0, 1));

     //vec4 color = fs_LightColor * fs_Intensity * clamp(value, 0, 1);


     OutputColor = vec4(color, sampleColor.a);
}
";
        internal const string PointLightVertex2 = @"
#version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 Projection;
        mat4 View;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    //   B____C
    //   |   /| 
    //   |  / |
    //   | /  |
    //   |/___|
    //  A     D

    const vec4 Quads[6]= vec4[6](
        vec4(-0.5, -0.5, 0, 1),
        vec4(-0.5, 0.5, 0, 0),
        vec4(0.5, 0.5, 1, 0),
        vec4(-0.5, -0.5, 0, 1),
        vec4(0.5, 0.5, 1, 0),
        vec4(0.5, -0.5, 1, 1)
    );


    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec4 Color;
    layout(location = 2) in float Radius;
    layout(location = 3) in float Intensity;
    layout(location = 4) in float Volume;
    layout(location = 5) in float Global;

    layout(location = 0) out vec4 fs_LightColor;
    layout(location = 1) out float fs_Intensity;
    layout(location = 2) out float fs_Volume;
    layout(location = 3) out vec2 fs_UV;
    layout(location = 4) out float fs_Global;

    void main()
    {
        vec4 unit_quad = Quads[gl_VertexIndex];
        vec2 unit_pos = unit_quad.xy;
        vec2 uv_pos = unit_quad.zw;

        vec2 pos = unit_pos * Radius;
        pos += Position.xy;

        vec3 cameraPosition = -transpose(mat3(View)) * View[3].xyz;
        vec3 endPos = vec3(pos, Position.z) - cameraPosition;

        gl_Position = Projection * vec4(endPos, 1);

        fs_LightColor = Color;
        fs_Intensity = Intensity;
        fs_Volume = Volume;
        fs_UV = uv_pos;
        fs_Global = Global;
    }
";

        internal const string PointLightFragment2 = @"
#version 450

   layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 Projection;
        mat4 View;
        vec2 Resolution;
        float Time;
        float Padding;
    };

layout(set = 1, binding = 0) uniform texture2D MainTex;
layout(set = 1, binding = 1) uniform sampler MainSampler;
layout(set = 1, binding = 2) uniform texture2D NormalTex;
layout(set = 1, binding = 3) uniform sampler NormalSampler;

layout(location = 0) in vec4 fs_LightColor;
layout(location = 1) in float fs_Intensity;
layout(location = 2) in float fs_Volume;
layout(location = 3) in vec2 fs_UV;
layout(location = 4) in float fs_Global;

layout(location = 0) out vec4 OutputColor;

void main()
{

    vec2 screenUV = gl_FragCoord.xy / Resolution;

    vec4 sampleColor = texture(sampler2D(MainTex, MainSampler), screenUV);
    vec4 normalSample =  texture(sampler2D(NormalTex, NormalSampler), screenUV);
    vec3 normal = normalSample.rgb;  // Get normal from normal map

    vec2 circCoord = 2.0 * fs_UV - 1.0;  // Centered coordinates for the quad
    float distance = length(circCoord);  // Distance from the center of the quad

    float radialFalloff = 1.0 - smoothstep(-0.2, 1, distance) * (1 - fs_Global);  // Adjusted falloff

    vec2 lightDir = normalize(circCoord);  // Light direction from quad center to fragment
    float NdotL = max(dot(normal.xy, lightDir), 0.0);  // Compute dot product

    //float nrmAtt = mix(1, NdotL, 1 - normalSample.a);
    float finalInt = fs_Intensity * radialFalloff * NdotL;  // Multiply by NdotL for normal-based attenuation
    vec3 endColor = fs_LightColor.rgb * finalInt;

    vec3 color = mix(sampleColor.rgb * endColor, sampleColor.rgb * fs_Intensity, fs_Global);  // Mix based on fs_Global flag

    OutputColor = vec4(color, sampleColor.a);
}
";

        internal const string LineDebugVertex = @"
#version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 Projection;
        mat4 View;
        vec2 Resolution;
        float Time;
        float Padding;
    };

layout(location = 0) in vec4 Color;
layout(location = 1) in vec3 Position;

layout(location = 0) out vec4 fs_Color;

void main()
{
    gl_Position = Projection * View * vec4(Position.xyz, 1.0);
    fs_Color = Color;
}
";

        internal const string LineDebugFragment = @"
#version 450

   layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 Projection;
        mat4 View;
        vec2 Resolution;
        float Time;
        float Padding;
    };

layout(location = 0) in vec4 fs_Color;

layout(location = 0) out vec4 OutputColor;

void main()
{    
  OutputColor = fs_Color;
}
";

        internal const string DebugUberVertex = @"
#version 450

    layout (set = 0, binding = 0) uniform PipelineData3D
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout(location = 0) in vec3 Position;
    layout(location = 1) in vec3 Scale;
    layout(location = 2) in vec4 Tint;
    layout(location = 3) in float ZRotation;
    layout(location = 4) in vec2 uvStart;
    layout(location = 5) in vec2 uvScale;

    layout(location = 0) out vec2 fsin_TexCoords;
    layout(location = 1) out vec4 fsin_Tint;
    layout(location = 2) out vec2 fsin_UnitUV;
    layout(location = 3) out vec2 fsin_UVScale;

    //layout(constant_id = 0) const bool InvertedY = false;


    //   B____C
    //   |   /| 
    //   |  / |
    //   | /  |
    //   |/___|
    //  A     D

    const vec4 Quads[6]= vec4[6](
        vec4(-0.5, -0.5, 0, 1),
        vec4(-0.5, 0.5, 0, 0),
        vec4(0.5, 0.5, 1, 0),
        vec4(-0.5, -0.5, 0, 1),
        vec4(0.5, 0.5, 1, 0),
        vec4(0.5, -0.5, 1, 1)
    );

    vec2 rotate(vec2 v, float a)
    {
        float s = sin(a);
        float c = cos(a);
        mat2 m = mat2(c, -s, s, c);
        return m * v;
    }

    void main()
    {
        vec4 unit_quad = Quads[gl_VertexIndex];
        vec2 unit_pos = unit_quad.xy;
        vec2 uv_pos = unit_quad.zw;

        //vec2 srcPos = src.xy;
        vec2 pos = unit_pos * Scale.xy;
        pos = rotate(pos, ZRotation);
        pos += Position.xy;

        gl_Position = VP * vec4(pos, Position.z, 1);

        vec2 uv_sample = uv_pos * uvScale + uvStart;
        //uv_sample = vec2(1, 1) + FlipScale * uv_pos;
    
        fsin_TexCoords = uv_sample ;
        fsin_Tint = Tint;
        fsin_UnitUV = uv_pos;
        fsin_UVScale = uvScale;
    }
";


        internal const string DebugUberFragment = @"
#version 450

    layout (set = 0, binding = 0) uniform PipelineData
    {
        mat4 VP;
        vec2 Resolution;
        float Time;
        float Padding;
    };

    layout (set = 1, binding = 0) uniform texture2D SpriteTex; 
    layout (set = 1, binding = 1) uniform sampler SpriteSampler;

    layout (set = 2, binding = 0) uniform ShaderProps
    {
        float DissolveFade;
        float EnableFade;
        float EnableOutline;
        float OutlineThickness;
        float EnableReplace;
        vec4 OutlineColor;
        vec4 ReplaceColor;
        float EnableShine;
    };


    //const float fade = 0.5;

    layout(location = 0) in vec2 fsin_TexCoords;
    layout(location = 1) in vec4 fsin_Tint;
    layout(location = 2) in vec2 fsin_UnitUV;
    layout(location = 3) in vec2 fsin_UVScale;

    layout(location = 0) out vec4 outputColor;

    // Glitch

    
    float rand1(vec2 co, float random_seed)
    {
        return fract(sin(dot(co.xy * random_seed, vec2(12.,85.5))) * 120.01);
    }


    float rand2(vec2 co, float random_seed)
    {
        float r1 = fract(sin(dot(co.xy * random_seed ,vec2(12.9898, 78.233))) * 43758.5453);
        return fract(sin(dot(vec2(r1 + co.xy * 1.562) ,vec2(12.9898, 78.233))) * 43758.5453);
    }


    vec4 glitch(float glitch_size, float glitch_amount, float _speed, float value)
    {
        vec2 uv = fsin_TexCoords;
	    float seed = floor(value * _speed * 10.0) / 10.0;
	    vec2 blockS = floor(uv * vec2 (24., 19.) * glitch_size) * 4.0;
	    vec2  blockL = floor(uv * vec2 (38., 14.) * glitch_size) * 4.0;

	    float line_noise = pow(rand2(blockS, seed), 3.0) * glitch_amount * pow(rand2(blockL, seed), 3.0);
	    vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), uv + vec2 (line_noise * 0.02 * rand1(vec2(2.0), seed), 0) * fsin_UVScale);
	    return color;
    }

    vec4 blend_color(vec4 txt, vec4 color, float value)
    {
	    vec3 tint = vec3(dot(txt.rgb, vec3(.22, .7, .07)));
	    tint.rgb *= color.rgb;
	    txt.rgb = mix(txt.rgb, tint.rgb, value);
	    return txt;
    }
    

    // Dissolve
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

    const vec2 OFFSETS[8] = {
	    vec2(-1, -1), vec2(-1, 0), vec2(-1, 1), vec2(0, -1), vec2(0, 1), 
	    vec2(1, -1), vec2(1, 0), vec2(1, 1)
    };

    void main()
    {
        vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);

        float dummy = Time - Time;
        //vec3 dummy2 = DummyProp - DummyProp;
      
        // OUTLINE

        if(EnableOutline != 0)
        {
            vec2 size = fsin_UVScale * OutlineThickness;
	        float outline = 0.0;
	        for (int i = 0; i < OFFSETS.length(); i++) {
		        outline += texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords + size * OFFSETS[i]).a;
	        }
	        outline = min(outline, 1.0);
        
	        color = mix(color, OutlineColor, outline - color.a);
        }


        //float glitchVal = 20.0 + Time * 1.0;
	    //color = glitch(0.0, 10.0, 1.0, glitchVal);

        // DISSOLVE FADE

        if(EnableFade != 0)
        {
            vec2 coord = fsin_UnitUV * 10;
	        float value = perlin_noise(coord) + dummy;
            color.a *= floor(DissolveFade + min(1, value));
        }

        // Sprite Color Tint
        color *= fsin_Tint;

        if(EnableReplace != 0)
        {
            color = ReplaceColor;
        }

        if(color.a < 0.01f)
            discard;

        
        //outputColor = color;



	    //vec4 tint = blend_color(color, vec4(1, 1, 1, 1), 0.0);
        color.rgb *= 1.0f;
        outputColor = color;
    }
";
    }
}

