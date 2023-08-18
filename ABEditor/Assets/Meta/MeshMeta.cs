using System;
using System.IO;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Core.Assets;
using Halak;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class MeshMeta : AssetMeta
	{
		public MeshMeta()
		{
		}

        public override Mesh CreateAssetBinding()
        {
            Mesh mesh = AssetCache.CreateMesh(base.fPath);
            return mesh;
        }

        public override JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }

        public override void SetReferences()
        {
            throw new NotImplementedException();
        }

        public override void DrawMeta()
        {

        }

        public override JValue Serialize()
        {
            base.Serialize();
            return jObj.Build();
        }
    }
}

