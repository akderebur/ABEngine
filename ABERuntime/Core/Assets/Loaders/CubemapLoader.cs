using System;
using System.IO;

namespace ABEngine.ABERuntime.Core.Assets
{
    internal class CubemapLoader : AssetLoader
    {
        internal override TextureCube LoadAssetRAW(byte[] data)
        {
            using (MemoryStream fs = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(fs))
            {
                
            }

            return null;
        }
    }
}