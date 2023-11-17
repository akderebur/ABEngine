using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ABEngine.ABERuntime.Core.Assets;
using Halak;
using Buffer = WGIL.Buffer;
using WGIL;

namespace ABEngine.ABERuntime
{
	public class Mesh : Asset
	{
        // TODO Switch to vertex interface
        private VertexStandard[] _vertices;
        private ushort[] _indices;

        public Vector3 boundsMin;
        public Vector3 boundsMax;

        internal Buffer vertexBuffer;
        internal Buffer indexBuffer;

        public Mesh()
        {
        }

        internal Mesh(uint hash) : this()
        {
            base.fPathHash = hash;
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
                CalculateBounds();
                if (vertexBuffer != null)
                    vertexBuffer.Dispose();

                vertexBuffer = Game.wgil.CreateBuffer((int)_vertices[0].VertexSize * _vertices.Length, BufferUsages.VERTEX | BufferUsages.COPY_DST);
                Game.wgil.WriteBuffer(vertexBuffer, _vertices);
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

                indexBuffer = Game.wgil.CreateBuffer(sizeof(ushort) * indices.Length, BufferUsages.INDEX | BufferUsages.COPY_DST);
                Game.wgil.WriteBuffer(indexBuffer, _indices);
            }
        }

        internal override JValue SerializeAsset()
        {
            JsonObjectBuilder assetEnt = new JsonObjectBuilder(200);
            assetEnt.Put("TypeID", 3);
            assetEnt.Put("FileHash", (long)fPathHash);
            return assetEnt.Build();
        }

        void CalculateBounds()
        {
            var order = vertices.OrderBy(v => v.Position.X);
            boundsMin.X = order.First().Position.X;
            boundsMax.X = order.Last().Position.X;

            order = vertices.OrderBy(v => v.Position.Y);
            boundsMin.Y = order.First().Position.Y;
            boundsMax.Y = order.Last().Position.Y;

            order = vertices.OrderBy(v => v.Position.Z);
            boundsMin.Z = order.First().Position.Z;
            boundsMax.Z = order.Last().Position.Z;
        }
    }

    public interface IVertex
    {
        public uint VertexSize { get; }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VertexStandard : IVertex
    {
        public uint VertexSize => 44;

        [FieldOffset(0)] public Vector3 Position;
        [FieldOffset(12)] public Vector3 Normal;
        [FieldOffset(24)] public Vector2 UV;
        [FieldOffset(32)] public Vector3 Tangent;

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

