using System;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Core.Assets;
using Halak;

namespace ABEngine.ABEditor.Assets.Meta
{
	public abstract class AssetMeta : JSerializable
	{
        public Guid uniqueID { get; set; }
        public uint fPathHash { get; set; }
        public string fPath { get; set; }
        public string displayName { get; set; }

        public string metaAssetPath { get; set; }
        public event Action<AssetMeta> refreshEvent;

        protected JsonObjectBuilder jObj; // Serialize
        protected JValue data; // Deserialize

        public AssetMeta()
		{
            uniqueID = Guid.NewGuid();
            fPathHash = 0;
            fPath = "";
		}

        public virtual JValue Serialize()
        {
            jObj = new JsonObjectBuilder(500);
            jObj.Put("GUID", uniqueID.ToString());
            jObj.Put("FileHash", (long)fPathHash);
            jObj.Put("FilePath", fPath);

            return null;
        }

        public virtual void Deserialize(string json)
        {
            data = JValue.Parse(json);
            uniqueID = Guid.Parse(data["GUID"]);
            long fPathHash = data["FileHash"];
            this.fPathHash = (uint)fPathHash;
            fPath = data["FilePath"];
            displayName = System.IO.Path.GetFileNameWithoutExtension(fPath);
        }

        public virtual void MetaCreated()
        {

        }

        public abstract Asset CreateAssetBinding();
        public abstract JSerializable GetCopy();
        public abstract void SetReferences();
        public abstract void DrawMeta();

        public virtual void RefreshAsset()
        {
            refreshEvent(this);
        }
    }
}

