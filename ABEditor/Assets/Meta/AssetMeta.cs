using System;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.ECS;
using Halak;

namespace ABEngine.ABEditor.Assets.Meta
{
	public abstract class AssetMeta : JSerializable
	{
        public Guid uniqueID { get; set; }
        public string metaAssetPath { get; set; }
        public event Action<AssetMeta, string> refreshEvent;

        public AssetMeta()
		{
            uniqueID = Guid.NewGuid();
		}

        public abstract void Deserialize(string json);
        public abstract JSerializable GetCopy(ref Entity newEntity);
        public abstract JValue Serialize();
        public abstract void SetReferences();
        public abstract void DrawMeta();

        public virtual void RefreshAsset(string assetPath)
        {
            refreshEvent(this, assetPath);
        }
    }
}

