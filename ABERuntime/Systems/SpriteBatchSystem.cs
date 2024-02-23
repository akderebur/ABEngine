using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using WGIL;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Components;
using Arch.Core;
using Arch.Core.Extensions;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime
{
    public class SpriteBatchSystem : RenderSystem
    {
        Dictionary<string, SpriteBatch> batches = new Dictionary<string, SpriteBatch>();
        //Dictionary<int, List<AssetBatchPair>> renderGroups = new Dictionary<int, List<AssetBatchPair>>();

        SortedDictionary<int, LayerContext> layerRenderGroups = new SortedDictionary<int, LayerContext>();


        public SpriteBatchSystem() : base() { }

        class OrderRender
        {
            public List<AssetBatchPair> pairList { get; set; }
            public bool isSorted;

            public OrderRender()
            {
                pairList = new List<AssetBatchPair>();
            }

            public void AddAssetPair(AssetBatchPair pair, bool isTransparent)
            {
                isSorted = isSorted || isTransparent;
                pairList.Add(pair);
                if (isSorted)
                   pairList = pairList.OrderBy(p => p.zValue).ToList();
            }

            public void Remove(AssetBatchPair pair)
            {
                pairList.Remove(pair);
            }

            public void UpdateOrder()
            {
                foreach (var pair in pairList)
                {
                    pair.UpdateBatches();
                }
            }
        }
         

        class LayerContext
        {
            public SortedDictionary<int, OrderRender> orderRenders { get; set; }
            public SortedSet<int> orderKeys { get; set; }
            public SortedSet<int> postKeys { get; set; }

            public int layerID { get; set; }

            public LayerContext(int layerId)
            {
                orderRenders = new SortedDictionary<int, OrderRender>();
                orderKeys = new SortedSet<int>();
                postKeys = new SortedSet<int>();

                layerID = layerId;
            }

            public void AddRenderToOrder(int orderId, AssetBatchPair renderPair, bool isTransparent)
            {
                if (orderId >= (int)RenderOrder.PostProcess)
                    postKeys.Add(orderId);
                else
                    orderKeys.Add(orderId);

                if (orderRenders.TryGetValue(orderId, out OrderRender orderRender))
                    orderRender.AddAssetPair(renderPair, isTransparent);
                else
                {
                    orderRender = new OrderRender();
                    orderRender.AddAssetPair(renderPair, isTransparent);
                    orderRenders.Add(orderId, orderRender);
                }
                   
                renderPair.renderOrder = orderId;
            }

            public void RemoveRender(AssetBatchPair renderPair)
            {
                if (renderPair.renderOrder >= (int)RenderOrder.PostProcess)
                    postKeys.Remove(renderPair.renderOrder);
                else
                    orderKeys.Remove(renderPair.renderOrder);

                orderRenders[renderPair.renderOrder].Remove(renderPair);
            }

            public void UpdateLayer()
            {
                foreach (var order in orderRenders.Values)
                {
                    order.UpdateOrder();
                }
            }
        }

        protected override void StartScene()
        {
            batches = new Dictionary<string, SpriteBatch>();
            layerRenderGroups = new SortedDictionary<int, LayerContext>();

            // Create batch groups
            var query = new QueryDescription().WithAll<Sprite>();
            var entities = new List<Entity>();
            Game.GameWorld.GetEntities(query, entities);

            var layerGroups = entities.GroupBy(s => s.Get<Sprite>().renderLayerIndex);

            foreach (var layerGroup in layerGroups)
            {
                //List<AssetBatchPair> pairs = new List<AssetBatchPair>();
                //renderGroups.Add(layerGroup.Key, pairs);

                int layerId = layerGroup.Key;
                LayerContext layerContext = new LayerContext(layerId);
                layerRenderGroups.Add(layerId, layerContext);


                // Opaque / PP
                var opaqueGroups = layerGroup.Where(s => s.Get<Sprite>().sharedMaterial.pipelineAsset.renderType != RenderType.Transparent);
                DoPipelineGrouping(opaqueGroups.ToList(), layerContext);


                //foreach (var assetGroup in opaqueGroups)
                //{
                //    AssetBatchPair pair = new AssetBatchPair() { pipelineAsset = assetGroup.Key,
                //                                                 layerIndex = layerGroup.Key };
                //    pair.onDelete += AssetPair_onDelete;
                //    pairs.Add(pair);

                //    var textureGroups = assetGroup.GroupBy(lg => lg.Get<Sprite>().texture);
                //    foreach (var textureGroup in textureGroups)
                //    {
                //        var matGroups = textureGroup.GroupBy(sg => sg.Get<Sprite>().sharedMaterial);

                //        foreach (var matGroup in matGroups)
                //        {
                //            var statics = matGroup.Where(sg => sg.Get<Transform>().isStatic);
                //            var dynamics = matGroup.Where(sg => !sg.Get<Transform>().isStatic);

                //            if (statics.Count() > 0)
                //            {
                //                SpriteBatch sb = new SpriteBatch(textureGroup.Key, matGroup.Key, layerGroup.Key, true);
                //                foreach (var spriteEnt in statics)
                //                {
                //                    sb.AddSpriteEntity(spriteEnt.Get<Transform>(), spriteEnt.Get<Sprite>());
                //                }

                //                sb.InitBatch();
                //                batches.Add(layerGroup.Key + "_" + textureGroup.Key.textureID + "_" + matGroup.Key.instanceID + "_1", sb);
                //                pair.batches.Add(sb);
                //                sb.onDelete += pair.OnBatchDelete;
                //            }

                //            if (dynamics.Count() > 0)
                //            {
                //                SpriteBatch sb = new SpriteBatch(textureGroup.Key, matGroup.Key, layerGroup.Key, false);
                //                foreach (var spriteEnt in dynamics)
                //                {
                //                    sb.AddSpriteEntity(spriteEnt.Get<Transform>(), spriteEnt.Get<Sprite>());
                //                }

                //                sb.InitBatch();
                //                batches.Add(layerGroup.Key + "_" + textureGroup.Key.textureID + "_" + matGroup.Key.instanceID + "_0", sb);
                //                pair.batches.Add(sb);
                //                sb.onDelete += pair.OnBatchDelete;
                //            }
                //        }
                //    }
                //}

                // Transparent
                var transparentGroups = layerGroup.Where(s => s.Get<Sprite>().sharedMaterial.pipelineAsset.renderType == RenderType.Transparent)
                                                  .GroupBy(lg => lg.Get<Transform>().worldPosition.Z);

                foreach (var zOrderGroup in transparentGroups)
                {
                    DoPipelineGrouping(zOrderGroup.ToList(), layerContext, zOrderGroup.Key, true);
                }
            }
        }

        void DoPipelineGrouping(List<Entity> spriteEnts, LayerContext layerContext, float zValue = 0f, bool isTransparent = false)
        {
            int layer = layerContext.layerID;

            // Render order grouping based on material render order
            var orderGroups = spriteEnts.GroupBy(se => se.Get<Sprite>().sharedMaterial.renderOrder);
            foreach (var orderGroup in orderGroups)
            {
                var pipelineGroups = orderGroup.GroupBy(se => se.Get<Sprite>().sharedMaterial.pipelineAsset);
                foreach (var assetGroup in pipelineGroups)
                {
                    AssetBatchPair pair = new AssetBatchPair()
                    {
                        pipelineAsset = assetGroup.Key,
                        layerIndex = layer,
                        zValue = zValue
                    };
                    pair.onDelete += AssetPair_onDelete;
                    layerContext.AddRenderToOrder(orderGroup.Key, pair, isTransparent);

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
                                SpriteBatch sb = new SpriteBatch(textureGroup.Key, matGroup.Key, layer, true, zValue);
                                foreach (var spriteEnt in statics)
                                {
                                    sb.AddSpriteEntity(spriteEnt.Get<Transform>(), spriteEnt.Get<Sprite>());
                                }

                                sb.InitBatch();
                                string batchKey = layer + "_" + textureGroup.Key.textureID + "_" + matGroup.Key.instanceID + "_1";
                                if (isTransparent) // Z-Key
                                    batchKey += "_" + zValue;
                                batches.Add(batchKey, sb);
                                pair.batches.Add(sb);
                                sb.onDelete += pair.OnBatchDelete;
                            }

                            if (dynamics.Count() > 0)
                            {
                                SpriteBatch sb = new SpriteBatch(textureGroup.Key, matGroup.Key, layer, false, zValue);
                                foreach (var spriteEnt in dynamics)
                                {
                                    sb.AddSpriteEntity(spriteEnt.Get<Transform>(), spriteEnt.Get<Sprite>());
                                }

                                sb.InitBatch();
                                string batchKey = layer + "_" + textureGroup.Key.textureID + "_" + matGroup.Key.instanceID + "_0";
                                if (isTransparent) // Z-Key
                                    batchKey += "_" + zValue;
                                batches.Add(batchKey, sb);
                                pair.batches.Add(sb);
                                sb.onDelete += pair.OnBatchDelete;
                            }
                        }
                    }
                }
            }
        }

        private void AssetPair_onDelete(AssetBatchPair pair)
        {
            layerRenderGroups[pair.layerIndex].RemoveRender(pair);
        }

        internal void RemoveSprite(Sprite sprite, int oldRenderLayerID, Texture2D oldTex, int oldMatInsId)
        {
            if (!started || sprite.transform == null)
                return;

            int staticKey = sprite.transform.isStatic ? 1 : 0;
            string key = oldRenderLayerID + "_" + oldTex.textureID + "_" + oldMatInsId + "_" + staticKey;
            if (sprite.sharedMaterial.pipelineAsset.renderType == RenderType.Transparent)
                key += "_" + sprite.transform.worldPosition.Z;

            if (batches.ContainsKey(key))
            {
                SpriteBatch batch = batches[key];
                int remCount = batch.RemoveSpriteEntity(sprite);
                if (remCount == -1)
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
            return GetBatchFromSprite(spriteTrans, sprite, sprite.texture, extraKey);
        }

        internal SpriteBatch GetBatchFromSprite(Transform spriteTrans, Sprite sprite, Texture2D tex2D, string extraKey)
        {
            PipelineMaterial mat = sprite.sharedMaterial;

            int staticKey = spriteTrans.isStatic ? 1 : 0;
            string key = sprite.renderLayerIndex + "_" + tex2D.textureID + "_" + mat.instanceID + "_" + staticKey;
            if (mat.pipelineAsset.renderType == RenderType.Transparent)
                key += "_" + spriteTrans.worldPosition.Z;
            key += extraKey;

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
            if (sprite.sharedMaterial.pipelineAsset.renderType == RenderType.Transparent)
                key += "_" + sprite.transform.worldPosition.Z;

            if (batches.TryGetValue(key, out SpriteBatch batch))
            {
                //SpriteBatch batch = batches[key];
                int remCount = batch.RemoveSpriteEntity(sprite);
                if (remCount == -1)
                    batches.Remove(key);
            }

            AddSpriteToBatch(sprite.transform, sprite);
        }

        internal int DEBUG_GetBatchCount()
        {
            return batches.Count;
        }

        internal SpriteBatch CreateSpriteBatch(Transform spriteTrans, Sprite sprite, Texture2D tex2D, string extraKey)
        {
            PipelineMaterial mat = sprite.sharedMaterial;

            int staticKey = spriteTrans.isStatic ? 1 : 0;
            string key = sprite.renderLayerIndex + "_" + tex2D.textureID + "_" + mat.instanceID + "_" + staticKey;
            if (mat.pipelineAsset.renderType == RenderType.Transparent)
                key += "_" + spriteTrans.worldPosition.Z;
            key += extraKey;

            if (batches.TryGetValue(key, out SpriteBatch batch))
                return batch;

            batch = new SpriteBatch(tex2D, mat, sprite.renderLayerIndex, spriteTrans.isStatic, spriteTrans.worldPosition.Z);
            batches.Add(key, batch);
            UpdateBatchPipeline(batch);
            return batch;
        }

        internal SpriteBatch AddSpriteToBatch(Transform spriteTrans, Sprite sprite, string extraKey)
        {
            if (!started)
                return null;

            PipelineMaterial mat = sprite.sharedMaterial;
            bool isTransparent = mat.pipelineAsset.renderType == RenderType.Transparent;

            int staticKey = spriteTrans.isStatic ? 1 : 0;
            string key = sprite.renderLayerIndex + "_" + sprite.texture.textureID + "_" + mat.instanceID + "_" + staticKey;
            if (isTransparent)
                key += "_" + spriteTrans.worldPosition.Z;
            key += extraKey;

            if (batches.TryGetValue(key, out SpriteBatch batch))
            {
                batch.AddSpriteEntity(spriteTrans, sprite);
                batch.InitBatch();
                return batch;
            }
            else
            {
                SpriteBatch sb = new SpriteBatch(sprite.texture, mat, sprite.renderLayerIndex, spriteTrans.isStatic, spriteTrans.worldPosition.Z);
                sb.key = key;
                sb.AddSpriteEntity(spriteTrans, sprite);
                sb.InitBatch();
                batches.Add(key, sb);

                // Add to render groups

                // Check layer
                if (layerRenderGroups.TryGetValue(sprite.renderLayerIndex, out LayerContext layerContext))
                {
                    // Check render order
                    AssetBatchPair pair = null;
                    if(layerContext.orderRenders.TryGetValue(sprite.sharedMaterial.renderOrder, out OrderRender orderRender))
                    {
                        // Check suitable pipeline

                        if (isTransparent) // Match Z-Value
                        {
                            pair = orderRender.pairList.FirstOrDefault(p => p.pipelineAsset == sprite.sharedMaterial.pipelineAsset &&
                                                                                       p.zValue == spriteTrans.worldPosition.Z);
                        }
                        else
                            pair = orderRender.pairList.FirstOrDefault(p => p.pipelineAsset == sprite.sharedMaterial.pipelineAsset);
                    }

                    if(pair == null)
                    {
                        pair = new AssetBatchPair()
                        {
                            pipelineAsset = sprite.sharedMaterial.pipelineAsset,
                            layerIndex = sprite.renderLayerIndex
                        };
                        if (isTransparent)
                            pair.zValue = spriteTrans.worldPosition.Z;
                        layerContext.AddRenderToOrder(sprite.sharedMaterial.renderOrder, pair, isTransparent);
                        pair.onDelete += AssetPair_onDelete;
                    }

                    pair.batches.Add(sb);
                    sb.onDelete += pair.OnBatchDelete;
                }
                else
                {
                    layerContext = new LayerContext(sprite.renderLayerIndex);

                    AssetBatchPair pair = new AssetBatchPair()
                    {
                        pipelineAsset = sprite.sharedMaterial.pipelineAsset,
                        layerIndex = sprite.renderLayerIndex
                    };
                    if (isTransparent)
                        pair.zValue = spriteTrans.worldPosition.Z;

                    pair.batches.Add(sb);
                    pair.onDelete += AssetPair_onDelete;
                    sb.onDelete += pair.OnBatchDelete;

                    layerContext.AddRenderToOrder(sprite.sharedMaterial.renderOrder, pair, isTransparent);
                    layerRenderGroups.Add(sprite.renderLayerIndex, layerContext);
                }

                return sb;
            }
        }

        internal SpriteBatch AddSpriteToBatch(Transform spriteTrans, Sprite sprite)
        {
            return AddSpriteToBatch(spriteTrans, sprite, "");
        }

        internal void UpdateBatchPipeline(SpriteBatch sb)
        {
            // Find suitable render group
            if (layerRenderGroups.TryGetValue(sb.renderLayerIndex, out LayerContext layerContext))
            {
                // Check render order
                AssetBatchPair pair = null;
                if (layerContext.orderRenders.TryGetValue(sb.renderOrder, out OrderRender orderRender))
                {
                    // Check suitable pipeline

                    if (sb.isTransparent) // Match Z-Value
                    {
                        pair = orderRender.pairList.FirstOrDefault(p => p.pipelineAsset == sb.material.pipelineAsset &&
                                                                   p.zValue == sb.zValue);
                    }
                    else
                        pair = orderRender.pairList.FirstOrDefault(p => p.pipelineAsset == sb.material.pipelineAsset);
                }

                if (pair == null)
                {
                    pair = new AssetBatchPair()
                    {
                        pipelineAsset = sb.material.pipelineAsset,
                        layerIndex = sb.renderLayerIndex
                    };
                    if (sb.isTransparent)
                        pair.zValue = sb.zValue;
                    layerContext.AddRenderToOrder(sb.material.renderOrder, pair, sb.isTransparent);
                    pair.onDelete += AssetPair_onDelete;
                }

                pair.batches.Add(sb);
                sb.onDelete += pair.OnBatchDelete;
            }
            else
            {
                layerContext = new LayerContext(sb.renderLayerIndex);

                AssetBatchPair pair = new AssetBatchPair()
                {
                    pipelineAsset = sb.material.pipelineAsset,
                    layerIndex = sb.renderLayerIndex
                };
                if (sb.isTransparent)
                    pair.zValue = sb.zValue;

                pair.batches.Add(sb);
                pair.onDelete += AssetPair_onDelete;
                sb.onDelete += pair.OnBatchDelete;

                layerContext.AddRenderToOrder(sb.material.renderOrder, pair, sb.isTransparent);
                layerRenderGroups.Add(sb.renderLayerIndex, layerContext);
            }
        }

        public override void Update(float gameTime, float deltaTime)
        {
            foreach (var layerContext in layerRenderGroups.Values)
            {
                layerContext.UpdateLayer();
            }
        }

        public void RenderPP(RenderPass pass, int renderLayer)
        {
            if (!layerRenderGroups.ContainsKey(renderLayer))
                return;

            LayerContext layerContext = layerRenderGroups[renderLayer];
            foreach (var orderKey in layerContext.postKeys)
            {
                OrderRender order = layerContext.orderRenders[orderKey];
                foreach (var group in order.pairList)
                {
                    group.pipelineAsset.BindPipeline(pass);

                    foreach (var sb in group.batches)
                    {
                        if (!sb.active)
                            continue;

                        //rendC++;
                        pass.SetVertexBuffer(0, sb.vertexBuffer);

                        pass.SetBindGroup(1, sb.texSet);

                        // Material Resource Sets
                        foreach (var setKV in sb.material.bindableSets)
                        {
                            pass.SetBindGroup(setKV.Key, setKV.Value);
                        }

                        pass.Draw(6, (int)sb.instanceCount);
                    }
                }
            }
        }

        public override void Render(RenderPass pass, int renderLayer)
        {
            if (!layerRenderGroups.ContainsKey(renderLayer))
                return;

            int rendC = 0;

            LayerContext layerContext = layerRenderGroups[renderLayer];
            foreach (var orderKey in layerContext.orderKeys)
            {
                OrderRender order = layerContext.orderRenders[orderKey];
                foreach (var group in order.pairList)
                {
                    group.pipelineAsset.BindPipeline(pass);

                    foreach (var sb in group.batches)
                    {
                        if (!sb.active)
                            continue;

                        //rendC++;
                        pass.SetVertexBuffer(0, sb.vertexBuffer);

                        pass.SetBindGroup(1, sb.texSet);

                        // Material Resource Sets
                        foreach (var setKV in sb.material.bindableSets)
                        {
                            pass.SetBindGroup(setKV.Key, setKV.Value);
                        }

                        pass.Draw(6, (int)sb.instanceCount);
                    }
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
        public float zValue; // Transparent only
        public int renderOrder;

        public event Action<AssetBatchPair> onDelete;

        public void UpdateBatches()
        {
            foreach (var batch in batches)
            {
                batch.UpdateBatch();
            }
        }

        //public void SortBatches()
        //{
        //    batches = batches.OrderBy(b => b.maxZ).ToList();
        //    maxZ = batches.Last().maxZ;
        //}

        public void OnBatchDelete(SpriteBatch sb)
        {
            sb.onDelete -= OnBatchDelete;
            batches.Remove(sb);

            if (batches.Count == 0) // Delete batch group
            {
                onDelete?.Invoke(this);
            }

        }
    }

}
