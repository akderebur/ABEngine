using System;
using Halak;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
    public interface JSerializable
    {
        public JValue Serialize();
        public void Deserialize(string json);
        public void SetReferences();
        public JSerializable GetCopy(ref Entity newEntity);
       // public static T AA();
    }
}
