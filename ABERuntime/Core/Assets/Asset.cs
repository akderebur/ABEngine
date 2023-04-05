using System;
using Halak;

namespace ABEngine.ABERuntime.Core.Assets
{
	public abstract class Asset
	{
		internal uint fPathHash { get; set; }
		public string name;

		internal abstract JValue SerializeAsset();
	}
}

