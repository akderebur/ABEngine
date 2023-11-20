using System;
using System.Numerics;

namespace ABEngine.ABERuntime.Components
{
	public class DirectionalLight : ABComponent
	{
		public Vector4 color { get; set; }
		public Vector3 direction { get; set; }
		public float Intensity { get; set; }
	}
}

