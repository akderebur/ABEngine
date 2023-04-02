using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.ECS;
using Force.Crc32;
using Halak;
using Veldrid;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class MaterialMeta : AssetMeta
	{
        public PipelineAsset pipelineAsset { get; set; }

        internal string changedPropName;
        internal Vector4 changedData;

        public MaterialMeta() : base()
		{
		}

        public override void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            base.uniqueID = Guid.Parse(data["GUID"]);
            pipelineAsset = GraphicsManager.GetPipelineAssetByName(data["PipelineAsset"]);
        }

        public override void DrawMeta()
        {

        }

        public override JSerializable GetCopy(ref Entity newEntity)
        {
            throw new NotImplementedException();
        }

        public override JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("GUID", uniqueID.ToString());
            jObj.Put("PipelineAsset", pipelineAsset.ToString());

            return jObj.Build();
        }

        public override void SetReferences()
        {
            throw new NotImplementedException();
        }

        // Asset Serialization

        internal static void CreateMaterialAsset(string savePath)
        {
            uint fileHash = AssetCache.AddFileHash(savePath);
            
            PipelineMaterial mat = GraphicsManager.GetUberMaterial().GetCopy();
            mat.fPathHash = fileHash;

            File.WriteAllBytes(savePath, MaterialToRAW(mat));
            AssetCache.AddMaterial(mat, fileHash);
        }

        internal static byte[] MaterialToRAW(PipelineMaterial mat)
        {
            using (MemoryStream ms = new MemoryStream())
            using(BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(mat.pipelineAsset.ToString());
                bw.Write(mat.shaderProps.Count);
                foreach (ShaderProp prop in mat.shaderProps)
                {
                    unsafe
                    {
                        var span = new Span<byte>(prop.Bytes, 24);
                        bw.Write(span);
                    }
                }

                foreach (uint texHash in mat.texHashes)
                    bw.Write(texHash);

                return ms.ToArray();
            }
        }
    }
}

