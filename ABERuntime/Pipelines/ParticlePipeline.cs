using System;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Pipelines
{
	public class ParticlePipeline : PipelineAsset
	{
		public ParticlePipeline()
		{
			base.ParseAsset(Shaders.ParticlePipelineAsset);
		}
	}
}

