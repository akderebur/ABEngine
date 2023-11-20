using System;
namespace ABEngine.ABERuntime.Core.Assets
{
	public class UserPipelineAsset : PipelineAsset
	{
		public UserPipelineAsset(string assetContent) : base()
		{
			base.ParseAsset(assetContent);
		}
	}
}

