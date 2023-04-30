using System;
using ABEngine.ABERuntime.Core.Assets;
using Halak;

namespace ABEngine.ABERuntime
{
	public class PrefabAsset : Asset
	{
        public string serializedData { get; set; }
        public Guid prefabGuid { get; set; }

        public PrefabAsset(uint hash)
		{
            base.fPathHash = hash;
		}

        internal override JValue SerializeAsset()
        {
            JsonObjectBuilder assetEnt = new JsonObjectBuilder(200);
            assetEnt.Put("TypeID", 2);
            assetEnt.Put("FileHash", (long)fPathHash);
            return assetEnt.Build();
        }
    }
}

