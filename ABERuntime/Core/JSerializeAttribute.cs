using System;
namespace ABEngine.ABERuntime
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class JSerializeAttribute : Attribute
    {
        public JSerializeAttribute()
        {
        }
    }
}

