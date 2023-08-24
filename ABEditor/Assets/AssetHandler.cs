using System;
using System.IO;
using ABEngine.ABERuntime;
using ABEngine.ABEditor.Assets.Meta;
using System.Collections.Generic;
using Veldrid.ImageSharp;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using System.Xml;
using Halak;
using ABEngine.ABERuntime.Core.Assets;
using System.Linq;
using Arch.Core;

namespace ABEngine.ABEditor.Assets
{
	public static class AssetHandler
	{
		static string AssetsPath;
		static Dictionary<string, AssetMeta> metaDict = new Dictionary<string, AssetMeta>();
        static Dictionary<AssetMeta, Asset> sceneAssets = new Dictionary<AssetMeta, Asset>();

        //static Dictionary<TextureMeta, Texture2D> sceneTextures = new Dictionary<TextureMeta, Texture2D>();
        //static Dictionary<MaterialMeta, PipelineMaterial> sceneMaterials = new Dictionary<MaterialMeta, PipelineMaterial>();

        static Dictionary<Guid, string> guidToMeta = new Dictionary<Guid, string>();
		static List<Guid> loadedGuids = new List<Guid>();
		static HashSet<string> assetExts = new HashSet<string>()
		{
			".png",
			".abmat",
			".abprefab",
			".abmesh"
		};

		static int guidMagic = 1230324289; // ABUI

        public static void InitFiles(string assetsPath)
		{
            AssetsPath = assetsPath;
			var files = Directory.GetFiles(AssetsPath, "*", SearchOption.AllDirectories);
			var metaFiles = Directory.GetFiles(AssetsPath, "*.abmeta", SearchOption.AllDirectories);

			Dictionary<uint, uint> updatedAssetHashes = new Dictionary<uint, uint>();

            // Map guid to metas
            foreach (var metaFile in metaFiles)
			{
                JValue data = JValue.Parse(File.ReadAllText(metaFile));
				Guid guid = Guid.Parse(data["GUID"]);

				if(!guidToMeta.ContainsKey(guid))
					guidToMeta.Add(guid, metaFile.ToCommonPath().Replace(AssetsPath, ""));
            }

            foreach (var file in files)
			{
                string ext = Path.GetExtension(file);

                if (assetExts.Contains(ext.ToLower())) // Viable asset file
				{
                    string fileAssetPath = file.ToCommonPath().Replace(AssetsPath, "");
					string metaAssetPath = fileAssetPath + ".abmeta";

                    Guid assetGuid = Guid.Empty;

                    using (FileStream fs = new FileStream(file, FileMode.Open))
					using (BinaryReader br = new BinaryReader(fs))
					{
						fs.Position = fs.Length - 20;
						int magic = br.ReadInt32();
						if (magic == guidMagic)
						{
							byte[] guidBytes = br.ReadBytes(16);
							assetGuid = new Guid(guidBytes);
						}
					}

                    if (guidToMeta.ContainsKey(assetGuid) && !loadedGuids.Contains(assetGuid))
                    {
						string oldMetaAssetPath = guidToMeta[assetGuid];
						AssetMeta meta = new DummyMeta();
						meta.Deserialize(File.ReadAllText(assetsPath + oldMetaAssetPath));

						if (meta.fPath.Equals(fileAssetPath)) // File exists without change
						{
						    meta = CreateMetaFromExtension(ext);
							meta.metaAssetPath = metaAssetPath;
							meta.Deserialize(File.ReadAllText(assetsPath + metaAssetPath));
							metaDict.Add(fileAssetPath, meta);
							loadedGuids.Add(meta.uniqueID);
						}
						else // File moved or renamed
						{
							updatedAssetHashes.Add(meta.fPath.ToHash32(), fileAssetPath.ToHash32());
							HandleMovedFile(oldMetaAssetPath, meta.fPath, fileAssetPath);
						}
                    }
                    else
					{
						HandleNewFile(fileAssetPath);
                    }

                }
			}

            // Replace hashes in scenes
            // TODO: Fix horrible string replacement
            var sceneFiles = Directory.GetFiles(AssetsPath, "*.abscene", SearchOption.AllDirectories);

            foreach (var sceneFile in sceneFiles)
			{
				string sceneData = File.ReadAllText(sceneFile);

				foreach (var updateHashKV in updatedAssetHashes)
				{
					string toFind = "\"FileHash\":" + updateHashKV.Key;
					string toReplace = "\"FileHash\":" + updateHashKV.Value;
					if (sceneData.Contains(toFind))
						sceneData = sceneData.Replace(toFind, toReplace);
                }

				File.WriteAllText(sceneFile, sceneData);
            }
		}

		static void UpdateSceneHash(uint oldHash, uint newHash)
		{
			string toFind = "\"FileHash\":" + oldHash;
			string toReplace = "\"FileHash\":" + newHash;

			// Replace hashes in scenes
			// TODO: Fix horrible string replacement
			var sceneFiles = Directory.GetFiles(AssetsPath, "*.abscene", SearchOption.AllDirectories);

			foreach (var sceneFile in sceneFiles)
			{
				string sceneData = File.ReadAllText(sceneFile);

				if (sceneData.Contains(toFind))
					sceneData = sceneData.Replace(toFind, toReplace);

				File.WriteAllText(sceneFile, sceneData);
			}
		}

		public static void ResetScene()
		{
			sceneAssets.Clear();
		}

		private static AssetMeta CreateMetaFromExtension(string ext)
		{
			AssetMeta meta = null;
			switch (ext.ToLower())
			{
				case ".png":
					meta = new TextureMeta();
					meta.refreshEvent += RefreshTextureAsset;
					break;
				case ".abmat":
                    meta = new MaterialMeta();
                    meta.refreshEvent += RefreshMaterialAsset;
					break;
				case ".abprefab":
					meta = new PrefabMeta();
					//meta.refreshEvent +=
					break;
				case ".abmesh":
					meta = new MeshMeta();
					break;
            }

			return meta;
		}

		// Public New/Moved File Methods
		public static void NewFileCreated(string fileAssetPath)
		{
            string ext = Path.GetExtension(fileAssetPath);
			if (!assetExts.Contains(ext.ToLower()))
				return;
				
            HandleNewFile(fileAssetPath);
		}

        public static void FileMoved(string oldMetaAssetPath, string oldFileAssetPath, string newFileAssetPath)
        {
            string ext = Path.GetExtension(newFileAssetPath);
            if (!assetExts.Contains(ext.ToLower()))
                return;

            HandleMovedFile(oldMetaAssetPath, oldFileAssetPath, newFileAssetPath);
        }

        public static void FileDeleted(string fileAssetPath)
        {
            string ext = Path.GetExtension(fileAssetPath);
            if (!assetExts.Contains(ext.ToLower()))
                return;

            if(metaDict.ContainsKey(fileAssetPath))
				metaDict.Remove(fileAssetPath);
        }

        private static void HandleNewFile(string fileAssetPath)
		{
			string fullPath = AssetsPath + fileAssetPath;
			if (!File.Exists(fullPath) || metaDict.ContainsKey(fileAssetPath))
				return;

			string ext = Path.GetExtension(fileAssetPath);
			string metaAssetPath = fileAssetPath + ".abmeta";

            AssetMeta meta = CreateMetaFromExtension(ext);
			meta.fPath = fileAssetPath;
			meta.metaAssetPath = metaAssetPath;
            meta.fPathHash = fileAssetPath.ToHash32();

            if (meta.GetType() == typeof(TextureMeta))
            {
                ImageSharpTexture img = new ImageSharpTexture(fullPath);
                ((TextureMeta)meta).imageSize = new Vector2(img.Width, img.Height);
            }

            File.WriteAllText(Game.AssetPath + metaAssetPath, meta.Serialize().Serialize());

			// Write GUID to the end of file
			using (FileStream fs = new FileStream(fullPath, FileMode.Open))
			using (BinaryReader br = new BinaryReader(fs))
			using (BinaryWriter bw = new BinaryWriter(fs))
			{
				fs.Position = fs.Length - 20;
				int magic = br.ReadInt32();
				if (magic == guidMagic) // Existing ABE GUID Tag, duplicate file maybe?
				{
                    bw.Write(meta.uniqueID.ToByteArray());
                }
                else
				{
					fs.Position = fs.Length;
					bw.Write(guidMagic);
					bw.Write(meta.uniqueID.ToByteArray());
				}
			}

			metaDict.Add(fileAssetPath, meta);
			guidToMeta.Add(meta.uniqueID, metaAssetPath);
			loadedGuids.Add(meta.uniqueID);

			meta.MetaCreated();
        }

        private static void HandleMovedFile(string oldMetaAssetPath, string oldFileAssetPath, string newFileAssetPath)
		{
            string ext = Path.GetExtension(oldFileAssetPath);

			string metaAssetPath = newFileAssetPath + ".abmeta";

            string oldFullPath = AssetsPath + oldFileAssetPath;
			string oldMetaFullPath = AssetsPath + oldMetaAssetPath;

			string newFullPath = AssetsPath + newFileAssetPath;
			string newMetaFullPath = newFullPath + ".abmeta";

			uint oldHash = oldFileAssetPath.ToHash32();
			uint newHash = newFileAssetPath.ToHash32();

            AssetMeta meta = null;
			if (metaDict.ContainsKey(oldFileAssetPath)) // Editor runtime
			{
				meta = metaDict[oldFileAssetPath];
				metaDict.Remove(oldFileAssetPath);

                if (sceneAssets.ContainsKey(meta))
					sceneAssets[meta].fPathHash = newFileAssetPath.ToHash32();
            }
			else
			{
				meta = CreateMetaFromExtension(ext);
				if (File.Exists(oldMetaFullPath))
					meta.Deserialize(File.ReadAllText(oldMetaFullPath));
            }

            meta.metaAssetPath = metaAssetPath;
			meta.fPath = newFileAssetPath;
			meta.fPathHash = newHash;

			if (guidToMeta.ContainsKey(meta.uniqueID))
				guidToMeta.Remove(meta.uniqueID);

			if (metaDict.ContainsKey(newFileAssetPath)) // Ideally shouldn't happen. Deleted file not removed?
				metaDict[newFileAssetPath] = meta;
			else
				metaDict.Add(newFileAssetPath, meta);

            if (File.Exists(oldMetaFullPath))
				File.Move(oldMetaFullPath, newMetaFullPath);

			// Overwrite either way
			File.WriteAllText(newMetaFullPath, meta.Serialize().Serialize());
			guidToMeta.Add(meta.uniqueID, metaAssetPath);
            loadedGuids.Add(meta.uniqueID);

			AssetCache.UpdateAsset(oldHash, newHash, newFileAssetPath);
			UpdateSceneHash(oldHash, newHash);
        }

        public static void CreateMaterial(string file)
		{
			MaterialMeta.CreateMaterialAsset(file);
        }

		public static AssetMeta GetMeta(string file)
		{
			if (metaDict.ContainsKey(file))
				return metaDict[file];
			else
				return null;
		}

        public static AssetMeta GetMeta(uint hash)
        {
			return metaDict.Values.FirstOrDefault(e => e.fPathHash == hash);
        }

        public static Asset GetAssetBinding(AssetMeta meta)
		{
			if (sceneAssets.ContainsKey(meta))
				return sceneAssets[meta];
			else
			{
				var asset = meta.CreateAssetBinding();
                sceneAssets.Add(meta, asset);
				return asset;
			}
		}

        public static void SaveMeta(AssetMeta meta)
		{
			if(meta is MaterialMeta)
			{
				// Save material properties
				PipelineMaterial mat = GetAssetBinding(meta) as PipelineMaterial;
				if(mat != null)
				{
					byte[] matData = MaterialMeta.MaterialToRAW(mat);
					using(FileStream fs = new FileStream(AssetsPath + meta.fPath, FileMode.Create))
					using(BinaryWriter bw = new BinaryWriter(fs))
					{
						bw.Write(matData);
						bw.Write(AssetCache.guidMagic);
						bw.Write(meta.uniqueID.ToByteArray());
					}
				}
			}

			File.WriteAllText(AssetsPath + meta.metaAssetPath, meta.Serialize().Serialize());
		}

		private static void RefreshMaterialAsset(AssetMeta assetMeta)
		{
			MaterialMeta matMeta = assetMeta as MaterialMeta;
			if (sceneAssets.ContainsKey(matMeta))
			{
				PipelineMaterial mat = sceneAssets[matMeta] as PipelineMaterial;
				if(matMeta.changedPropName != null)
					mat.SetVector4(matMeta.changedPropName, matMeta.changedData);
				if (matMeta.changedPipeline != null)
					mat.ChangePipeline(matMeta.changedPipeline);
			}
		}

        private static void RefreshTextureAsset(AssetMeta assetMeta)
		{
			TextureMeta texMeta = assetMeta as TextureMeta;
			if (sceneAssets.ContainsKey(texMeta))
			{
				Texture2D oldTex = sceneAssets[texMeta] as Texture2D;
				Texture2D newTex = AssetCache.CreateTexture2D(assetMeta.fPath, texMeta.sampler, texMeta.spriteSize);

                // Find all entities with this texture
                var query = new QueryDescription().WithAll<Sprite>();
                Game.GameWorld.Query(in query, (ref Sprite sprite) =>
				{
					if (sprite.texture == oldTex)
					{
						if (oldTex.isSpriteSheet)
						{
							int sprID = sprite.GetSpriteID();
							sprite.SetTexture(newTex);
							sprite.SetSpriteID(sprID);
						}
						else
                            sprite.SetTexture(newTex);
                    }
				});

				sceneAssets.Remove(texMeta);
                sceneAssets.Add(texMeta, newTex);
			}
		}

	}
}


