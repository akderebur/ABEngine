using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.IO.Compression;
using WGIL;
using Halak;
using ABEngine.ABERuntime.ECS;
using Arch.Core;
using ABEngine.ABERuntime.Components;
using Arch.Core.Extensions;

namespace ABEngine.ABERuntime.Core.Assets
{
    class AssetEntry
    {
        public uint hash { get; set; }
        public int size { get; set; }
        public long offset { get; set; }
    }

    public static class AssetCache
    {
        internal static int guidMagic = 1230324289; // ABUI

        // Debug - Raw
        private static readonly Dictionary<string, ImageSharpTexture> s_images_debug = new Dictionary<string, ImageSharpTexture>();
        private static readonly Dictionary<ImageSharpTexture, Texture> s_textures_debug = new Dictionary<ImageSharpTexture, Texture>();

        // Debug - Hash to path
        private static readonly Dictionary<uint, string> hashToFName = new Dictionary<uint, string>();
        private static readonly Dictionary<string, uint> pipelineNameToHash = new Dictionary<string, uint>();
        private static readonly Dictionary<string, uint> sceneNameToHash = new Dictionary<string, uint>();

        // Release - ABPK
        private static readonly Dictionary<uint, Texture> s_textures = new Dictionary<uint, Texture>();
        private static readonly Dictionary<Texture, TextureView> s_textureViews = new Dictionary<Texture, TextureView>();

        // ABE Types
        private static readonly List<Texture2D> s_texture2ds = new List<Texture2D>();
        private static readonly List<SpriteClip> s_clips = new List<SpriteClip>();

        private static Texture2D defTexture = null;

        // Serialize
        static Dictionary<uint, AssetEntry> assetDictPK;
        static Dictionary<uint, Asset> assetDict;

        // Scene specific serialize
        static List<Asset> sceneAssets = new List<Asset>();

        static BinaryReader pr; // ABPK reader

        // Loaders
        static Dictionary<Type, AssetLoader> assetLoaders;

        public static void InitAssetCache()
        {
            assetDict = new Dictionary<uint, Asset>();

            // Asset Loaders
            assetLoaders = new()
            {
                { typeof(PipelineMaterial), new MaterialLoader() },
                { typeof(Mesh), new MeshLoader() },
                { typeof(PrefabAsset), new PrefabLoader() },
            };

            LoadDefaultMaterials();

            string commonAssetPath = Game.AssetPath;

            if (Game.debug)
            {
                var fileEnum = Directory.EnumerateFiles(commonAssetPath, "*.*", SearchOption.AllDirectories);

                // Get hash override assets
                var files = fileEnum.Where(s => s.ToLower().EndsWith(".hoa"));
                foreach (var file in files)
                {
                    string localPath = file.ToCommonPath().Replace(commonAssetPath, "");
                    string hashStr = Path.GetFileNameWithoutExtension(localPath);
                    hashToFName.Add(Convert.ToUInt32(hashStr, 16), localPath);
                }

                var hashParser = (string extension) =>
                {
                    var files = fileEnum.Where(s => s.ToLower().EndsWith(extension));
                    foreach (var file in files)
                    {
                        string localPath = file.ToCommonPath().Replace(commonAssetPath, "");
                        hashToFName.Add(localPath.ToHash32(), localPath);
                    }
                };

                hashParser(".png");
                hashParser(".abmat");
                hashParser(".abprefab");
                hashParser(".abmesh");
                hashParser(".abmodel");

                // Get user pipelines
                files = fileEnum.Where(s => s.ToLower().EndsWith(".abpipeline"));
                foreach (var file in files)
                {
                    string localPath = file.ToCommonPath().Replace(commonAssetPath, "");
                    uint hash = localPath.ToHash32();
                    hashToFName.Add(hash, localPath);

                    string content = File.ReadAllText(file);
                    int bracketInd = content.IndexOf("{");
                    string pipelineName = content.Substring(0, bracketInd).Trim();
                    pipelineNameToHash.Add(pipelineName, hash);
                }

                return;
            }

            // ABPK
            FileStream fs = new FileStream(Game.AssetPath + "Assets.abhd", FileMode.Open);
            BinaryReader br = new BinaryReader(fs);

            assetDictPK = new Dictionary<uint, AssetEntry>();

            int magic = br.ReadInt32();

            if (magic != 1263551041) // ABPK
            {
                br.Close();
                return;
            }

            int sceneCount = br.ReadInt32();
            int pipelineCount = br.ReadInt32();
            int assetCount = br.ReadInt32();

            // Scene name to hash
            for (int i = 0; i < sceneCount; i++)
            {
                string sceneName = br.ReadString();
                uint sceneHash = br.ReadUInt32();

                sceneNameToHash.Add(sceneName, sceneHash);
            }

            // Pipeline name to hash
            for (int i = 0; i < pipelineCount; i++)
            {
                string pipeName = br.ReadString();
                uint pipeHash = br.ReadUInt32();

                pipelineNameToHash.Add(pipeName, pipeHash);
            }

            // Assets
            int offset = 0;
            for (int a = 0; a < assetCount; a++)
            {
                AssetEntry asset = new AssetEntry()
                {
                    hash = br.ReadUInt32(),
                    size = br.ReadInt32(),
                    offset = offset,
                };
                offset += asset.size;
                assetDictPK.Add(asset.hash, asset);
            }


            pr = new BinaryReader(new FileStream(Game.AssetPath + "Assets.abpk", FileMode.Open));
        }

        private static Texture GetTextureFromPK(uint hash)
        {
            if (!s_textures.TryGetValue(hash, out Texture tex))
            {
                // Create Texture from asset dictionary

                AssetEntry texAsset = assetDictPK[hash];
                pr.BaseStream.Position = texAsset.offset;
                byte[] compressed = pr.ReadBytes(texAsset.size);

                // Decompress Data
                using (var inputStream = new MemoryStream(compressed))
                {
                    using (var outputStream = new MemoryStream())
                    {
                        using (var compressionStream = new BrotliStream(inputStream, CompressionMode.Decompress))
                        {
                            compressionStream.CopyTo(outputStream);
                        }

                        // Parse texture data
                        BinaryReader br = new BinaryReader(outputStream);
                        br.BaseStream.Position = 0;

                        int texIntType = br.ReadInt32();
                        uint width = br.ReadUInt32();
                        uint height = br.ReadUInt32();
                        byte[] pixelData = br.ReadBytes((int)outputStream.Length - 12);

                        // Load texture
                        tex = Game.wgil.CreateTexture(width, height, (TextureFormat)texIntType, TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_DST);
                        Game.wgil.WriteTexture(tex, pixelData.AsSpan(), pixelData.Length, 4);

                        s_textures.Add(hash, tex);
                    }
                }
            }

            return tex;
        }

        private static Asset LoadAssetFromPK(uint hash, AssetLoader loader)
        {
            AssetEntry assetEntry = assetDictPK[hash];
            pr.BaseStream.Position = assetEntry.offset;
            return loader.LoadAssetRAW(pr.ReadBytes(assetEntry.size));
        }

        private static PipelineAsset GetPipelineFromPK(uint hash)
        {
            // Find material in asset dictionary
            AssetEntry pipeAsset = assetDictPK[hash];

            pr.BaseStream.Position = pipeAsset.offset;
            return new UserPipelineAsset(Encoding.UTF8.GetString(pr.ReadBytes(pipeAsset.size)));
        }

        //General purpose
        public static Texture2D CreateTexture2D(string texturePath)
        {
            return GetOrCreateTexture2D(texturePath, GraphicsManager.linearSampleClamp, Vector2.Zero);
        }

        public static Texture2D CreateTexture2D(string texturePath, Sampler sampler)
        {
            return GetOrCreateTexture2D(texturePath, sampler, Vector2.Zero);
        }

        public static Texture2D CreateTexture2D(string texturePath, Sampler sampler, bool isLinear)
        {
            return GetOrCreateTexture2D(texturePath, sampler, Vector2.Zero, 0, isLinear);
        }

        public static Texture2D CreateTexture2D(string texturePath, Sampler sampler, Vector2 spriteSize)
        {
            return GetOrCreateTexture2D(texturePath, sampler, spriteSize);
        }

        public static PipelineMaterial CreateMaterial(string matPath)
        {
            var newMat = GetOrCreateAsset<PipelineMaterial>(matPath, 0);
            return newMat;
        }

        public static PrefabAsset CreatePrefabAsset(string prefabAssetPath)
        {
            var newPrefab = GetOrCreateAsset<PrefabAsset>(prefabAssetPath, 0);
            return newPrefab;
        }

        public static SpriteClip CreateSpriteClip(string clipAssetPath)
        {
            var exClip = FindExistingSpriteClip(clipAssetPath);
            if (exClip != null)
                return exClip;
            else
            {
                SpriteClip newClip = new SpriteClip(clipAssetPath);
                s_clips.Add(newClip);
                return newClip;
            }
        }

        public static SpriteClip CreateSpriteClip(Texture2D tex2d, List<Vector2> framePoses)
        {
            SpriteClip newClip = new SpriteClip(s_clips.Count, tex2d, framePoses);
            s_clips.Add(newClip);
            return newClip;
        }

        public static Mesh CreateMesh(string meshFilePath)
        {
            return GetOrCreateAsset<Mesh>(meshFilePath);
        }

        public static Transform CreateModel(string modelAssetPath)
        {
            BinaryReader br = null;
            string modAssetFolder = "";
            if(Game.debug)
            {
                modAssetFolder = Path.GetDirectoryName(modelAssetPath);
                string modelAbsPath = Game.AssetPath + modelAssetPath;
                if (File.Exists(modelAbsPath))
                    br = new BinaryReader(new FileStream(modelAbsPath, FileMode.Open));
                else
                    return null;
            }
            else
            {
                // Load from PK
            }

            int nodeC = br.ReadInt32();
            Transform[] nodeTransforms = new Transform[nodeC];

            for (int i = 0; i < nodeC; i++)
            {
                string nodeName = br.ReadString();
                int parId = br.ReadInt32();

                Entity nodeEnt = EntityManager.CreateEntity(nodeName, "");
                Transform nodeTrans = nodeEnt.Get<Transform>();
                nodeTransforms[i] = nodeTrans;

                if (parId >= 0)
                    nodeTrans.parent = nodeTransforms[parId];

                Vector3 locPos = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                Quaternion locRot = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                Vector3 locSca = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                nodeTrans.SetTRS(locPos, locRot, locSca);

                // Visualize skel
                //Mesh cubeMesh = Rendering.CubeModel.GetCubeMesh();
                //MeshRenderer mr = new MeshRenderer(cubeMesh);
                //var cubeEnt = EntityManager.CreateEntity(nodeName, "", mr);
                //cubeEnt.Get<Transform>().localPosition = nodeTrans.worldPosition;
                //cubeEnt.Get<Transform>().localScale = Vector3.One * 0.01f;
            }

            int matCount = br.ReadInt32();
            for (int m = 0; m < matCount; m++)
            {
                uint matHash = br.ReadUInt32();
                GetOrCreateAsset<PipelineMaterial>("", matHash);
            }

            int staMeshCount = br.ReadInt32();
            int skinMeshCount = br.ReadInt32();

            for (int i = 0; i < skinMeshCount; i++)
            {
                uint meshHash = br.ReadUInt32();
                uint matHash = br.ReadUInt32();
                int nodeId = br.ReadInt32();

                Mesh mesh = GetOrCreateAsset<Mesh>("", meshHash);
                PipelineMaterial material = GetOrCreateAsset<PipelineMaterial>("", matHash);

                SkinnedMeshRenderer mr = new SkinnedMeshRenderer(mesh, material);
                Transform mrTrans = nodeTransforms[nodeId];

                int boneCount = br.ReadInt32();
                mr.bones = new Transform[boneCount];
                for (int b = 0; b < boneCount; b++)
                {
                    mr.bones[b] = nodeTransforms[br.ReadInt32()];
                }

                mrTrans.entity.Add(mr);
            }


            br.Close();

            return nodeTransforms[0];
        }

        public static PipelineAsset CreatePipelineAsset(string pipelineName, params MaterialFeature[] materialFeatures)
        {
            var pipeline = GraphicsManager.GetPipelineAssetByName(pipelineName);
            if(pipeline == null)
            {
                // Try user pipeline
                if(pipelineNameToHash.TryGetValue(pipelineName, out uint hash))
                {
                    if (Game.debug)
                    {
                        string filePath = hashToFName[hash];
                        pipeline = new UserPipelineAsset(File.ReadAllText(Game.AssetPath.ToCommonPath() + filePath));
                    }
                    else
                    {
                        pipeline = GetPipelineFromPK(hash);
                    }
                }
            }

            if(pipeline != null && materialFeatures.Length > 0)
            {
                string defineKey = "";
                foreach (var feature in materialFeatures)
                    defineKey += "*" + PipelineAsset.MatFeatureToKey[feature];

                pipeline = pipeline.GetPipelineVariant(defineKey);
            }

            return pipeline;
        }

        internal static Texture2D GetDefaultTexture()
        {
            if (defTexture != null)
                return defTexture;

            // Load texture
            Texture tex = Game.wgil.CreateTexture(128, 128, TextureFormat.Rgba8Unorm, TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_DST);

            var pixelData = new byte[128 * 128 * 4];
            byte data = 255;
            Array.Fill(pixelData, data);

            int offset = 10;
            for (int x = 0; x < 128; x++)
            {
                if (x < offset || x > (128 - offset))
                {
                    for (int y = 0; y < 128; y++)
                    {
                        int coord = (y * 128 * 4) + (x * 4) + 3;
                        pixelData[coord] = 0;
                    }
                }
            }

            for (int y = 0; y < 128 * 128 * 4; y += 128 * 4)
            {
                if (y < offset * 128 * 4 || y > (128 * 128 * 4 - offset * 128 * 4))
                {
                    for (int x = 0; x < 128; x++)
                    {
                        int coord = y + x * 4 + 3; 
                        pixelData[coord] = 0;
                    }
                }
            }

            Game.wgil.WriteTexture(tex, pixelData.AsSpan(), pixelData.Length, 4);

            defTexture = new Texture2D(1, tex, GraphicsManager.pointSamplerClamp, Vector2.Zero); ;
            return defTexture;
        }


        // ABE Helpers

        internal static Texture2D GetOrCreateTexture2D(string texPath, Sampler sampler, Vector2 spriteSize, uint preHash = 0, bool linear = false)
        {
            uint hash = preHash;
            if (hash == 0)
                hash = texPath.ToHash32();

            if (sampler == null)
                sampler = GraphicsManager.linearSampleClamp;

            var tex2d = s_texture2ds.FirstOrDefault(t => t.fPathHash == hash && t.textureSampler == sampler && t.spriteSize == spriteSize);
            if (tex2d != null)
                return tex2d;

            Texture tex = null;
            if (!Game.debug)
                tex = GetTextureFromPK(hash);
            else
            {
                if (preHash != 0)
                    texPath = hashToFName[preHash];
                tex = GetTextureDebug(Game.AssetPath + texPath, false, linear);
            }

            tex2d = new Texture2D(hash, tex, sampler, spriteSize);
            s_texture2ds.Add(tex2d);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = tex2d;
            else
                assetDict.Add(hash, tex2d);
            return tex2d;
        }

        private static T GetCachedAsset<T>(string assetPath, uint preHash, out uint hash) where T : Asset
        {
            hash = preHash;
            if (hash == 0)
                hash = assetPath.ToHash32();

            T tAsset = null;
            if (assetDict.TryGetValue(hash, out Asset asset))
                tAsset = asset as T;

            return tAsset;
        }

        private static void RegisterAsset(Asset asset, uint hash)
        {
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = asset;
            else
                assetDict.Add(hash, asset);
        }

        private static T GetOrCreateAsset<T>(string assetPath, uint preHash = 0) where T : Asset
        {
            T asset = GetCachedAsset<T>(assetPath, preHash, out uint hash);

            if (asset != null)
                return asset;

            // Not cached / Load the asset
            AssetLoader loader = assetLoaders[typeof(T)];
            if (!Game.debug)
                asset = LoadAssetFromPK(hash, loader) as T;
            else
            {
                if (preHash != 0)
                    assetPath = hashToFName[preHash];
                asset = loader.LoadAssetRAW(File.ReadAllBytes(Game.AssetPath + assetPath)) as T;
            }

            asset.fPathHash = hash;
            RegisterAsset(asset, hash);

            return asset;
        }

        public static string GetTextAsset(string assetPath)
        {
            string fullPath = Game.AssetPath + assetPath;
            if (File.Exists(fullPath))
                return File.ReadAllText(fullPath);
            return "";
        }

        internal static List<string> GetUserPipelines()
        {
            return pipelineNameToHash.Keys.ToList();
        }

        // Editor ONLY remove later
        internal static Texture2D GetTextureEditorBinding(string texPath)
        {
            uint hash = texPath.ToHash32();
            var tex2d = s_texture2ds.FirstOrDefault(t => t.fPathHash == hash);
            return tex2d;
        }

        internal static void AddAsset(Asset asset, string file)
        {
            uint hash = asset.fPathHash;
            hashToFName.Add(hash, file);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = asset;
            else
                assetDict.Add(hash, asset);
        }

        // Editor ONLY remove later
        internal static void UpdateAsset(uint oldHash, uint hash, string file)
        {
            if (hashToFName.ContainsKey(oldHash))
                hashToFName.Remove(oldHash);
            if (!hashToFName.ContainsKey(hash))
                hashToFName.Add(hash, file);

            if (assetDict.ContainsKey(hash) || !assetDict.ContainsKey(oldHash))
                return;

            var asset = assetDict[oldHash];
            if (asset is PrefabAsset)
                PrefabManager.UpdatePrefab(oldHash, hash);

            asset.fPathHash = hash;
            assetDict.Remove(oldHash);
            assetDict.Add(hash, asset);
        }

        private static SpriteClip FindExistingSpriteClip(string clipAssetPath)
        {
            return s_clips.FirstOrDefault(c => c.clipAssetPath.Equals(clipAssetPath));
        }

        internal static TextureView GetTextureView(Texture texture)
        {
            if (!s_textureViews.TryGetValue(texture, out TextureView view))
            {
                view = texture.CreateView();
                s_textureViews.Add(texture, view);
            }

            return view;
        }

        // Debug
        //public static Texture GetOrCreateTexture(string imgPath)
        //{
        //    ImageSharpTexture texData = GetImage(imgPath);
        //    return GetTexture(texData);
        //}

        //public static ImageSharpTexture GetOrCreateTextureData(string imgPath)
        //{
        //    return GetImage(imgPath);
        //}

        //internal static ImageSharpTexture GetImage(string path, bool loadMipmap)
        //{
        //    if (!s_images.TryGetValue(path, out ImageSharpTexture img))
        //    {
        //        img = new ImageSharpTexture(path, loadMipmap);
        //        s_images.Add(path, img);
        //    }

        //    return img;
        //}

        public static TextureView GetOrCreateTextureView(Texture texture)
        {
            if (texture == null)
                return defTexture.GetView();

            if (!s_textureViews.TryGetValue(texture, out TextureView view))
            {
                view = texture.CreateView();
                s_textureViews.Add(texture, view);
            }

            return view;
        }

        internal static Texture GetTextureDebug(string texPath, bool mipmap = false, bool linear = false)
        {
            var imageData = GetImageDebug(texPath, mipmap, linear);
            return GetTextureDebug(imageData);
        }

        internal static Texture GetTextureDebug(string folder, string texPath, bool mipmap = false, bool linear = false)
        {
            var imageData = GetImageDebug(folder + texPath, mipmap, linear);
            return GetTextureDebug(imageData);
        }

        internal static ImageSharpTexture GetImageDebug(string path, bool mipmap, bool linear)
        {
            if (!s_images_debug.TryGetValue(path, out ImageSharpTexture img))
            {
                img = new ImageSharpTexture(path, mipmap, !linear);
                s_images_debug.Add(path, img);
            }

            return img;
        }

        internal static Texture GetTextureDebug(ImageSharpTexture textureData)
        {
            if (!s_textures_debug.TryGetValue(textureData, out Texture tex))
            {
                tex = textureData.CreateWGILTexture();
                s_textures_debug.Add(textureData, tex);
            }

            return tex;
        }


        // Serialization - For scene
        internal static JValue SerializeAssets()
        {
            JsonObjectBuilder assets = new JsonObjectBuilder(2000);
            assets.Put("Count", sceneAssets.Count);

            JsonArrayBuilder assetsArr = new JsonArrayBuilder(2000);

            foreach (var asset in sceneAssets)
                assetsArr.Push(asset.SerializeAsset());

            assets.Put("Entries", assetsArr.Build());

            sceneAssets.Clear();

            return assets.Build();
        }

        internal static void DeserializeAssets(JValue assets)
        {
            int assetC = assets["Count"];
           
            foreach (var asset in assets["Entries"].Array())
            {
                Asset curAsset = null;

                uint hash = (uint)((long)asset["FileHash"]);
                if (hash == 0) // Not a disk asset
                    continue;

                int typeID = asset["TypeID"];
                switch (typeID)
                {
                    case 0: // Texture
                        curAsset = DeserializeTexture(asset, hash);
                        break;
                    case 1: // Material
                        curAsset = GetOrCreateAsset<PipelineMaterial>(null, hash);
                        break;
                    case 2: // Prefab
                        curAsset = GetOrCreateAsset<PrefabAsset>(null, hash);
                        break;
                    case 3: // Mesh
                        curAsset = GetOrCreateAsset<Mesh>(null, hash);
                        break;
                    default:
                        break;
                }
                if (curAsset != null)
                    sceneAssets.Add(curAsset);
            }
        }

        internal static int GetAssetSceneIndex(uint hash)
        {
            if (!assetDict.ContainsKey(hash))
                return -1;

            Asset asset = assetDict[hash];
            int assetIndex = sceneAssets.IndexOf(asset);
            if (assetIndex == -1) // Not in the list
            {
                sceneAssets.Add(asset);
                return sceneAssets.Count - 1;
            }

            return assetIndex;
        }

        internal static void ClearSerializeDependencies()
        {
            sceneAssets.Clear();
        }

        internal static long AddAssetDependency(uint hash)
        {
            if (assetDict.ContainsKey(hash))
            {
                Asset asset = assetDict[hash];
                if(!sceneAssets.Contains(asset))
                {
                    sceneAssets.Add(asset);
                }
            }

            return hash;
        }


        internal static Asset GetAssetFromSceneIndex(int index)
        {
            if (index < 0 || index >= sceneAssets.Count)
                return null;
            return sceneAssets[index];
        }

        internal static Asset GetAssetFromHash(uint hash)
        {
            if (assetDict.TryGetValue(hash, out Asset asset))
                return asset;
            return null;
        }

        internal static string GetFilenameFromHash(uint hash)
        {
            if (hashToFName.TryGetValue(hash, out string fName))
                return fName;

            return null;
        }

        internal static void ClearSceneCache()
        {
            s_texture2ds.Clear();
            s_clips.Clear();
            assetDict.Clear();
            sceneAssets.Clear();

            LoadDefaultMaterials();
        }

        static void LoadDefaultMaterials()
        {
            // Default Materials
            var uberMat = GraphicsManager.GetUberMaterial();
            var additiveMat = GraphicsManager.GetUberAdditiveMaterial();
            var uber3d = GraphicsManager.GetUber3D();
            //var uberTransparent = GraphicsManager.GetUberTransparentMaterial();

            assetDict.Add(uberMat.fPathHash, uberMat);
            assetDict.Add(additiveMat.fPathHash, additiveMat);
            assetDict.Add(uber3d.fPathHash, uber3d);
            //assetDict.Add(uberTransparent.fPathHash, uberTransparent);
        }

        private static Texture2D DeserializeTexture(JValue texAsset, uint hash)
        {
            string samplerName = texAsset["Sampler"];
            Sampler sampler = NameToSampler(samplerName);
            Vector2 spriteSize = new Vector2(texAsset["SpriteSizeX"], texAsset["SpriteSizeY"]);

            return GetOrCreateTexture2D(null, sampler, spriteSize, hash);
        }


        internal static Sampler NameToSampler(string name)
        {
            switch (name)
            {
                case "LinearClamp":
                    return GraphicsManager.linearSampleClamp;
                case "LinearWrap":
                    return GraphicsManager.linearSamplerWrap;
                case "PointClamp":
                    return GraphicsManager.pointSamplerClamp;
                default:
                    return GraphicsManager.linearSampleClamp;
            }
        }


        public static void DisposeResources()
        {
            if (Game.debug)
            {
                foreach (KeyValuePair<ImageSharpTexture, Texture> kvp in s_textures_debug)
                {
                    kvp.Value.Dispose();
                }
                s_textures_debug.Clear();

                s_images_debug.Clear();
            }
            else
            {
                foreach (KeyValuePair<uint, Texture> kvp in s_textures)
                {
                    kvp.Value.Dispose();
                }
                s_textures.Clear();
            }

            foreach (KeyValuePair<Texture, TextureView> kvp in s_textureViews)
            {
                kvp.Value.Dispose();
            }
            s_textureViews.Clear();


            // ABE Types
            s_texture2ds.Clear();
            s_clips.Clear();
            defTexture = null;
        }
    }
}
