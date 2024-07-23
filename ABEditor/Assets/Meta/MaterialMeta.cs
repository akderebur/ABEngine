using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Core.Assets;
using Halak;

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
            pipelineAsset = Graphics.GetUberMaterial().pipelineAsset;
		}

        public override JValue Serialize()
        {
            base.Serialize();
            jObj.Put("PipelineAsset", pipelineAsset.name);
            return jObj.Build();
        }

        public override void Deserialize(string json)
        {
            base.Deserialize(json);
            pipelineAsset = AssetCache.CreatePipelineAsset(data["PipelineAsset"]);
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
            PipelineMaterial mat = Graphics.GetUberMaterial().GetCopy();
            mat.fPathHash = fileHash;

            File.WriteAllBytes(savePath, MaterialToRAW(mat));
            AssetCache.AddAsset(mat, assetPath);
        }


        internal static byte[] MaterialToRAW(PipelineMaterial mat)
        {
            using (MemoryStream ms = new MemoryStream())
            using(BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(mat.name);
                bw.Write(mat.pipelineAsset.name);

                // Counts dummy
                long countPos = bw.BaseStream.Position;
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);

                int texCount = 0;
                int vecCount = 0;
                int floatCount = 0;
                
                // Write Textures
                foreach (var texName in mat.pipelineAsset.GetTextureNames())
                {
                    bw.Write(texName);
                    bw.Write(0);
                    bw.Write(false);
                    texCount++;
                }

                var propNames = mat.pipelineAsset.GetPropNames();
                
                // Write Vectors
                for (int i = 0; i < mat.shaderProps.Count; i++)
                {
                    ShaderProp prop = mat.shaderProps[i];
                    if(prop.SizeInBytes <= 4)
                        continue;

                    string propName = propNames[i];
                    bw.Write(propName);
                    
                    unsafe
                    {
                        var span = new Span<byte>(prop.Bytes, 16);
                        bw.Write(span);
                    }

                    vecCount++;
                }
                
                // Write Floats
                for (int i = 0; i < mat.shaderProps.Count; i++)
                {
                    ShaderProp prop = mat.shaderProps[i];
                    if(prop.SizeInBytes > 4)
                        continue;

                    string propName = propNames[i];
                    bw.Write(propName);
                    
                    bw.Write(prop.Float1);
                    floatCount++;
                }

                bw.BaseStream.Position = countPos;
                bw.Write(texCount);
                bw.Write(vecCount);
                bw.Write(floatCount);

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

