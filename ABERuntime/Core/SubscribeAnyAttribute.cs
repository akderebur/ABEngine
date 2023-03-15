using System;
namespace ABEngine.ABERuntime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SubscribeAnyAttribute : Attribute
    {
        public Type[] ComponentTypes { get; }

        public SubscribeAnyAttribute(params Type[] componentTypes)
        {
            ComponentTypes = componentTypes;
        }
    }
}

