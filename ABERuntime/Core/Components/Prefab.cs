using System;
using System.Numerics;
using Halak;

namespace ABEngine.ABERuntime.Components
{
	public class Prefab : JSerializable
	{
		public uint prefabHash { get; set; }

		public Prefab()
		{
		}

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("type", GetType().ToString());
            jObj.Put("PrefabHash", (long)prefabHash);

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            long hash = data["PrefabHash"];
            prefabHash = (uint)hash;
        }

        public void SetReferences()
        {
        }

        public JSerializable GetCopy()
        {
            return new Prefab()
            {
                prefabHash = prefabHash
            };
        }
    }
}

