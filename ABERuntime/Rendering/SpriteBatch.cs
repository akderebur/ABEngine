using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Rendering
{
    public class SpriteBatch : RenderBatch
    {
        // GPU Resources
        public Buffer vertexBuffer;
        public BindGroup texSet;
        public Buffer layerBuffer;

        List<SpriteTransformPair> sprites = new List<SpriteTransformPair>();
        List<QuadVertex> verticesList = new List<QuadVertex>();

        QuadVertex[] vertices = null;

        bool autoDestroy = true;

        Vector3 imageSize;

        public SpriteBatch(Texture2D texture, PipelineMaterial pipelineMaterial, int renderLayerIndex, bool isStatic, float zValue)
            :  base(texture, pipelineMaterial, renderLayerIndex, isStatic, zValue)
        {
            imageSize = new Vector3(texture.imageSize.X, texture.imageSize.Y, 1f);

            layerBuffer = _wgil.CreateBuffer(16, BufferUsages.UNIFORM | BufferUsages.COPY_DST);
            _wgil.WriteBuffer(layerBuffer, new Vector4(renderLayerIndex, 0, 0, 0));
        }

        protected override void PipelineMaterial_onPipelineChanged(PipelineAsset pipeline)
        {
            // Signal remove from  Pipeline Asset Pairs
            base.TriggerDelete();

            bool oldTrans = isTransparent;
            if (pipeline.renderType == RenderType.Transparent)
                isTransparent = true;
            else
                isTransparent = false;
            if (oldTrans != isTransparent)
                InitBatch();

            renderOrder = material.renderOrder;

            Game.spriteBatchSystem.UpdateBatchPipeline(this);
        }

        public void AddSpriteEntity(Transform trans, Sprite spriteData)
        {
            if (!spriteData.sizeSet)
                spriteData.Resize(imageSize.ToVector2());

            QuadVertex quad = new QuadVertex(trans.worldPosition, spriteData.GetSize(), trans.worldScale);
            verticesList.Add(quad);

            sprites.Add(new SpriteTransformPair { spriteData = spriteData, transform = trans });
            active = true;
        }

        public int RemoveSpriteEntity(Sprite sprite)
        {
            var spriteEntry = sprites.FirstOrDefault(sp => sp.spriteData == sprite);
            if (spriteEntry != null)
            {
                verticesList.RemoveAt(sprites.IndexOf(spriteEntry));
                sprites.Remove(spriteEntry);

                if (sprites.Count == 0) // Deactivate batch
                {
                    active = false;

                    if (autoDestroy) // Destroy batch
                    {
                        vertexBuffer.Dispose();
                        layerBuffer.Dispose();
                        texSet.Dispose();
                        base.DeleteBatch();
                        return -1;
                    }
                }
                else
                    InitBatch();
            }

            return sprites.Count;
        }

        internal void SetAutoDestroy(bool autoDestroy)
        {
            this.autoDestroy = autoDestroy;
        }

        internal override void DeleteBatch()
        {
            verticesList.Clear();
            sprites.Clear();

            vertexBuffer.Dispose();
            texSet.Dispose();
            layerBuffer.Dispose();
            base.DeleteBatch();
        }

        public void InitBatch()
        {
            vertices = verticesList.ToArray();
            instanceCount = (uint)verticesList.Count;
            //indices = indicesList.ToArray();

            // Texture
            if (texture2d.fPathHash != 0 && texSet == null)
            {
                var layouts = material.pipelineAsset.GetResourceLayouts();
                if (layouts.Count > 1 && layouts[1] == GraphicsManager.sharedSpriteNormalLayout)
                {
                    // Sprite normals
                    Texture2D normalTex = AssetCache.GetDefaultTexture();
                    foreach (var sprite in sprites)
                    {
                        if (sprite.spriteData.normalTexture != null)
                        {
                            normalTex = sprite.spriteData.normalTexture;
                            break;
                        }
                    }

                    var texSetDesc = new BindGroupDescriptor()
                    {
                        BindGroupLayout = GraphicsManager.sharedSpriteNormalLayout,
                        Entries = new BindResource[]
                        {
                            texture2d.GetView(),
                            texture2d.textureSampler,
                            normalTex.GetView(),
                            normalTex.textureSampler,
                            layerBuffer
                        }
                    };

                    texSet = _wgil.CreateBindGroup(ref texSetDesc);
                }
                else
                {
                    var texSetDesc = new BindGroupDescriptor()
                    {
                        BindGroupLayout = GraphicsManager.sharedSpriteNormalLayout,
                        Entries = new BindResource[]
                        {
                            texture2d.GetView(),
                            texture2d.textureSampler,
                        }
                    };

                    texSet = _wgil.CreateBindGroup(ref texSetDesc);
                }
            }

            // Init sort
            if (isTransparent)
            {
                // Back to front - Transparent
                sprites = sprites.OrderBy(sp => sp.transform.worldPosition.Z).ToList();
                maxZ = sprites.Last().transform.worldPosition.Z;
            }
            else
            {
                // Front to back - Opaque
                sprites = sprites.OrderByDescending(sp => sp.transform.worldPosition.Z).ToList();
                maxZ = -sprites.First().transform.worldPosition.Z;
            }

            if (isStatic)
            {
                int index = 0;
                for (int i = 0; i < sprites.Count; i++)
                {
                    SpriteTransformPair spritePair = sprites[i];
                    Transform spriteTrans = spritePair.transform;
                    Sprite spriteData = spritePair.spriteData;

                    vertices[index++] = new QuadVertex(spriteTrans.worldPosition,
                                               spriteData.GetSize(),
                                               spriteTrans.worldScale,
                                               spriteData.tintColor,
                                               spriteTrans.localEulerAngles.Z,
                                               spriteData.uvPos,
                                               spriteData.uvScale,
                                               spriteData.pivot);
                }
            }

            // Buffer resources
            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            vertexBuffer = _wgil.CreateBuffer(vertices.Length * (int)QuadVertex.VertexSize, BufferUsages.VERTEX | BufferUsages.COPY_DST);
            _wgil.WriteBuffer(vertexBuffer, vertices);
        }

        int runC = 0;
        public override void UpdateBatch()
        {
            // Dynamic batches only
            if (!active || isStatic)
            {
                return;
            }

            // Write to GPU buffer

            var sorted = sprites.Where(sp => sp.transform.enabled);

            int renderCount = sorted.Count();
            int index = 0;

            if (renderCount > 0)
            {
                if (isDynamicSort) // Update sort
                {
                    sorted = sorted.OrderBy(sp => sp.transform.worldPosition.Z);
                }

                foreach (var spritePair in sorted)
                {
                    Transform spriteTrans = spritePair.transform;
                    Sprite spriteData = spritePair.spriteData;

                    vertices[index++] = new QuadVertex(spriteTrans.worldPosition,
                                               spriteData.GetSize(),
                                               spriteTrans.worldScale,
                                               spriteData.tintColor,
                                               spriteTrans.localEulerAngles.Z,
                                               spriteData.uvPos,
                                               spriteData.uvScale,
                                               spriteData.pivot);
                }

            }

            for (int i = 0; i < sprites.Count - renderCount; i++)
            {
                vertices[index++] = new QuadVertex(Vector3.Zero,
                                         Vector2.Zero,
                                         Vector3.Zero,
                                         Vector4.Zero,
                                         0f,
                                         Vector2.Zero,
                                         Vector2.Zero,
                                         Vector2.Zero);
            }

            _wgil.WriteBuffer(vertexBuffer, vertices);
        }

        internal override void Render(RenderPass pass)
        {
            pass.SetVertexBuffer(0, vertexBuffer);

            pass.SetBindGroup(1, texSet);

            // Material Resource Sets
            foreach (var setKV in material.bindableSets)
            {
                pass.SetBindGroup(setKV.Key, setKV.Value);
            }

            pass.Draw(6, (int)instanceCount);
        }
    }

    class SpriteTransformPair
    {
        public Sprite spriteData { get; set; }
        public Transform transform { get; set; }
    }
}
