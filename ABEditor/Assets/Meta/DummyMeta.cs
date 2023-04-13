using System;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Core.Assets;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class DummyMeta : AssetMeta
	{
		public DummyMeta()
		{
		}

        public override Asset CreateAssetBinding()
        {
            throw new NotImplementedException();
        }

        public override void DrawMeta()
        {
            throw new NotImplementedException();
        }

        public override JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }

        public override void SetReferences()
        {
            throw new NotImplementedException();
        }
    }
}

