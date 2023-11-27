using System;
namespace ABEngine.ABERuntime.Rendering
{
    public enum RenderOrder
    {
        Opaque = 100,
        Transparent = 200,
        PostProcess = 300
    }

    public enum RenderType
    {
        Opaque,
        Transparent
    }
}

