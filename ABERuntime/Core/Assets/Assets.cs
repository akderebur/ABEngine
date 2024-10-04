using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.ECS;
using Friflo.Engine.ECS;
using Halak;
using WGIL;

namespace ABEngine.ABERuntime.Core.Assets;

public static class Assets
{
    internal static int guidMagic = 1230324289; // ABUI

    private static BaseAssetCache _currentCache;
    private static Texture2D _defTexture = null;

    // Scene specific serialize
    static List<Asset> sceneAssets = new();

    internal static void SetCache(BaseAssetCache cache)
    {
        _currentCache = cache;
        _currentCache.InitCache();
    }

    // Internal
    internal static Texture2D GetOrCreateTexture2D(string texPath, Sampler sampler, Vector2 spriteSize, uint preHash,
        bool isLinear)
    {
        return _currentCache.GetOrCreateTexture2D(texPath, sampler, spriteSize, preHash, isLinear);
    }

    internal static string GetUserShaderInclude(string includeName)
    {
        return _currentCache.GetUserShaderInclude(includeName);
    }

    internal static TextureView GetOrCreateTextureView(Texture texture, bool isCube = false)
    {
        if (texture == null)
            return _defTexture.GetView();

        return _currentCache.GetViewFromTexture(texture, isCube);
    }

    //General purpose
    public static Texture2D CreateTexture2D(string texturePath)
    {
        return _currentCache.GetOrCreateTexture2D(texturePath, Graphics.linearSampleClamp, Vector2.Zero);
    }

    public static Texture2D CreateTexture2D(string texturePath, Sampler sampler)
    {
        return _currentCache.GetOrCreateTexture2D(texturePath, sampler, Vector2.Zero);
    }

    public static Texture2D CreateTexture2D(string texturePath, Sampler sampler, bool isLinear)
    {
        return _currentCache.GetOrCreateTexture2D(texturePath, sampler, Vector2.Zero, 0, isLinear);
    }

    public static Texture2D CreateTexture2D(string texturePath, Sampler sampler, Vector2 spriteSize)
    {
        return _currentCache.GetOrCreateTexture2D(texturePath, sampler, spriteSize);
    }

    public static PipelineMaterial CreateMaterial(string matPath)
    {
        var newMat = _currentCache.GetOrCreateAsset<PipelineMaterial>(matPath, 0);
        return newMat;
    }

    public static PrefabAsset CreatePrefabAsset(string prefabAssetPath)
    {
        var newPrefab = _currentCache.GetOrCreateAsset<PrefabAsset>(prefabAssetPath, 0);
        return newPrefab;
    }

    public static SpriteClip CreateSpriteClip(string clipAssetPath)
    {
        var newSpriteClip = _currentCache.GetOrCreateAsset<SpriteClip>(clipAssetPath, 0);
        return newSpriteClip;
    }

    public static SpriteClip CreateSpriteClip(Texture2D tex2d, List<Vector2> framePoses)
    {
        SpriteClip newClip = new SpriteClip(tex2d, framePoses);
        return newClip;
    }

    public static Mesh CreateMesh(string meshAssetPath)
    {
        return _currentCache.GetOrCreateAsset<Mesh>(meshAssetPath);
    }

    public static AnimationClip CreateAnimationClip(string clipAssetPath)
    {
        return _currentCache.GetOrCreateAsset<AnimationClip>(clipAssetPath);
    }

    public static TextureCube CreateTextureCube(string[] texturePaths, Sampler sampler)
    {
        if (texturePaths.Length < 1)
            return null;

        Texture cubeTexture = null;
        uint depth = 0;
        uint maxDepth = (uint)texturePaths.Length;

        bool first = true;
        if (_currentCache is DebugAssetCache cache)
        {
            foreach (var path in texturePaths)
            { 
                var image = cache.GetImageDebug(Game.AssetPath.ToCommonPath() + path, false, false);
                
                if (first)
                {
                    first = false;
                    cubeTexture = Game.wgil.CreateTexture(image.Width, image.Height, 1, image.Format,
                        TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_DST, maxDepth);
                }

                image.WriteToCubemap(cubeTexture, depth);
                depth++;
            }
        }

        return  new TextureCube(0, cubeTexture, sampler, false);
    }

    public static PipelineAsset CreatePipelineAsset(string pipelineName, params MaterialFeature[] materialFeatures)
    {
        var pipeline = Graphics.GetPipelineAssetByName(pipelineName);
        if (pipeline == null)
        {
            // Try user pipeline
            pipeline = _currentCache.GetPipeline(pipelineName);
        }

        if (pipeline != null && materialFeatures.Length > 0)
        {
            string[] defines = new string[materialFeatures.Length];
            for (int i = 0; i < defines.Length; i++)
            {
                defines[i] = PipelineAsset.MatFeatureToKey[materialFeatures[i]];
            }

            pipeline = pipeline.GetPipelineVariant(defines);
        }

        return pipeline;
    }


    internal static Texture2D GetDefaultTexture()
    {
        if (_defTexture != null)
            return _defTexture;

        // Load texture
        Texture tex = Game.wgil.CreateTexture(128, 128, TextureFormat.Rgba8Unorm,
            TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_DST);

        var pixelData = new byte[128 * 128 * 4];
        byte data = 255;
        Array.Fill(pixelData, data);

        // Black edges
        /*int offset = 0;
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
        }*/

        Game.wgil.WriteTexture(tex, pixelData.AsSpan(), pixelData.Length, 4);

        _defTexture = new Texture2D(1, tex, Graphics.pointSamplerClamp, Vector2.Zero, false);
        ;
        return _defTexture;
    }

    public static Entity CreateModel(string modelAssetPath)
    {
        BinaryReader br = null;
        string modAssetFolder = "";
        if (Game.debug)
        {
            modAssetFolder = Path.GetDirectoryName(modelAssetPath);
            string modelAbsPath = Game.AssetPath + modelAssetPath;
            if (File.Exists(modelAbsPath))
                br = new BinaryReader(new FileStream(modelAbsPath, FileMode.Open));
            else
                return Entities.NullEntity;
        }
        else
        {
            // Load from PK
        }

        int nodeC = br.ReadInt32();
        int skelBoneC = br.ReadInt32();
        Entity[] nodes = new Entity[nodeC];
        Entity[] skeletonBones = new Entity[skelBoneC];

        int skellBoneInd = 0;
        for (int i = 0; i < nodeC; i++)
        {
            string nodeName = br.ReadString();
            int parId = br.ReadInt32();

            Entity nodeEnt = Entities.CreateEntity(nodeName);
            ref TRS nodeTrans = ref nodeEnt.GetComponent<TRS>();
            nodes[i] = nodeEnt;

            if (parId >= 0)
                nodeEnt.SetParent(nodes[parId]);

            if (br.ReadByte() == 1) // Is skeleton bone?
                skeletonBones[skellBoneInd++] = nodeEnt;

            nodeTrans.Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            nodeTrans.Rotation = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            nodeTrans.Scale = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

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
            _currentCache.GetOrCreateAsset<PipelineMaterial>("", matHash);
        }

        int staMeshCount = br.ReadInt32();
        int skinMeshCount = br.ReadInt32();

        for (int i = 0; i < skinMeshCount; i++)
        {
            uint meshHash = br.ReadUInt32();
            uint matHash = br.ReadUInt32();
            int nodeId = br.ReadInt32();

            Mesh mesh = _currentCache.GetOrCreateAsset<Mesh>("", meshHash);
            PipelineMaterial material = _currentCache.GetOrCreateAsset<PipelineMaterial>("", matHash);

            SkinnedMeshRenderer mr = new SkinnedMeshRenderer(mesh, material);
            ref TRS mrTrans = ref nodes[nodeId].GetComponent<TRS>();

            int boneCount = br.ReadInt32();
            mr.Bones = new Entity[boneCount];
            for (int b = 0; b < boneCount; b++)
            {
                mr.Bones[b] = nodes[br.ReadInt32()];
            }

            //mrTrans.entity.Add(mr);
        }
        
        br.Close();

        if (skinMeshCount > 0)
        {
            // Skeleton component
            Skeleton skeleton = new Skeleton()
            {
                Bones = skeletonBones
            };

            nodes[0].AddComponent(skeleton);
        }

        return nodes[0];
    }

    // Serialization - For scene

    internal static Sampler NameToSampler(string name)
    {
        switch (name)
        {
            case "LinearClamp":
                return Graphics.linearSampleClamp;
            case "LinearWrap":
                return Graphics.linearSamplerWrap;
            case "PointClamp":
                return Graphics.pointSamplerClamp;
            default:
                return Graphics.linearSampleClamp;
        }
    }

    private static Texture2D DeserializeTexture(JValue texAsset, uint hash)
    {
        string samplerName = texAsset["Sampler"];
        Sampler sampler = NameToSampler(samplerName);
        Vector2 spriteSize = new Vector2(texAsset["SpriteSizeX"], texAsset["SpriteSizeY"]);
        bool isLinear = texAsset["IsLinear"];

        return _currentCache.GetOrCreateTexture2D(null, sampler, spriteSize, hash, isLinear);
    }

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
                    curAsset = _currentCache.GetOrCreateAsset<PipelineMaterial>(null, hash);
                    break;
                case 2: // Prefab
                    curAsset = _currentCache.GetOrCreateAsset<PrefabAsset>(null, hash);
                    break;
                case 3: // Mesh
                    curAsset = _currentCache.GetOrCreateAsset<Mesh>(null, hash);
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
        Asset asset = _currentCache.GetAsset(hash);
        if (asset == null)
            return -1;

        int assetIndex = sceneAssets.IndexOf(asset);
        if (assetIndex == -1) // Not in the list
        {
            sceneAssets.Add(asset);
            return sceneAssets.Count - 1;
        }

        return assetIndex;
    }
    
    internal static Asset GetAssetFromSceneIndex(int index)
    {
        if (index < 0 || index >= sceneAssets.Count)
            return null;
        return sceneAssets[index];
    }

    internal static void ClearSerializeDependencies()
    {
        sceneAssets.Clear();
    }
    
    internal static void ClearSceneCache()
    {
        sceneAssets.Clear();
       _currentCache.ClearSceneCache();
    }

    internal static void DisposeResources()
    {
        _currentCache.DisposeResources();
        _defTexture = null;
    }
    
    internal static void AddAsset(Asset asset, string file)
    {
        throw new NotImplementedException();
    }
        
    internal static void UpdateAsset(uint oldHash, uint hash, string file)
    {
        throw new NotImplementedException();
    }

    internal static T GetAssetFromHash<T>(uint hash) where T : Asset
    {
        return _currentCache.GetAsset(hash) as T;
    }
    
    internal static List<string> GetUserPipelines()
    {
        return _currentCache.GetUserPipelines();
    }
}