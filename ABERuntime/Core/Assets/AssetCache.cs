using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Utilities;
using Force.Crc32;
using System.Text;
using System.IO.Compression;
using SixLabors.ImageSharp.PixelFormats;
using Vulkan;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Halak;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime
{
    class AssetEntry
    {
        public uint hash { get; set; }
        public int size { get; set; }
        public long offset { get; set; }
        public int streamId { get; set; }
    }

    public static class AssetCache
    {
        // Debug - Raw
        private static readonly Dictionary<string, ImageSharpTexture> s_images_debug = new Dictionary<string, ImageSharpTexture>();
        private static readonly Dictionary<ImageSharpTexture, Texture> s_textures_debug = new Dictionary<ImageSharpTexture, Texture>();

        // Debug - Hash to path
        private static readonly Dictionary<uint, string> hashToFName = new Dictionary<uint, string>();

        // Release - ABPK
        private static readonly Dictionary<uint, Texture> s_textures = new Dictionary<uint, Texture>();
        private static readonly Dictionary<Texture, TextureView> s_textureViews = new Dictionary<Texture, TextureView>();

        // ABE Types
        private static readonly List<Texture2D> s_texture2ds = new List<Texture2D>();
        private static readonly List<PipelineMaterial> s_materials = new List<PipelineMaterial>();
        private static readonly List<SpriteClip> s_clips = new List<SpriteClip>();

        private static Texture2D defTexture = null;
        private static Texture2D gridTexture = null;

        // Serialize
        

        static Dictionary<uint, AssetEntry> assetDictPK;
        static Dictionary<uint, Asset> assetDict;


        static List<BinaryReader> pkReaders; 
        public static void InitAssetCache()
        {
            assetDict = new Dictionary<uint, Asset>();
            if (Game.debug)
            {
                var images = Directory.EnumerateFiles(Game.AssetPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".jpg") || s.ToLower().EndsWith(".png"));
                foreach (var image in images)
                {
                    string localPath = image.Replace(Game.AssetPath, "");
                    uint hash = Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(localPath));
                    hashToFName.Add(hash, localPath);
                }


                var materials = Directory.EnumerateFiles(Game.AssetPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".abmat"));
                foreach (var material in materials)
                {
                    string localPath = material.Replace(Game.AssetPath, "");
                    uint hash = Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(localPath));
                    hashToFName.Add(hash, localPath);
                }

                return;
            }
          
            var pks = Directory.GetFiles(Game.AssetPath, "*.abpk", SearchOption.TopDirectoryOnly);
            pkReaders = new List<BinaryReader>();
            assetDictPK  = new Dictionary<uint, AssetEntry>();

            for (int i = 0; i < pks.Length; i++)
            {
                FileStream fs = new FileStream(pks[i], FileMode.Open);
                BinaryReader br = new BinaryReader(fs);

                // Read Header
                int magic = br.ReadInt32();
                if (magic != 1263551041) // ABPK
                {
                    br.Close();
                    continue;
                }

                int streamId = pkReaders.Count;
                int assetC = br.ReadInt32();
                int offset = 8 + assetC * 8;

                for (int a = 0; a < assetC; a++)
                {
                    AssetEntry asset = new AssetEntry()
                    {
                        hash = br.ReadUInt32(),
                        size = br.ReadInt32(),
                        offset = offset,
                        streamId = streamId
                    };
                    offset += asset.size;
                    assetDictPK.Add(asset.hash, asset);
                }

                pkReaders.Add(br);
            }
        }

        private static Texture GetTextureFromPK(uint hash)
        {
            if (!s_textures.TryGetValue(hash, out Texture tex))
            {
                // Create Texture from asset dictionary

                AssetEntry texAsset = assetDictPK[hash];
                BinaryReader pr = pkReaders[texAsset.streamId];

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
                        var texDesc = TextureDescription.Texture2D(
                        width, height, 1, 1, (PixelFormat)texIntType, TextureUsage.Sampled);
                        tex = GraphicsManager.rf.CreateTexture(texDesc);
                        unsafe
                        {
                            fixed (byte* pin = pixelData)
                            {
                               GraphicsManager.gd.UpdateTexture(
                               tex,
                               (IntPtr)pin,
                               (uint)pixelData.Length,
                               0,
                               0,
                               0,
                               width,
                               height,
                               1,
                               0,
                               0);
                            }
                        }

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
            BinaryReader pr = pkReaders[matAsset.streamId];

            pr.BaseStream.Position = matAsset.offset;
            byte[] compressed = pr.ReadBytes(matAsset.size);

            // Decompress Data
            using (var inputStream = new MemoryStream(compressed))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var compressionStream = new BrotliStream(inputStream, CompressionMode.Decompress))
                    {
                        compressionStream.CopyTo(outputStream);
                    }

                    // Parse material data
                    BinaryReader br = new BinaryReader(outputStream);
                    br.BaseStream.Position = 0;

                    

                   
                }
            }


            return null;
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

        public static Texture2D CreateTexture2D(string texturePath, Sampler sampler, Vector2 spriteSize)
        {
            return GetOrCreateTexture2D(texturePath, sampler, spriteSize);
        }


        // Serialization - Retrieve from hash
        //internal static Texture2D GetSerializedTexture(uint hash, Sampler sampler, Vector2 spriteSize)
        //{
        //    return GetOrCreateTexture2D(null, sampler, spriteSize, hash); ;
        //}

        internal static PipelineMaterial GetSerializedMaterial(uint hash)
        {
            return null;
        }

        // Editor
        //public static Texture2D CreateTexture2D(string folderPath, string texturePath, Sampler sampler)
        //{
        //    var exTex = FindExistingTex2D(folderPath + texturePath, sampler, Vector2.Zero);
        //    if (exTex != null)
        //        return exTex;
        //    else
        //    {
        //        Texture2D newTex = new Texture2D(folderPath, texturePath, sampler);
        //        s_texture2ds.Add(newTex);
        //        return newTex;
        //    }
        //}
        // End Editor

        //public static PipelineMaterial CreateMaterial(PipelineMaterial refeferenceMat)
        //{
        //    PipelineMaterial newMat = refeferenceMat.GetCopy();
        //    s_materials.Add(newMat);
        //    return newMat;
        //}

        public static PipelineMaterial CreateMaterial(string matPath)
        {
            var newMat = GetOrCreateMaterial(matPath);
            return newMat;
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

        internal static Texture2D GetDefaultTexture()
        {
            if (defTexture != null)
                return defTexture;

            // Load texture
            var texDesc = TextureDescription.Texture2D(
            128, 128, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            var tex = GraphicsManager.rf.CreateTexture(texDesc);
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

            unsafe
            {
                fixed (byte* pin = pixelData)
                {
                    GraphicsManager.gd.UpdateTexture(
                    tex,
                    (IntPtr)pin,
                    (uint)pixelData.Length,
                    0,
                    0,
                    0,
                    128,
                    128,
                    1,
                    0,
                    0);
                }
            }

            defTexture = new Texture2D(1, tex, GraphicsManager.pointSamplerClamp, Vector2.Zero); ;
            return defTexture;
        }

        internal static Texture2D GetGridTexture()
        {
            if (gridTexture != null)
                return gridTexture;

            // Load texture
            var texDesc = TextureDescription.Texture2D(
            128, 128, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            var tex = GraphicsManager.rf.CreateTexture(texDesc);
            var pixelData = new byte[128 * 128 * 4];
            for (int i = 0; i < pixelData.Length; i+=4)
            {
                pixelData[i] = 127;
                pixelData[i + 1] = 127;
                pixelData[i + 2] = 127;
                pixelData[i + 3] = 255;
            }

            unsafe
            {
                fixed (byte* pin = pixelData)
                {
                    GraphicsManager.gd.UpdateTexture(
                    tex,
                    (IntPtr)pin,
                    (uint)pixelData.Length,
                    0,
                    0,
                    0,
                    128,
                    128,
                    1,
                    0,
                    0);
                }
            }

            gridTexture = new Texture2D(1, tex, GraphicsManager.linearSampleClamp, Vector2.Zero); ;
            return gridTexture;
        }

        // ABE Helpers

        private static Texture2D GetOrCreateTexture2D(string texPath, Sampler sampler, Vector2 spriteSize, uint preHash = 0)
        {
            uint hash = preHash;
            if(hash == 0)
                hash = Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(texPath));

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
                tex = GetTextureDebug(Game.AssetPath + texPath);
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
                hash = Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(matPath));

            PipelineMaterial mat = s_materials.FirstOrDefault(t => t.fPathHash == hash);
            if (mat != null)
                return mat;

            if (!Game.debug)
                mat = GetMaterialFromPK(hash);
            else
            {
                if (preHash != 0)
                    matPath = hashToFName[preHash];
                mat = LoadMaterialRAW(preHash, File.ReadAllBytes(Game.AssetPath + matPath));
            }

            s_materials.Add(mat);
            if (assetDict.ContainsKey(hash))
                assetDict[hash] = mat;
            else
                assetDict.Add(hash, mat);
            return mat;
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
                view = GraphicsManager.rf.CreateTextureView(texture);
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
            if (!s_textureViews.TryGetValue(texture, out TextureView view))
            {
                view = GraphicsManager.rf.CreateTextureView(texture);
                s_textureViews.Add(texture, view);
            }

            return view;
        }

        internal static Texture GetTextureDebug(string texPath)
        {
            var imageData = GetImageDebug(texPath);
            return GetTextureDebug(imageData);
        }

        internal static Texture GetTextureDebug(string folder, string texPath)
        {
            var imageData = GetImageDebug(folder + texPath);
            return GetTextureDebug(imageData);
        }

        internal static ImageSharpTexture GetImageDebug(string path)
        {
            if (!s_images_debug.TryGetValue(path, out ImageSharpTexture img))
            {
                img = new ImageSharpTexture(path);
                s_images_debug.Add(path, img);
            }

            return img;
        }

        internal static Texture GetTextureDebug(ImageSharpTexture textureData)
        {
            if (!s_textures_debug.TryGetValue(textureData, out Texture tex))
            {
                tex = textureData.CreateDeviceTexture(GraphicsManager.gd, GraphicsManager.rf);
                s_textures_debug.Add(textureData, tex);
            }

            return tex;
        }

        internal static PipelineMaterial LoadMaterialRAW(uint hash, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader br = new BinaryReader(ms))
            {
                string pipelineName = br.ReadString();

                PipelineAsset pipelineAsset = GraphicsManager.GetPipelineAssetByName(pipelineName);
                var layouts = pipelineAsset.GetResourceLayouts();

                PipelineMaterial mat = new PipelineMaterial(hash, pipelineAsset, layouts[0], layouts[1]);
                ShaderProp prop = new ShaderProp();
                
            }

            return null;
        }

        // Serialization
        internal static JValue SerializeAssets()
        {
            JsonObjectBuilder assets = new JsonObjectBuilder(2000);
            assets.Put("Count", assetDict.Count);

            JsonArrayBuilder assetsArr = new JsonArrayBuilder(2000);

            foreach (var assetKV in assetDict)
                assetsArr.Push(assetKV.Value.SerializeAsset());

            assets.Put("Entries", assetsArr.Build());
            return assets.Build();
        }

        internal static void DeserializeAssets(JValue assets)
        {
            int assetC = assets["Count"];
           
            foreach (var asset in assets["Entries"].Array())
            {
                Asset curAsset = null;

                uint hash = (uint)((long)asset["FileHash"]);
                int typeID = asset["TypeID"];
                switch (typeID)
                {
                    case 0: // Texture
                        curAsset = DeserializeTexture(asset, hash);
                        break;
                    default:
                        break;
                }
            }

        }

        internal static int GetAssetSceneIndex(Asset asset)
        {
            int index = 0;
            foreach (var assetKV in assetDict)
            {
                if (assetKV.Value == asset)
                    return index;
                index++;
            }

            return -1;
        }

        internal static Asset GetAssetFromSceneIndex(int index)
        {
            return assetDict.ElementAt(index).Value;
        }

        internal static void ClearSceneCache()
        {
            s_texture2ds.Clear();
            s_clips.Clear();
            s_materials.Clear();
            assetDict.Clear();
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
            defTexture = null;
            gridTexture = null;
        }
    }
}
