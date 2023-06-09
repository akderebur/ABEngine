using System;
namespace ABEngine.ABERuntime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CollisionEventAttribute : Attribute
    {
        public Type[] ComponentTypes { get; }

        public CollisionEventAttribute(params Type[] componentTypes)
        {
            ComponentTypes = componentTypes;
        }
    }
}

