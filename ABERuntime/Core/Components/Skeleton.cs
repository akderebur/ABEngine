using System;
using Halak;

namespace ABEngine.ABERuntime.Components
{
	public class Skeleton : JSerializable
	{
        public Transform[] bones { get; internal set; }

        public Skeleton()
		{
		}

        public void Deserialize(string json)
        {
            throw new NotImplementedException();
        }

        public JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }

        public JValue Serialize()
        {
            throw new NotImplementedException();
        }

        public void SetReferences()
        {
            throw new NotImplementedException();
        }
    }
}

