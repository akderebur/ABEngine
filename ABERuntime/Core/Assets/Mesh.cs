using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Halak;
using Buffer = WGIL.Buffer;
using WGIL;

namespace ABEngine.ABERuntime.Core.Assets
{
	public class Mesh : Asset
	{
        public Vector3 boundsMin;
        public Vector3 boundsMax;

        internal Buffer vertexBuffer;
        internal Buffer indexBuffer;

        public Vector3[] positions { private get; set; }
        public Vector3[] normals { private get; set; }
        public Vector4[] tangents { private get; set; }
        public Vector2[] uv0 { private get; set; }
        public Vector4BInt[] boneIDs { private get; set; }
        public Vector4[] boneWeights { private get; set; }

        public Matrix4x4[] invBindMatrices { get; set; }

        public ushort[] indices { internal get; set; }
        public bool isSkinned { get; set; }

        public Mesh()
        {
        }

        public void UpdateMesh()
        {
            if (positions == null || positions.Length == 0)
                return;

            CalculateBounds();

            // Vertex Buffer
            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            // Fail-safes
            if (normals == null || normals.Length < positions.Length)
                normals = new Vector3[positions.Length];
            if (tangents == null || tangents.Length < positions.Length)
                tangents = new Vector4[positions.Length];
            if (uv0 == null || uv0.Length < positions.Length)
                uv0 = new Vector2[positions.Length];

            bool boneIDCond = boneIDs != null && boneIDs.Length == positions.Length;
            bool boneWCond = boneWeights != null && boneWeights.Length == positions.Length;
            if (isSkinned && boneIDCond && boneWCond)
            {
                vertexBuffer = Game.wgil.CreateBuffer(80 * positions.Length, BufferUsages.VERTEX | BufferUsages.COPY_DST);
                VertexSkinned[] vertices = new VertexSkinned[positions.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    VertexSkinned vertex = new VertexSkinned()
                    {
                        Position = positions[i],
                        Normal = normals[i],
                        Tangent = tangents[i],
                        UV = uv0[i],
                        BoneIds = boneIDs[i],
                        Weights = boneWeights[i]
                    };
                    vertices[i] = vertex;
                }

                Game.wgil.WriteBuffer(vertexBuffer, vertices);
            }
            else
            {
                isSkinned = false;
                vertexBuffer = Game.wgil.CreateBuffer(48 * positions.Length, BufferUsages.VERTEX | BufferUsages.COPY_DST);

                VertexStandard[] vertices = new VertexStandard[positions.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    VertexStandard vertex = new VertexStandard()
                    {
                        Position = positions[i],
                        Normal = normals[i],
                        Tangent = tangents[i],
                        UV = uv0[i],
                    };
                    vertices[i] = vertex;
                }

                Game.wgil.WriteBuffer(vertexBuffer, vertices);
            }

            // Index Buffer
            if (indexBuffer != null)
                indexBuffer.Dispose();
            indexBuffer = Game.wgil.CreateBuffer(sizeof(ushort) * indices.Length, BufferUsages.INDEX | BufferUsages.COPY_DST);
            Game.wgil.WriteBuffer(indexBuffer, indices);
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
            var order = positions.OrderBy(v => v.X);
            boundsMin.X = order.First().X;
            boundsMax.X = order.Last().X;

            order = positions.OrderBy(v => v.Y);
            boundsMin.Y = order.First().Y;
            boundsMax.Y = order.Last().Y;

            order = positions.OrderBy(v => v.Z);
            boundsMin.Z = order.First().Z;
            boundsMax.Z = order.Last().Z;
        }

        internal void CreateFromStandard(VertexStandard[] vertices, ushort[] indices)
        {
            if (vertices == null)
                return;
            isSkinned = false;

            positions = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                positions[i] = vertices[i].Position;

            CalculateBounds();

            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            vertexBuffer = Game.wgil.CreateBuffer(48 * positions.Length, BufferUsages.VERTEX | BufferUsages.COPY_DST).SetManualDispose(true);
            Game.wgil.WriteBuffer(vertexBuffer, vertices);

            // Index Buffer
            this.indices = indices;
            if (indexBuffer != null)
                indexBuffer.Dispose();
            indexBuffer = Game.wgil.CreateBuffer(sizeof(ushort) * this.indices.Length, BufferUsages.INDEX | BufferUsages.COPY_DST).SetManualDispose(true);
            Game.wgil.WriteBuffer(indexBuffer, indices);
        }
    }

    public interface IVertex
    {
        public uint vertexSize { get; }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VertexStandard : IVertex
    {
        public uint vertexSize => 48;

        [FieldOffset(0)]  public Vector3 Position;
        [FieldOffset(12)] public Vector3 Normal;
        [FieldOffset(24)] public Vector2 UV;
        [FieldOffset(32)] public Vector4 Tangent;

        public VertexStandard(Vector3 position, Vector3 normal, Vector2 uv) : this(position, normal, uv, Vector4.Zero) { }
        public VertexStandard(Vector3 position, Vector3 normal, Vector2 uv, Vector4 tangent)
        {
            Position = position;
            Normal = normal;
            UV = uv;
            Tangent = tangent;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VertexSkinned : IVertex
    {
        public uint vertexSize => 80;

        [FieldOffset(0)]  public Vector3 Position;
        [FieldOffset(12)] public Vector3 Normal;
        [FieldOffset(24)] public Vector2 UV;
        [FieldOffset(32)] public Vector4 Tangent;
        [FieldOffset(48)] public Vector4BInt BoneIds;
        [FieldOffset(64)] public Vector4 Weights;

        public VertexSkinned(Vector3 position, Vector3 normal, Vector2 uv) : this(position, normal, uv, Vector4.Zero, Vector4BInt.Zero, new Vector4(1, 0, 0, 0)) { }
        public VertexSkinned(Vector3 position, Vector3 normal, Vector2 uv, Vector4 tangent, Vector4BInt boneIds, Vector4 weights)
        {
            Position = position;
            Normal = normal;
            UV = uv;
            Tangent = tangent;
            BoneIds = boneIds;
            Weights = weights;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4BInt
    {
        public int B0;
        public int B1;
        public int B2;
        public int B3;

        public static Vector4BInt Zero = new Vector4BInt()
        {
            B0 = 0,
            B1 = 0,
            B2 = 0,
            B3 = 0
        };
    }
}

