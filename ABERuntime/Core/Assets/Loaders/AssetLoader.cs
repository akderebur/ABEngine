using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ABEngine.ABERuntime.Core.Assets
{
	internal abstract class AssetLoader
	{
		internal virtual async Task<Asset> LoadAssetRAW(byte[] data) { return null; }
    }
}

