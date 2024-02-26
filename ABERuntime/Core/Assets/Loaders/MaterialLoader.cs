using System;
using System.IO;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Assets
{
	internal class MaterialLoader : AssetLoader
	{
        internal override PipelineMaterial LoadAssetRAW(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                string matName = br.ReadString();
                string pipelineName = br.ReadString();

                PipelineAsset pipelineAsset = AssetCache.CreatePipelineAsset(pipelineName);
                PipelineMaterial mat = pipelineAsset.GetDefaultMaterial().GetCopy();
                mat.name = matName;

                // Shader props
                int textureCount = br.ReadInt32();
                int vectorCount = br.ReadInt32();
                int floatCount = br.ReadInt32();

                // Textures
                for (int i = 0; i < textureCount; i++)
                {
                    string propname = br.ReadString();
                    uint texHash = br.ReadUInt32();
                    bool isLinear = br.ReadBoolean();

                    Texture2D tex2d = AssetCache.GetOrCreateTexture2D(null, null, Vector2.Zero, texHash, isLinear);
                    mat.SetTexture(propname, tex2d);
                }

                // Vectors
                for (int i = 0; i < vectorCount; i++)
                {
                    string propname = br.ReadString();
                    Vector4 vector = new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    mat.SetVector4(propname, vector);
                }

                // Floats
                for (int i = 0; i < floatCount; i++)
                {
                    string propname = br.ReadString();
                    float value = br.ReadSingle();
                    mat.SetFloat(propname, value);
                }

                return mat;
            }
        }
    }
}

