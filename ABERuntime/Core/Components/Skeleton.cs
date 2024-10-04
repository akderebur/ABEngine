using System;
using Friflo.Engine.ECS;
using Halak;

namespace ABEngine.ABERuntime.Components
{
	public struct Skeleton : JSerializable, IComponent
    {
        public Entity[] Bones = null;

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

