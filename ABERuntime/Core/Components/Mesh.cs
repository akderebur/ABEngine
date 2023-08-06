using System;
using System.Numerics;
using Veldrid;

namespace ABEngine.ABERuntime.Core.Components
{
	public class Mesh
	{
		public PipelineMaterial material { get; set; }

	}

    public struct VertexStandard
    {
        public const uint VertexSize = 44;

        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public Vector3 Tangent;

        public VertexStandard(Vector3 position, Vector3 normal, Vector2 uv) : this(position, normal, uv, Vector3.Zero) { }
        public VertexStandard(Vector3 position, Vector3 normal, Vector2 uv, Vector3 tangent)
        {
            Position = position;
            Normal = normal;
            UV = uv;
            Tangent = tangent;
        }
    }
}

