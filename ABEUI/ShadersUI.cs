using System;
namespace ABEngine.ABEUI
{
	internal static class ShadersUI
	{
        internal const string SliderVertex = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoords;

layout(location = 0) out vec2 fsin_0;

void main()
{
    fsin_0 = TexCoords;
    gl_Position = vec4(Position, 0, 1);
}
";

        internal const string SliderFragment = @"
#version 450

layout (set = 0, binding = 0) uniform SliderInfo
{
    float Percentage;
    float Dummy;
    vec2 SlideDir;
    vec4 SliderColor;
};

layout(set = 0, binding = 1) uniform texture2D SpriteTex;
layout(set = 0, binding = 2) uniform sampler SpriteSampler;

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 0) out vec4 OutputColor;

void main()
{
    vec4 color = texture(sampler2D(SpriteTex, SpriteSampler), fsin_TexCoords);
    OutputColor =  mix(vec4(0), color, step(fsin_TexCoords.x * SlideDir.x + fsin_TexCoords.y * SlideDir.y, Percentage / 100.0));
}
";
    }
}

