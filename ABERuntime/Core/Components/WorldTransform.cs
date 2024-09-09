using System.Numerics;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Components;

public struct WorldTransform : IComponent
{
    public Matrix4x4 matrix;
}