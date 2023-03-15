using System;
namespace ABEngine.ABERuntime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SubscribeAttribute : Attribute
	{
        public Type[] ComponentTypes { get; }

        public SubscribeAttribute(params Type[] componentTypes)
        {
            ComponentTypes = componentTypes;
        }
    }
}

