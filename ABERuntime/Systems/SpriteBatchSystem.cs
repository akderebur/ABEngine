using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Veldrid;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABERuntime
{
    public class SpriteBatchSystem : RenderSystem
    {
        Dictionary<string, SpriteBatch> batches = new Dictionary<string, SpriteBatch>();
        Dictionary<int, List<AssetBatchPair>> renderGroups = new Dictionary<int, List<AssetBatchPair>>();

        private GraphicsDevice _gd;
        private CommandList _cl;
        private ResourceFactory _rsFactory;

        public SpriteBatchSystem(PipelineAsset asset) : base(asset) { }


        public override void Start()
        {
            batches = new Dictionary<string, SpriteBatch>();

            _gd = GraphicsManager.gd;
            _cl = GraphicsManager.cl;
            _rsFactory = GraphicsManager.rf;


            // Create batch groups
            var spriteQuery = Game.GameWorld.CreateQuery().Has<Sprite>();
            var layerGroups = spriteQuery.GetEntities().Where(e => e.enabled).GroupBy(s => s.Get<Sprite>().renderLayerIndex);

            foreach (var layerGroup in layerGroups)
            {
                List<AssetBatchPair> pairs = new List<AssetBatchPair>();
                renderGroups.Add(layerGroup.Key, pairs);

                var assetGroups = layerGroup.GroupBy(lg => lg.Get<Sprite>().sharedMaterial.pipelineAsset);

                foreach (var assetGroup in assetGroups)
                {
                    AssetBatchPair pair = new AssetBatchPair() { pipelineAsset = assetGroup.Key,
                                                                 layerIndex = layerGroup.Key };
                    pair.onDelete += AssetPair_onDelete;
                    pairs.Add(pair);

                    var textureGroups = assetGroup.GroupBy(lg => lg.Get<Sprite>().texture);
                    foreach (var textureGroup in textureGroups)
                    {
                        var matGroups = textureGroup.GroupBy(sg => sg.Get<Sprite>().sharedMaterial);

                        foreach (var matGroup in matGroups)
                        {
                            var statics = matGroup.Where(sg => sg.Get<Transform>().isStatic);
                            var dynamics = matGroup.Where(sg => !sg.Get<Transform>().isStatic);

                            if (statics.Count() > 0)
                            {
                                SpriteBatch sb = new SpriteBatch(textureGroup.Key, matGroup.Key, layerGroup.Key, true);
                                foreach (var spriteEnt in statics)
                                {
                                    sb.AddSpriteEntity(spriteEnt.transform, spriteEnt.Get<Sprite>());
                                }

                                sb.InitBatch();
                                batches.Add(layerGroup.Key + "_" + textureGroup.Key.textureID + "_" + matGroup.Key.instanceID + "_1", sb);
                                pair.batches.Add(sb);
                                sb.onDelete += pair.OnBatchDelete;
                            }

                            if (dynamics.Count() > 0)
                            {
                                SpriteBatch sb = new SpriteBatch(textureGroup.Key, matGroup.Key, layerGroup.Key, false);
                                foreach (var spriteEnt in dynamics)
                                {
                                    sb.AddSpriteEntity(spriteEnt.transform, spriteEnt.Get<Sprite>());
                                }

                                sb.InitBatch();
                                batches.Add(layerGroup.Key + "_" + textureGroup.Key.textureID + "_" + matGroup.Key.instanceID + "_0", sb);
                                pair.batches.Add(sb);
                                sb.onDelete += pair.OnBatchDelete;
                            }
                        }
                    }

                }
            }

            base.Start();
        }

        private void AssetPair_onDelete(AssetBatchPair pair)
        {
            renderGroups[pair.layerIndex].Remove(pair);
        }

        internal void RemoveSprite(Sprite sprite, int oldRenderLayerID, Texture2D oldTex, int oldMatInsId)
        {
            if (!started || sprite.transform == null)
                return;

            int staticKey = sprite.transform.isStatic ? 1 : 0;
            string key = oldRenderLayerID + "_" + oldTex.textureID + "_" + oldMatInsId + "_" + staticKey;

            if (batches.ContainsKey(key))
            {
                SpriteBatch batch = batches[key];
                int remCount = batch.RemoveSpriteEntity(sprite);
                if (remCount == 0)
                    batches.Remove(key);
            }
        }

        internal void DeleteBatch(SpriteBatch batch)
        {
            var batchKV = batches.FirstOrDefault(b => b.Value == batch);
            if (!string.IsNullOrEmpty(batchKV.Key))
                batches.Remove(batchKV.Key);
        }

        internal SpriteBatch GetBatchFromSprite(Transform spriteTrans, Sprite sprite, string extraKey)
        {
            PipelineMaterial mat = sprite.sharedMaterial;

            int staticKey = spriteTrans.isStatic ? 1 : 0;
            string key = sprite.renderLayerIndex + "_" + sprite.texture.textureID + "_" + mat.instanceID + "_" + staticKey + extraKey;

            if (batches.TryGetValue(key, out SpriteBatch batch))
                return batch;

            return null;
        }

        public void UpdateSpriteBatch(Sprite sprite, int oldRenderLayerID, Texture2D oldTex, int oldMatInsId)
        {
            if (!started || sprite.transform == null)
                return;

            int staticKey = sprite.transform.isStatic ? 1 : 0;
            string key = oldRenderLayerID + "_" + oldTex.textureID + "_" + oldMatInsId + "_" + staticKey;

            if (batches.TryGetValue(key, out SpriteBatch batch))
            {
                //SpriteBatch batch = batches[key];
                int remCount = batch.RemoveSpriteEntity(sprite);
                if (remCount == 0)
                    batches.Remove(key);
            }

            AddSpriteToBatch(sprite.transform, sprite);
        }

        internal int DEBUG_GetBatchCount()
        {
            return batches.Count;
        }

        internal void AddSpriteToBatch(Transform spriteTrans, Sprite sprite, string extraKey)
        {
            if (!started)
                return;

            PipelineMaterial mat = sprite.sharedMaterial;

            int staticKey = spriteTrans.isStatic ? 1 : 0;
            string key = sprite.renderLayerIndex + "_" + sprite.texture.textureID + "_" + mat.instanceID + "_" + staticKey + extraKey;

            if (batches.TryGetValue(key, out SpriteBatch batch))
            {
                batch.AddSpriteEntity(spriteTrans, sprite);
                batch.InitBatch();
            }
            else
            {
                SpriteBatch sb = new SpriteBatch(sprite.texture, mat, sprite.renderLayerIndex, spriteTrans.isStatic);
                sb.AddSpriteEntity(spriteTrans, sprite);
                sb.InitBatch();
                batches.Add(key, sb);

                // Add to render groups
                List<AssetBatchPair> pairList = null;
                if (renderGroups.ContainsKey(sprite.renderLayerIndex))
                {
                    pairList = renderGroups[sprite.renderLayerIndex];
                    AssetBatchPair pair = pairList.FirstOrDefault(p => p.pipelineAsset == sprite.sharedMaterial.pipelineAsset);
                    if (pair == null)
                    {
                        pair = new AssetBatchPair()
                        {
                            pipelineAsset = sprite.sharedMaterial.pipelineAsset,
                            layerIndex = sprite.renderLayerIndex
                        };
                        pairList.Add(pair);

                        pair.onDelete += AssetPair_onDelete;
                    }

                    pair.batches.Add(sb);
                    sb.onDelete += pair.OnBatchDelete;
                }
                else
                {
                    pairList = new List<AssetBatchPair>();
                    renderGroups.Add(sprite.renderLayerIndex, pairList);

                    AssetBatchPair pair = new AssetBatchPair()
                    {
                        pipelineAsset = sprite.sharedMaterial.pipelineAsset,
                        layerIndex = sprite.renderLayerIndex
                    };
                    pairList.Add(pair);

                    pair.batches.Add(sb);
                    pair.onDelete += AssetPair_onDelete;
                    sb.onDelete += pair.OnBatchDelete;
                }
            }
        }

        internal void AddSpriteToBatch(Transform spriteTrans, Sprite sprite)
        {
            AddSpriteToBatch(spriteTrans, sprite, "");
        }

        internal void UpdateBatchPipeline(SpriteBatch sb)
        {
            // Find suitable render group
            if(renderGroups.TryGetValue(sb.renderLayerIndex, out List<AssetBatchPair> pairL))
            {
                foreach (var pair in pairL)
                {
                    if(pair.pipelineAsset == sb.material.pipelineAsset)
                    {
                        pair.batches.Add(sb);
                        sb.onDelete += pair.OnBatchDelete;

                        return;
                    }
                }
            }

            // No group available. Create new
            List<AssetBatchPair> pairList = null;
            if (renderGroups.ContainsKey(sb.renderLayerIndex))
            {
                pairList = renderGroups[sb.renderLayerIndex];
                AssetBatchPair pair = pairList.FirstOrDefault(p => p.pipelineAsset == sb.material.pipelineAsset);
                if (pair == null)
                {
                    pair = new AssetBatchPair()
                    {
                        pipelineAsset = sb.material.pipelineAsset,
                        layerIndex = sb.renderLayerIndex
                    };
                    pairList.Add(pair);

                    pair.onDelete += AssetPair_onDelete;
                }

                pair.batches.Add(sb);
                sb.onDelete += pair.OnBatchDelete;
            }
            else
            {
                pairList = new List<AssetBatchPair>();
                renderGroups.Add(sb.renderLayerIndex, pairList);

                AssetBatchPair pair = new AssetBatchPair()
                {
                    pipelineAsset = sb.material.pipelineAsset,
                    layerIndex = sb.renderLayerIndex
                };
                pairList.Add(pair);

                pair.batches.Add(sb);
                pair.onDelete += AssetPair_onDelete;
                sb.onDelete += pair.OnBatchDelete;
            }
        }

        IEnumerable<IGrouping<int, IGrouping<PipelineAsset, KeyValuePair<string, SpriteBatch>>>> renderOrder;
        public override void Update(float gameTime, float deltaTime)
        {
            Game.pipelineData.Time = gameTime;

            foreach (var key in renderGroups.Keys)
            {
                foreach (var pair in renderGroups[key])
                {
                    foreach (var batch in pair.batches)
                    {
                        batch.UpdateBatch();
                    }

                    pair.SortBatches();
                }

                renderGroups[key] = renderGroups[key].OrderBy(pair => pair.maxZ).ToList();
            }
        }

        public override void Render(int renderLayer)
        {
            if (!renderGroups.ContainsKey(renderLayer))
                return;

            int rendC = 0;

            foreach (var group in renderGroups[renderLayer])
            {
                group.pipelineAsset.BindPipeline();

                foreach (var sb in group.batches)
                {
                    //if(sb.material.isLateRender)
                    //{
                    //    _cl.CopyTexture(Game.mainRenderTexture, Game.ScreenTexture);
                    //}

                    rendC++;
                    _cl.SetVertexBuffer(0, sb.vertexBuffer);

                    _cl.SetGraphicsResourceSet(1, sb.texSet);

                    // Material Resource Sets
                    foreach (var setKV in sb.material.bindableSets)
                    {
                        _cl.SetGraphicsResourceSet(setKV.Key, setKV.Value);
                    }

                    _cl.Draw(6, sb.instanceCount, 0, 0);
                }
            }

            //Console.WriteLine("Draw call: " + rendC);

        }
    }

    class AssetBatchPair
    {
        public int layerIndex { get; set; }
        public PipelineAsset pipelineAsset { get; set; }
        public List<SpriteBatch> batches = new List<SpriteBatch>();
        public float maxZ;

        public event Action<AssetBatchPair> onDelete;

        public void SortBatches()
        {
            batches = batches.OrderBy(b => b.maxZ).ToList();
            maxZ = batches.Last().maxZ;
        }

        public void OnBatchDelete(SpriteBatch sb)
        {
            sb.onDelete -= OnBatchDelete;
            batches.Remove(sb);

            if(batches.Count == 0) // Delete batch group
            {
                onDelete?.Invoke(this);
            }
            
        }
    }

}
