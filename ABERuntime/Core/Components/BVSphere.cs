using System;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.Systems;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Components;

public struct BVSphere : IComponent
{
    public float radius;
    public BVHGroup bvhGroup;

    public BVSphere()
    {
        radius = 1;
        bvhGroup = BVHSystem.GetDefaultBVHGroup();
    }
    
    public static BVSphere FromSprite(in Sprite sprite)
    {
        Vector2 size = sprite.GetSize();
        return new BVSphere()
        {
            radius = MathF.Max(size.X, size.Y)
        };
    }
    
    public static BVSphere FromMesh(in Mesh mesh)
    {
        return new BVSphere()
        {
            radius = MathF.Max(MathF.Max(mesh.boundsMax.X, mesh.boundsMax.Y), mesh.boundsMax.Z)
        };
    }
}