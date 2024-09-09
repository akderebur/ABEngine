using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.Specialized;
using WGIL;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Friflo.Engine.ECS;
using WGIL.IO;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime
{
    public class SpriteBatchSystem : RenderSystem
    {
        class SpriteTextureGroup
        {
            private Texture2D spriteTexture;
            private Texture2D normalTexture;

            private Dictionary<PipelineMaterial, SpriteBatch> batches;
            
            // GPU
            private BindGroup textureBindGroup;

            public SpriteTextureGroup(Texture2D spriteTexture, Texture2D normalTexture, Buffer layerBuffer)
            {
                this.spriteTexture = spriteTexture;
                this.normalTexture = normalTexture;

                BindGroupDescriptor desc = new BindGroupDescriptor()
                {
                    BindGroupLayout = Graphics.sharedSpriteNormalLayout,
                    Entries = new BindResource[]
                    {
                        spriteTexture.GetView(),
                        spriteTexture.textureSampler,
                        spriteTexture.GetView(),
                        spriteTexture.textureSampler,
                        layerBuffer
                    }
                };
                textureBindGroup = Game.wgil.CreateBindGroup(ref desc);

                batches = new Dictionary<PipelineMaterial, SpriteBatch>();
            }

            public SpriteBatch AddSprite(ref Sprite sprite)
            {
                if (batches.TryGetValue(sprite.sharedMaterial, out SpriteBatch batch))
                {
                   batch.AddSprite();
                   sprite.batch = batch;
                   return null;
                }
                else
                {
                    // Create batch
                    batch = new SpriteBatch(sprite.sharedMaterial);
                    batch.AddSprite();
                    batches.Add(sprite.sharedMaterial, batch);
                    return batch;
                }
            }

            public void Render(RenderPass pass)
            {
                pass.SetBindGroup(1, textureBindGroup);
                foreach (var batch in batches.Values)
                {
                    batch.Render(pass);
                }
            }
        }

        class LayerContext
        {
            // Batched based on sprite and normal texture for each layer
            public int layerID { get; set; }
            private Dictionary<(Texture2D, Texture2D), SpriteTextureGroup> textureGroups;
            private List<SpriteBatch> batches;

            // GPU
            private Buffer layerBuffer;
            
            public LayerContext(int layerId)
            {
                layerID = layerId;
                layerBuffer = Game.wgil.CreateBuffer(16, BufferUsages.COPY_DST | BufferUsages.UNIFORM);
                Game.wgil.WriteBuffer(layerBuffer, new Vector4(layerId, 0f, 0f, 0f));

                textureGroups = new Dictionary<(Texture2D, Texture2D), SpriteTextureGroup>();
                batches = new List<SpriteBatch>();
            }

            public void AddSprite(ref Sprite sprite)
            {
                if (!textureGroups.TryGetValue((sprite.texture, sprite.normalTexture), out SpriteTextureGroup group))
                {
                    group = new SpriteTextureGroup(sprite.texture, sprite.normalTexture, layerBuffer);
                    textureGroups.Add((sprite.texture, sprite.normalTexture), group);
                }

                SpriteBatch batch = group.AddSprite(ref sprite);
                if (batch != null)
                {
                    batch.batchID = batches.Count;
                    sprite.batch = batch;
                    batches.Add(batch);
                }
            }

            public SpriteBatch GetBatch(int batchID)
            {
                return batches[batchID];
            }

            public void UpdateBatches()
            {
                foreach (var batch in batches)
                {
                    batch.UpdateBatch();
                }
            }
            
            public void InitBatches()
            {
                foreach (var batch in batches)
                {
                    batch.InitBatch();
                }
            }

            public void RenderBatches(RenderPass pass)
            {
                foreach (var textureGroup in textureGroups.Values)
                {
                    textureGroup.Render(pass);
                }
            }
        }

        private readonly ArchetypeQuery<Sprite> spriteQuery = Game.GameWorld.Query<Sprite>();
        private readonly ArchetypeQuery<Sprite, WorldTransform> spriteRenderQuery = Game.GameWorld.Query<Sprite, WorldTransform>();

        private List<LayerContext> layers;
        
        protected override void StartScene()
        {
            layers = new List<LayerContext>();
            for (int i = 0; i < 2; i++)
            {
                layers.Add(new LayerContext(i));
            }
            
            spriteQuery.ForEachEntity((ref Sprite sprite, Entity entity) => {
                layers[sprite.renderLayerIndex].AddSprite(ref sprite);
            });
            
            foreach (var layer in layers)
            {
                layer.InitBatches();
            }
        }

        void DoPipelineGrouping()
        {
        }
        

        internal void RemoveSprite(Sprite sprite, int oldRenderLayerID, Texture2D oldTex, int oldMatInsId)
        {

        }

        internal void DeleteBatch(RenderBatch batch)
        {
   
        }

        internal SpriteBatch GetBatchFromSprite(Transform spriteTrans, Sprite sprite, string extraKey)
        {
            return GetBatchFromSprite(spriteTrans, sprite, sprite.texture, extraKey);
        }

        internal SpriteBatch GetBatchFromSprite(Transform spriteTrans, Sprite sprite, Texture2D tex2D, string extraKey)
        {
           
            return null;
        }

        public void UpdateSpriteBatch(Sprite sprite, int oldRenderLayerID, Texture2D oldTex, int oldMatInsId)
        {
          
        }

        internal void AddGenericBatch(RenderBatch batch)
        {
            
        }

        internal void RemoveGenericBatch(RenderBatch batch)
        {
        }

        internal int DEBUG_GetBatchCount()
        {
            return 0;
        }

        internal SpriteBatch CreateSpriteBatch(Transform spriteTrans, Sprite sprite, Texture2D tex2D, string extraKey)
        {
            return null;
        }

        internal SpriteBatch AddSpriteToBatch(Transform spriteTrans, Sprite sprite, string extraKey)
        {
            return null;
        }

        internal SpriteBatch AddSpriteToBatch(Transform spriteTrans, Sprite sprite)
        {
            return AddSpriteToBatch(spriteTrans, sprite, "");
        }

        internal void UpdateBatchPipeline(RenderBatch sb)
        {
            
        }

        public override void Update(float gameTime, float deltaTime)
        {
            spriteRenderQuery.ForEachEntity((ref Sprite sprite, ref WorldTransform transform, Entity entity) =>
            {
                SpriteBatch batch = sprite.batch;
                batch.UpdateSprite(sprite, new Vector3(transform.matrix.M41, transform.matrix.M42, transform.matrix.M43), Vector3.One);
            });
            
            foreach (var layer in layers)
            {
                layer.UpdateBatches();
            }
        }

        public void RenderPP(RenderPass pass, int renderLayer)
        {
           
        }

        public override void Render(RenderPass pass, int renderLayer)
        {
            
            layers[renderLayer].RenderBatches(pass);
        }
    }
}
