using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Core.Assets;
using Halak;
using Veldrid;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class MaterialMeta : AssetMeta
	{
        public PipelineAsset pipelineAsset { get; set; }

        internal string changedPropName;
        internal Vector4 changedData;
        internal PipelineAsset changedPipeline;

        public MaterialMeta() : base()
		{
            pipelineAsset = GraphicsManager.GetUberMaterial().pipelineAsset;
		}

        public override JValue Serialize()
        {
            base.Serialize();
            jObj.Put("PipelineAsset", pipelineAsset.ToString());
            return jObj.Build();
        }

        public override void Deserialize(string json)
        {
            base.Deserialize(json);
            pipelineAsset = GraphicsManager.GetPipelineAssetByName(data["PipelineAsset"]);
        }


        public override void SetReferences()
        {
            throw new NotImplementedException();
        }


        public override JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }

        public override void DrawMeta()
        {

        }

        // Asset Serialization

        internal static void CreateMaterialAsset(string savePath)
        {
            string assetPath = savePath.Replace(Game.AssetPath, "");
            uint fileHash = assetPath.ToHash32();
            PipelineMaterial mat = GraphicsManager.GetUberMaterial().GetCopy();
            mat.fPathHash = fileHash;

            File.WriteAllBytes(savePath, MaterialToRAW(mat));
            AssetCache.AddMaterial(mat, assetPath);
        }


        internal static byte[] MaterialToRAW(PipelineMaterial mat)
        {
            using (MemoryStream ms = new MemoryStream())
            using(BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(mat.name);
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

        public override PipelineMaterial CreateAssetBinding()
        {
            PipelineMaterial mat = AssetCache.CreateMaterial(base.fPath);
            mat.name = Path.GetFileNameWithoutExtension(base.fPath);
            return mat;
        }
    }
}

