using System;
using System.IO;
using System.Threading.Tasks;

namespace ABEngine.ABERuntime.Core.Assets
{
	internal class PrefabLoader : AssetLoader
	{
        internal override async Task<Asset> LoadAssetRAW(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                PrefabAsset prefabAsset = new PrefabAsset();
                prefabAsset.serializedData = br.ReadString();
                if (ms.Position + 20 <= ms.Length && br.ReadInt32() == AssetCache.guidMagic)
                    prefabAsset.prefabGuid = new Guid(br.ReadBytes(16));

                return prefabAsset;
            }
        }
    }
}

