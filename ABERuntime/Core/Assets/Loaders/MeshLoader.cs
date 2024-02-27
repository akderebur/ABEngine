using System;
using Box2D.NetStandard.Collision.Shapes;
using System.IO;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Assets
{
	internal class MeshLoader : AssetLoader
	{
        internal override Mesh LoadAssetRAW(byte[] data)
        {
            Mesh mesh = new Mesh();
            using (MemoryStream fs = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int vertC = br.ReadInt32();
                int indC = br.ReadInt32();

                int compC = br.ReadByte();

                for (int vc = 0; vc < compC; vc++)
                {
                    char vcID = br.ReadChar();
                    switch (vcID)
                    {
                        case 'P':
                            Vector3[] poses = new Vector3[vertC];
                            for (int i = 0; i < vertC; i++)
                                poses[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            mesh.Positions = poses;
                            break;
                        case 'U':
                            Vector2[] uv = new Vector2[vertC];
                            for (int i = 0; i < vertC; i++)
                                uv[i] = new Vector2(br.ReadSingle(), br.ReadSingle());
                            mesh.UV0 = uv;
                            break;
                        case 'N':
                            Vector3[] normals = new Vector3[vertC];
                            for (int i = 0; i < vertC; i++)
                                normals[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            mesh.Normals = normals;
                            break;
                        case 'T':
                            Vector4[] tangents = new Vector4[vertC];
                            for (int i = 0; i < vertC; i++)
                                tangents[i] = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                            mesh.Tangents = tangents;
                            break;
                        case 'B':
                            mesh.IsSkinned = true;
                            Vector4BInt[] boneIds = new Vector4BInt[vertC];
                            Vector4[] boneWeights = new Vector4[vertC];
                            for (int i = 0; i < vertC; i++)
                            {
                                Vector4BInt idVec = new Vector4BInt();
                                Vector4 weightVec = new Vector4();

                                idVec.B0 = br.ReadUInt16();
                                weightVec.X = br.ReadSingle();

                                idVec.B1 = br.ReadUInt16();
                                weightVec.Y = br.ReadSingle();

                                idVec.B2 = br.ReadUInt16();
                                weightVec.Z = br.ReadSingle();

                                idVec.B3 = br.ReadUInt16();
                                weightVec.W = br.ReadSingle();

                                boneIds[i] = idVec;
                                boneWeights[i] = weightVec;
                            }
                            mesh.BoneIDs = boneIds;
                            mesh.BoneWeights = boneWeights;
                            break;
                        case 'M':
                            int matrixCount = br.ReadInt32();
                            Matrix4x4[] invBindMatrices = new Matrix4x4[matrixCount];
                            for (int m = 0; m < matrixCount; m++)
                            {
                                Matrix4x4 invBindMatrix = new Matrix4x4(
                                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
                                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
                                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(),
                                br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                                invBindMatrices[m] = invBindMatrix;
                            }
                            mesh.invBindMatrices = invBindMatrices;
                            break;
                        default:
                            break;
                    }
                }

                ushort[] indices = new ushort[indC];
                for (int i = 0; i < indC; i++)
                    indices[i] = br.ReadUInt16();
                mesh.Indices = indices;
            }

            mesh.UpdateMesh();
            return mesh;
        }
    }
}