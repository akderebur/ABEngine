using System;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.ECS;

public struct EntityGuid : IComponent
{
    public Guid value { get; set; }
    
    public EntityGuid()
    {
        value = Guid.NewGuid();
    }

    public EntityGuid(Guid guid)
    {
        value = guid;
    }
    
    public          bool    Equals      (EntityGuid other)                  => value == other.value;
    public static   bool    operator == (in EntityGuid g1, in EntityGuid g2)    => g1.value == g1.value;
    public static   bool    operator != (in EntityGuid g1, in EntityGuid g2)    => g1.value != g2.value;
}