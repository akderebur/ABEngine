using System;
using Halak;

namespace ABEngine.ABERuntime.Core.Assets
{
	public abstract class Asset
	{
		public uint fPathHash { get; set; }

		internal abstract JValue SerializeAsset();
	}
}

