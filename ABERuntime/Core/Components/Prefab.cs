using System;
using System.Numerics;
using Halak;
using System.Collections.Generic;

namespace ABEngine.ABERuntime.Components
{
	public class Prefab : JSerializable
	{
		//public PrefabAsset prefabAsset { get; set; }

        public Prefab()
		{
		}

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("type", GetType().ToString());
            //jObj.Put("PrefabAsset", AssetCache.GetAssetSceneIndex(this.prefabAsset.fPathHash));

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            //prefabAsset = AssetCache.GetAssetFromSceneIndex(data["PrefabAsset"]) as PrefabAsset;
        }

        public void SetReferences()
        {
        }

        public JSerializable GetCopy()
        {
            return new Prefab()
            {
                //prefabAsset = this.prefabAsset
            };
        }
    }
}

