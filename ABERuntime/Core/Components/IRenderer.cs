using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Components
{
	public interface IRenderer
	{
		public Mesh Mesh { get; set; }
		public PipelineMaterial Material { get; set; }
	}
}

