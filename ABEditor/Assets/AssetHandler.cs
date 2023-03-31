using System;
using System.IO;
using ABEngine.ABERuntime;
using ABEngine.ABEditor.Assets.Meta;
using System.Collections.Generic;
using Veldrid.ImageSharp;
using System.Numerics;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABEditor.Assets
{
	public static class AssetHandler
	{
		static string AssetsPath;
		static Dictionary<string, AssetMeta> metaDict = new Dictionary<string, AssetMeta>();
		static Dictionary<TextureMeta, Texture2D> sceneTextures = new Dictionary<TextureMeta, Texture2D>();

		public static void InitFiles(string assetsPath)
		{
            AssetsPath = assetsPath;
			var files = Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories);

			foreach (var file in files)
			{
				AssetMeta sharedMeta = null;
				string extOrg = Path.GetExtension(file);
                string ext = extOrg.ToLower();
				string metaFullPath  = file.Replace(ext, ".abmeta");
                if (ext.Equals(".png"))
				{
					TextureMeta meta = new TextureMeta();
					sharedMeta = meta;

					if (File.Exists(metaFullPath))
						meta.Deserialize(File.ReadAllText(metaFullPath));
					else
					{
						ImageSharpTexture img = new ImageSharpTexture(file);
						meta.imageSize = new Vector2(img.Width, img.Height);
						File.WriteAllText(metaFullPath, meta.Serialize().Serialize());
					}

					metaDict.Add(file.Replace(assetsPath, ""), meta);
					meta.refreshEvent += RefreshTextureAsset;

				}
				else if(ext.Equals(".abmat"))
				{
					MaterialMeta meta = new MaterialMeta();
					sharedMeta = meta;

                    if (File.Exists(metaFullPath))
                        meta.Deserialize(File.ReadAllText(metaFullPath));
                    else
                    {
                        File.WriteAllText(metaFullPath, meta.Serialize().Serialize());
                    }
                }

				if(sharedMeta != null)
				{
					sharedMeta.metaAssetPath = metaFullPath.Replace(assetsPath, "");
                }
			}
		}

		public static AssetMeta GetMeta(string file)
		{
			if (metaDict.ContainsKey(file))
				return metaDict[file];
			else
				return null;
		}

		public static Texture2D GetTextureBinding(TextureMeta texMeta, string texPath)
		{
			if (sceneTextures.ContainsKey(texMeta))
				return sceneTextures[texMeta];
			else
			{
                Texture2D tex = AssetCache.CreateTexture2D(texPath, texMeta.sampler, texMeta.spriteSize);
				sceneTextures.Add(texMeta, tex);
				return tex;
            }
        }

		public static void SaveMeta(AssetMeta meta)
		{
			File.WriteAllText(AssetsPath + meta.metaAssetPath, meta.Serialize().Serialize());
		}

		private static void RefreshTextureAsset(AssetMeta assetMeta, string assetPath)
		{
			TextureMeta texMeta = assetMeta as TextureMeta;
			if (sceneTextures.ContainsKey(texMeta))
			{
				Texture2D oldTex = sceneTextures[texMeta];
				Texture2D newTex = AssetCache.CreateTexture2D(assetPath, texMeta.sampler, texMeta.spriteSize);

				// Find all entities with this texture
				var query = Game.GameWorld.CreateQuery().Has<Sprite>();
				query.Foreach((ref Sprite sprite) =>
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

				sceneTextures.Remove(texMeta);
				sceneTextures.Add(texMeta, newTex);
			}
		}
	}
}


