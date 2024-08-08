using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WGIL;

namespace ABEngine.ABERuntime.Core.Assets
{
    internal abstract class BaseAssetCache : IAssetCache
    {
        protected readonly Dictionary<string, uint> pipelineNameToHash = new();
        protected readonly Dictionary<string, uint> sceneNameToHash = new();
        
        // Cache
        private readonly Dictionary<Texture, TextureView> cachedViews = new();
        private readonly List<Texture2D> cachedTexture2Ds = new();

        // ABE
        protected Dictionary<uint, Asset> assetDict;

        // Loaders
        Dictionary<Type, AssetLoader> assetLoaders;

        internal virtual void InitCache()
        {
            assetDict = new Dictionary<uint, Asset>();

            // Asset Loaders
            assetLoaders = new()
            {
                { typeof(PipelineMaterial), new MaterialLoader() },
                { typeof(Mesh), new MeshLoader() },
                { typeof(PrefabAsset), new PrefabLoader() },
                { typeof(AnimationClip), new AnimClipLoader() },
                { typeof(SpriteClip), new SpriteClipLoader() },
            };
            
            LoadDefaultMaterials();
        }
        
        internal void LoadDefaultMaterials()
        {
            // Default Materials
            var uberMat = Graphics.GetUberMaterial();
            var additiveMat = Graphics.GetUberAdditiveMaterial();
            var uber3d = Graphics.GetUber3D();

            assetDict.Add(uberMat.fPathHash, uberMat);
            assetDict.Add(additiveMat.fPathHash, additiveMat);
            assetDict.Add(uber3d.fPathHash, uber3d);
        }
        
        private T GetCachedAsset<T>(string assetPath, uint preHash, out uint hash) where T : Asset
        {
            hash = preHash;
            if (hash == 0)
                hash = assetPath.ToHash32();

            T tAsset = null;
            if (assetDict.TryGetValue(hash, out Asset asset))
                tAsset = asset as T;

            return tAsset;
        }

        private void RegisterAsset(Asset asset, uint hash)
        {
            assetDict[hash] = asset;
        }

        internal T GetOrCreateAsset<T>(string assetPath, uint preHash = 0) where T : Asset
        {
            T asset = GetCachedAsset<T>(assetPath, preHash, out uint hash);

            if (asset != null)
                return asset;

            // Not cached / Load the asset
            AssetLoader loader = assetLoaders[typeof(T)];
            asset = LoadAsset(assetPath, hash, preHash, loader) as T;
            asset.fPathHash = hash;
            RegisterAsset(asset, hash);

            return asset;
        }
        
        protected abstract Asset LoadAsset(string assetPath, uint hash, uint preHash, AssetLoader loader);
        
        internal Texture2D GetOrCreateTexture2D(string texPath, Sampler sampler, Vector2 spriteSize, uint preHash = 0, bool isLinear = false)
        {
            uint hash = preHash;
            if (hash == 0)
                hash = texPath.ToHash32();

            if (sampler == null)
                sampler = Graphics.linearSampleClamp;

            var tex2d = cachedTexture2Ds.FirstOrDefault(t => t.fPathHash == hash && t.textureSampler == sampler && t.spriteSize == spriteSize);
            if (tex2d != null)
                return tex2d;

            // Not cached, load texture
            Texture tex = LoadTexture(texPath, hash, preHash, isLinear);
            tex2d = new Texture2D(hash, tex, sampler, spriteSize, isLinear);
            cachedTexture2Ds.Add(tex2d);
            RegisterAsset(tex2d, hash);
            return tex2d;
        }
        
        protected abstract Texture LoadTexture(string texPath, uint hash, uint preHash, bool isLinear);
        
        internal virtual PipelineAsset GetPipeline(string pipelineName)
        {
            if(pipelineNameToHash.TryGetValue(pipelineName, out uint hash))
            {
                return LoadPipeline(hash);
            }

            return null;
        }

        protected abstract PipelineAsset LoadPipeline(uint hash);

        internal string GetUserShaderInclude(string includeName)
        {
            if (pipelineNameToHash.TryGetValue(includeName, out uint hash))
            {
                return LoadShaderInclude(hash);
            }

            return "";
        }
        
        protected abstract string LoadShaderInclude(uint hash);
        
        internal TextureView GetViewFromTexture(Texture texture)
        {
            if (!cachedViews.TryGetValue(texture, out TextureView view))
            {
                view = texture.CreateView();
                cachedViews.Add(texture, view);
            }

            return view;
        }

        internal Asset GetAsset(uint hash)
        {
            return assetDict.GetValueOrDefault(hash, null);
        }

        internal void ClearSceneCache()
        {
            cachedTexture2Ds.Clear();
            assetDict.Clear();
            
            LoadDefaultMaterials();
        }

        internal virtual void DisposeResources()
        {
            foreach (KeyValuePair<Texture, TextureView> kvp in cachedViews)
            {
                kvp.Value.Dispose();
            }
            cachedViews.Clear();
            
            // ABE Types
            cachedTexture2Ds.Clear();
        }

        internal List<string> GetUserPipelines()
        {
            return pipelineNameToHash.Keys.ToList();
        }
    }
}