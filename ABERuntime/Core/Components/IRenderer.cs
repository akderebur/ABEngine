using System;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Components
{
	public interface IRenderer
	{
		public Mesh mesh { get; set; }
		public PipelineMaterial material { get; set; }
	}
}

