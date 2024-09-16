using ABEngine.ABERuntime.Systems;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Components;

public interface IRenderable
{
    public BVHGroup bvhGroup { get; set; }
}