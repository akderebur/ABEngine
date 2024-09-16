using System;
using System.Numerics;
using ABEngine.ABERuntime.Systems;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Components;

public struct BVSphere : IComponent
{
    public float Radius;
    public BVHGroup BVHGroup;

    public BVSphere()
    {
        Radius = 1;
        BVHGroup = BVHSystem.GetDefaultBVHGroup();
    }
    
    public static BVSphere FromSprite(in Sprite sprite)
    {
        Vector2 size = sprite.GetSize();
        return new BVSphere()
        {
            Radius = MathF.Max(size.X, size.Y)
        };
    }
}