using System.Numerics;

namespace ABEngine.ABERuntime.Components
{
	public class DirectionalLight : ABComponent
	{
		public Vector4 color { get; set; }
		public Vector3 direction { get; set; }
		public float intensity { get; set; }

		public DirectionalLight()
		{
			color = Vector4.One;
			intensity = 1f;
		}
	}
}

