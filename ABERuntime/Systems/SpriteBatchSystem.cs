using System;
using System.Numerics;
using System.Collections.Generic;
using WGIL;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Friflo.Engine.ECS;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime
{
    public class SpriteBatchSystem : RenderSystem
    {
        private readonly ArchetypeQuery<Sprite> spriteQuery = Game.GameWorld.Query<Sprite>();
        private readonly ArchetypeQuery<Sprite, WorldTransform> spriteRenderQuery = Game.GameWorld.Query<Sprite, WorldTransform>();

        private List<LayerContext> layers;

        private Buffer spriteTransformBuffer;
        private BindGroup spriteFrameBindGroup;
        
        internal void SetupResources()
        {
            if(spriteTransformBuffer != null)
                return;
            
            spriteTransformBuffer = wgil.CreateBuffer(64 * 300000, BufferUsages.STORAGE | BufferUsages.COPY_DST)
                .SetManualDispose(true);

            BindGroupDescriptor spriteFrameBGDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = Graphics.spriteSharedFrameLayout,
                Entries = new[]
                {
                    Game.pipelineBuffer,
                    spriteTransformBuffer
                }
            };

            spriteFrameBindGroup = wgil.CreateBindGroup(ref spriteFrameBGDesc).SetManualDispose(true);
        }
        
        protected override void StartScene()
        {
            layers = new List<LayerContext>();
            for (int i = 0; i < Graphics.renderLayers.Count; i++)
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
        
        public override void Update(float gameTime, float deltaTime)
        {
            int totalCount = 0;
            foreach (var (sprites, transforms, entities) in spriteRenderQuery.Chunks)
            {
                wgil.WriteBuffer(spriteTransformBuffer, transforms.Span, totalCount * 64, entities.Length * 64);
                
                for (int n = 0; n < entities.Length; n++)
                {
                    ref Sprite sprite = ref sprites[n];
                    if (sprite.batch == null)
                    {
                        layers[sprite.renderLayerIndex].AddSprite(ref sprite);
                    }
                    
                    totalCount++;
                    if (sprite.bvhGroup.IsCulled)
                    {
                        //Console.WriteLine("Cull");
                        //sprite.batch.UpdateSpriteTest(sprite, 0);
                        continue;
                    }
 
                    sprite.batch.UpdateSprite(sprite, totalCount - 1);
                }
            }
            
            foreach (var layer in layers)
            {
                layer.UpdateBatches();
            }
        }
        
        public override void Render(RenderPass pass)
        { 
            pass.SetBindGroup(0, spriteFrameBindGroup);
            layers[0].RenderBatches(pass);
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
                    //sprite.batchID = batch.batchID;
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
        
        class SpriteTextureGroup
        {
            private Dictionary<PipelineMaterial, SpriteBatch> batches;
            
            // GPU
            private BindGroup textureBindGroup;

            public SpriteTextureGroup(Texture2D spriteTexture, Texture2D normalTexture, Buffer layerBuffer)
            {
                BindGroupDescriptor desc = new BindGroupDescriptor()
                {
                    BindGroupLayout = Graphics.sharedSpriteNormalLayout,
                    Entries = new BindResource[]
                    {
                        spriteTexture.GetView(),
                        spriteTexture.textureSampler,
                        normalTexture.GetView(),
                        normalTexture.textureSampler,
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
    }
}
