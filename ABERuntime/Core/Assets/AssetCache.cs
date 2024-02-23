﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.IO.Compression;
using WGIL;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Halak;
using System.Runtime.CompilerServices;
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
        private static readonly List<PipelineMaterial> s_materials = new List<PipelineMaterial>();
        private static readonly List<PrefabAsset> s_prefabAssets = new List<PrefabAsset>();
        private static readonly List<SpriteClip> s_clips = new List<SpriteClip>();
        private static readonly List<Mesh> s_meshes = new List<Mesh>();

        private static Texture2D defTexture = null;

        // Serialize
        static Dictionary<uint, AssetEntry> assetDictPK;
        static Dictionary<uint, Asset> assetDict;

        // Scene specific serialize
        static List<Asset> sceneAssets = new List<Asset>();

        static BinaryReader pr; // ABPK reader

        public static void InitAssetCache()
        {
            assetDict = new Dictionary<uint, Asset>();

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

        private static PipelineMaterial GetMaterialFromPK(uint hash)
        {
            // Find material in asset dictionary
            AssetEntry matAsset = assetDictPK[hash];

            pr.BaseStream.Position = matAsset.offset;
            return LoadMaterialRAW(hash, pr.ReadBytes(matAsset.size));
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
            var newMat = GetOrCreateMaterial(matPath);
            return newMat;
        }

        public static PrefabAsset CreatePrefabAsset(string prefabAssetPath)
        {
            var newPrefab = GetOrCreatePrefabAsset(prefabAssetPath);
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

        public static Mesh CreateMesh(string meshFilePath, bool flipUVs = false)
        {
            return GetOrCreateMesh(meshFilePath, 0, flipUVs);
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
                GetOrCreateMaterial("", matHash);
            }

            int staMeshCount = br.ReadInt32();
            int skinMeshCount = br.ReadInt32();

            for (int i = 0; i < skinMeshCount; i++)
            {
                uint meshHash = br.ReadUInt32();
                uint matHash = br.ReadUInt32();
                int nodeId = br.ReadInt32();

                Mesh mesh = GetOrCreateMesh("", meshHash);
                PipelineMaterial material = GetOrCreateMaterial("", matHash);

                MeshRenderer mr = new MeshRenderer(mesh, material);
                Transform mrTrans = nodeTransforms[nodeId];

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

        private static Texture2D GetOrCreateTexture2D(string texPath, Sampler sampler, Vector2 spriteSize, uint preHash = 0, bool linear = false)
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

        private static PipelineMaterial GetOrCreateMaterial(string matPath, uint preHash = 0)
        {
            uint hash = preHash;
            if (hash == 0)
                hash = matPath.ToHash32();

            PipelineMaterial mat = s_materials.FirstOrDefault(t => t.fPathHash == hash);
            if (mat != null)
                return mat;

            if (!Game.debug)
                mat = GetMaterialFromPK(hash);
            else
            {
                if (preHash != 0)
                    matPath = hashToFName[preHash];
                mat = LoadMaterialRAW(hash, File.ReadAllBytes(Game.AssetPath + matPath));
            }

            s_materials.Add(mat);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = mat;
            else
                assetDict.Add(hash, mat);
            return mat;
        }

        private static PrefabAsset GetOrCreatePrefabAsset(string prefabPath, uint preHash = 0)
        {
            uint hash = preHash;
            if (hash == 0)
                hash = prefabPath.ToHash32();

            PrefabAsset prefabAsset = s_prefabAssets.FirstOrDefault(t => t.fPathHash == hash);
            if (prefabAsset != null)
                return prefabAsset;

            if (!Game.debug)
            {
                //prefabAsset = GetMaterialFromPK(hash);
            }
            else
            {
                if (preHash != 0)
                    prefabPath = hashToFName[preHash];
                prefabAsset = LoadPrefabAssetRAW(hash, File.ReadAllBytes(Game.AssetPath + prefabPath));
            }

            s_prefabAssets.Add(prefabAsset);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = prefabAsset;
            else
                assetDict.Add(hash, prefabAsset);
            return prefabAsset;
        }

        private static Mesh GetOrCreateMesh(string meshPath, uint preHash = 0, bool flipUVs = false)
        {
            uint hash = preHash;
            if (hash == 0)
                hash = meshPath.ToHash32();

            Mesh mesh = s_meshes.FirstOrDefault(t => t.fPathHash == hash);
            if (mesh != null)
                return mesh;

            if (!Game.debug)
            {
                //prefabAsset = GetMaterialFromPK(hash);
            }
            else
            {
                if (preHash != 0)
                    meshPath = hashToFName[preHash];

                string meshAbsPath = Game.AssetPath.ToCommonPath() + meshPath;
                if (!File.Exists(meshAbsPath))
                    return null;

                mesh = LoadMeshRAW(hash, File.ReadAllBytes(meshAbsPath), flipUVs);
            }

            s_meshes.Add(mesh);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = mesh;
            else
                assetDict.Add(hash, mesh);
            return mesh;
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

        internal static void AddMaterial(PipelineMaterial mat, string file)
        {
            uint hash = mat.fPathHash;
            hashToFName.Add(hash, file);
            s_materials.Add(mat);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = mat;
            else
                assetDict.Add(hash, mat);
        }

        // Editor ONLY remove later
        internal static void AddPrefab(PrefabAsset prefabAsset, string file)
        {
            uint hash = prefabAsset.fPathHash;
            hashToFName.Add(hash, file);
            s_prefabAssets.Add(prefabAsset);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = prefabAsset;
            else
                assetDict.Add(hash, prefabAsset);
        }

        internal static void UpdateAsset(uint oldHash, uint hash, string file)
        {
            if (hashToFName.ContainsKey(oldHash))
                hashToFName.Remove(oldHash);
            if (!hashToFName.ContainsKey(hash))
                hashToFName.Add(hash, file);

            if (assetDict.ContainsKey(hash) || !assetDict.ContainsKey(oldHash))
                return;

            var asset = assetDict[oldHash];
            if (s_prefabAssets.Contains(asset))
                PrefabManager.UpdatePrefab(oldHash, hash);

            asset.fPathHash = hash;
            assetDict.Remove(oldHash);
            assetDict.Add(hash, asset);
        }

        private static PipelineMaterial FindExistingMaterial(int matInsID)
        {
            return s_materials.FirstOrDefault(m => m.instanceID == matInsID);
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

        internal static PipelineMaterial LoadMaterialRAW(uint hash, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                string matName = br.ReadString();
                string pipelineName = br.ReadString();

                PipelineAsset pipelineAsset = CreatePipelineAsset(pipelineName);
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

                    Texture2D tex2d = GetOrCreateTexture2D(null, null, Vector2.Zero, texHash, isLinear);
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

        internal static PrefabAsset LoadPrefabAssetRAW(uint hash, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                PrefabAsset prefabAsset = new PrefabAsset(hash);
                prefabAsset.serializedData = br.ReadString();
                if(ms.Position + 20 <= ms.Length && br.ReadInt32() == guidMagic)
                    prefabAsset.prefabGuid = new Guid(br.ReadBytes(16));

                return prefabAsset;
            }
        }

        private static Mesh LoadMeshRAW(uint hash, byte[] meshData, bool flipUVs)
        {
            float uvMult = flipUVs ? -1 : 1;
            float uvAdd = flipUVs ? 1 : 0;

            Mesh mesh = new Mesh(hash);
            using (MemoryStream fs = new MemoryStream(meshData))
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
                                uv[i] = new Vector2(br.ReadSingle(), br.ReadSingle() * uvMult + uvAdd);
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
                            br.BaseStream.Position += 64 * matrixCount;
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
                        curAsset = GetOrCreateMaterial(null, hash);
                        break;
                    case 2: // Prefab
                        curAsset = GetOrCreatePrefabAsset(null, hash);
                        break;
                    case 3: // Mesh
                        curAsset = GetOrCreateMesh(null, hash);
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
            s_materials.Clear();
            s_prefabAssets.Clear();
            s_meshes.Clear();
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

            s_materials.Add(uberMat);
            s_materials.Add(additiveMat);
            s_materials.Add(uber3d);
            //s_materials.Add(uberTransparent);

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
            s_materials.Clear();
            s_clips.Clear();
            s_prefabAssets.Clear();
            s_meshes.Clear();
            defTexture = null;
        }
    }
}
