using System;
using System.Collections.Generic;
using System.IO;

namespace ABEngine.ABERuntime.Core.Assets
{
	internal abstract class AssetLoader
	{
		internal virtual Asset LoadAssetRAW(byte[] data) { return null; }
    }
}

