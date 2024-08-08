using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WGIL;

namespace ABEngine.ABERuntime.Core.Assets
{
    internal class DebugAssetCache : BaseAssetCache
    {
        private static readonly Dictionary<string, ImageSharpTexture> s_images_debug = new();
        private static readonly Dictionary<ImageSharpTexture, Texture> s_textures_debug = new();
        
        private static readonly Dictionary<uint, string> hashToFName = new();

        internal override void InitCache()
        {
            base.InitCache();
            
            string commonAssetPath = Game.AssetPath;

            var fileEnum = Directory.EnumerateFiles(commonAssetPath, "*.*", SearchOption.AllDirectories);

            // Get hash override assets
            var files = fileEnum.Where(s => s.ToLower().EndsWith(".hoa"));
            foreach (var file in files)
            {
                string localPath = file.ToCommonPath().Replace(commonAssetPath, "");
                string hashStr = Path.GetFileNameWithoutExtension(localPath);
                hashToFName.Add(Convert.ToUInt32(hashStr, 16), localPath);
            }

            // Get standard assets
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
            hashParser(".abclip");

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

            // Get glsl includes
            files = fileEnum.Where(s => s.ToLower().EndsWith(".glsl"));
            foreach (var file in files)
            {
                string localPath = file.ToCommonPath().Replace(commonAssetPath, "");
                uint hash = localPath.ToHash32();
                hashToFName.Add(hash, localPath);

                string includeName = Path.GetFileNameWithoutExtension(file);
                pipelineNameToHash.Add(includeName, hash);
            }
        }
        
        protected override Asset LoadAsset(string assetPath, uint hash, uint preHash, AssetLoader loader)
        {
            if (preHash != 0)
                assetPath = hashToFName[preHash];
            return loader.LoadAssetRAW(File.ReadAllBytes(Game.AssetPath + assetPath));
        }

        protected override Texture LoadTexture(string texPath, uint hash, uint preHash, bool isLinear)
        {
            if (preHash != 0)
                texPath = hashToFName[preHash];
            return LoadTextureDebug(Game.AssetPath + texPath, false, isLinear);
        }
        
        internal Texture LoadTextureDebug(string texPath, bool mipmap = false, bool linear = false)
        {
            var imageData = GetImageDebug(texPath, mipmap, linear);
            return GetTextureDebug(imageData);
        }
        
        internal ImageSharpTexture GetImageDebug(string path, bool mipmap, bool linear)
        {
            if (!s_images_debug.TryGetValue(path, out ImageSharpTexture img))
            {
                img = new ImageSharpTexture(path, mipmap, !linear);
                s_images_debug.Add(path, img);
            }

            return img;
        }
        
        internal Texture GetTextureDebug(ImageSharpTexture textureData, bool isCube = false)
        {
            if (!s_textures_debug.TryGetValue(textureData, out Texture tex))
            {
                if(!isCube)
                    tex = textureData.CreateWGILTexture();
                s_textures_debug.Add(textureData, tex);
            }

            return tex;
        }
     
        protected override PipelineAsset LoadPipeline(uint hash)
        {
            string filePath = hashToFName[hash];
            return new UserPipelineAsset(File.ReadAllText(Game.AssetPath.ToCommonPath() + filePath));
        }
        
        protected override string LoadShaderInclude(uint hash)
        {
            string filePath = hashToFName[hash];
            return File.ReadAllText(Game.AssetPath.ToCommonPath() + filePath);
        }

        internal override void DisposeResources()
        {
            base.DisposeResources();
            
            foreach (KeyValuePair<ImageSharpTexture, Texture> kvp in s_textures_debug)
            {
                kvp.Value.Dispose();
            }
            s_textures_debug.Clear();
            s_images_debug.Clear();
        }
    }
}