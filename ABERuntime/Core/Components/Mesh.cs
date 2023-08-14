using System;
using System.Numerics;
using Veldrid;

namespace ABEngine.ABERuntime.Core.Components
{
	public class Mesh
	{
		public PipelineMaterial material { get; set; }

        // TODO Switch to vertex interface
        private VertexStandard[] _vertices;
        private ushort[] _indices;

        internal DeviceBuffer vertexBuffer;
        internal DeviceBuffer indexBuffer;
        internal DeviceBuffer vertexUniformBuffer;

        internal ResourceSet vertexTransformSet;


        public Mesh()
        {
            material = GraphicsManager.GetUber3D();

            // Mesh model matrix
            vertexUniformBuffer = GraphicsManager.rf.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            vertexTransformSet = GraphicsManager.rf.CreateResourceSet(new ResourceSetDescription(GraphicsManager.sharedMeshUniform_VS, vertexUniformBuffer));
        }

        public Mesh(VertexStandard[] vertices, ushort[] indices) : this()
        {
            this.vertices = vertices;
            this.indices = indices;
        }

        public VertexStandard[] vertices
        {
            get { return _vertices; }
            set
            {
                _vertices = value;
                if (vertexBuffer != null)
                    vertexBuffer.Dispose();
                vertexBuffer = GraphicsManager.rf.CreateBuffer(
                                new BufferDescription(_vertices[0].VertexSize * (uint)_vertices.Length, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
                GraphicsManager.gd.UpdateBuffer(vertexBuffer, 0, _vertices);
            }
        }

        public ushort[] indices
        {
            get { return _indices; }
            set
            {
                _indices = value;
                if (indexBuffer != null)
                    indexBuffer.Dispose();
                indexBuffer = GraphicsManager.rf.CreateBuffer(
                             new BufferDescription(sizeof(ushort) * (uint)_indices.Length, BufferUsage.IndexBuffer | BufferUsage.Dynamic ));
                GraphicsManager.gd.UpdateBuffer(indexBuffer, 0, _indices);
            }
        }
	}

    public interface IVertex
    {
        public uint VertexSize { get; }
    }

    public struct VertexStandard : IVertex
    {
        public uint VertexSize => 44;

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

