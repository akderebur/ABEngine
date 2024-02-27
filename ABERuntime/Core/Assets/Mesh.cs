using System;
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

        public Vector3[] Positions { private get; set; }
        public Vector3[] Normals { private get; set; }
        public Vector4[] Tangents { private get; set; }
        public Vector2[] UV0 { private get; set; }
        public Vector4BInt[] BoneIDs { private get; set; }
        public Vector4[] BoneWeights { private get; set; }

        public Matrix4x4[] invBindMatrices { get; set; }

        public ushort[] Indices { internal get; set; }
        public bool IsSkinned { get; set; }

        public Mesh()
        {
        }

        public void UpdateMesh()
        {
            if (Positions == null || Positions.Length == 0)
                return;

            CalculateBounds();

            // Vertex Buffer
            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            // Fail-safes
            if (Normals == null || Normals.Length < Positions.Length)
                Normals = new Vector3[Positions.Length];
            if (Tangents == null || Tangents.Length < Positions.Length)
                Tangents = new Vector4[Positions.Length];
            if (UV0 == null || UV0.Length < Positions.Length)
                UV0 = new Vector2[Positions.Length];

            bool boneIDCond = BoneIDs != null && BoneIDs.Length == Positions.Length;
            bool boneWCond = BoneWeights != null && BoneWeights.Length == Positions.Length;
            if (IsSkinned && boneIDCond && boneWCond)
            {
                vertexBuffer = Game.wgil.CreateBuffer(80 * Positions.Length, BufferUsages.VERTEX | BufferUsages.COPY_DST);
                VertexSkinned[] vertices = new VertexSkinned[Positions.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    VertexSkinned vertex = new VertexSkinned()
                    {
                        Position = Positions[i],
                        Normal = Normals[i],
                        Tangent = Tangents[i],
                        UV = UV0[i],
                        BoneIds = BoneIDs[i],
                        Weights = BoneWeights[i]
                    };
                    vertices[i] = vertex;
                }

                Game.wgil.WriteBuffer(vertexBuffer, vertices);
            }
            else
            {
                IsSkinned = false;
                vertexBuffer = Game.wgil.CreateBuffer(48 * Positions.Length, BufferUsages.VERTEX | BufferUsages.COPY_DST);

                VertexStandard[] vertices = new VertexStandard[Positions.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    VertexStandard vertex = new VertexStandard()
                    {
                        Position = Positions[i],
                        Normal = Normals[i],
                        Tangent = Tangents[i],
                        UV = UV0[i],
                    };
                    vertices[i] = vertex;
                }

                Game.wgil.WriteBuffer(vertexBuffer, vertices);
            }

            // Index Buffer
            if (indexBuffer != null)
                indexBuffer.Dispose();
            indexBuffer = Game.wgil.CreateBuffer(sizeof(ushort) * Indices.Length, BufferUsages.INDEX | BufferUsages.COPY_DST);
            Game.wgil.WriteBuffer(indexBuffer, Indices);
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
            var order = Positions.OrderBy(v => v.X);
            boundsMin.X = order.First().X;
            boundsMax.X = order.Last().X;

            order = Positions.OrderBy(v => v.Y);
            boundsMin.Y = order.First().Y;
            boundsMax.Y = order.Last().Y;

            order = Positions.OrderBy(v => v.Z);
            boundsMin.Z = order.First().Z;
            boundsMax.Z = order.Last().Z;
        }

        internal void CreateFromStandard(VertexStandard[] vertices, ushort[] indices)
        {
            if (vertices == null)
                return;
            IsSkinned = false;

            Positions = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                Positions[i] = vertices[i].Position;

            CalculateBounds();

            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            vertexBuffer = Game.wgil.CreateBuffer(48 * Positions.Length, BufferUsages.VERTEX | BufferUsages.COPY_DST).SetManualDispose(true);
            Game.wgil.WriteBuffer(vertexBuffer, vertices);

            // Index Buffer
            Indices = indices;
            if (indexBuffer != null)
                indexBuffer.Dispose();
            indexBuffer = Game.wgil.CreateBuffer(sizeof(ushort) * Indices.Length, BufferUsages.INDEX | BufferUsages.COPY_DST).SetManualDispose(true);
            Game.wgil.WriteBuffer(indexBuffer, indices);
        }
    }

    public interface IVertex
    {
        public uint VertexSize { get; }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct VertexStandard : IVertex
    {
        public uint VertexSize => 48;

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
        public uint VertexSize => 80;

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

