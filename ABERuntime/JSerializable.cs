using System;
using Halak;

namespace ABEngine.ABERuntime
{
    public interface JSerializable
    {
        public JValue Serialize();
        public void Deserialize(string json);
        public void SetReferences();
        public JSerializable GetCopy();
       // public static T AA();
    }
}
