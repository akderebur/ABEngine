using System;
using Halak;

namespace ABEngine.ABERuntime.Core.Assets
{
	public abstract class Asset
	{
		internal uint fPathHash { get; set; }

		internal abstract JValue SerializeAsset();
	}
}

