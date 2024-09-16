using System.Numerics;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Components
{
	public struct DirectionalLight : IComponent
	{
		public Vector4 color;
		public Vector3 direction;
		public float intensity;

		public DirectionalLight()
		{
			direction = -Vector3.UnitZ;;
			color = Vector4.One;
			intensity = 1f;
		}
	}
}

