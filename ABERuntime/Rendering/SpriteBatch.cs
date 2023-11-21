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
    public class SpriteBatch
    {
        private WGILContext _wgil;
        public PipelineMaterial material;

        public Buffer vertexBuffer;
        public Buffer layerBuffer;
   
        public BindGroup texSet;

        public uint instanceCount;
        public bool isStatic { get; set; }

        Texture2D texture2d;

        List<SpriteTransformPair> sprites = new List<SpriteTransformPair>();
        List<QuadVertex> verticesList = new List<QuadVertex>();

        QuadVertex[] vertices = null;

        public event Action onPropertyChanged;

        public float maxZ = 0f;
        public int renderLayerIndex = 0;

        internal event Action<SpriteBatch> onDelete;
        bool autoDestroy = true;
        internal bool active = false;

        Vector3 imageSize;

        public string key;

        public SpriteBatch(Texture2D texture, PipelineMaterial pipelineMaterial, int renderLayerIndex, bool isStatic)
        {
            _wgil = Game.wgil;

            this.texture2d = texture;
            imageSize = new Vector3(texture.imageSize.X, texture.imageSize.Y, 1f);

            this.material = pipelineMaterial;
            this.renderLayerIndex = renderLayerIndex;
            this.isStatic = isStatic;

            pipelineMaterial.onPipelineChanged += PipelineMaterial_onPipelineChanged;

            layerBuffer = _wgil.CreateBuffer(16, BufferUsages.UNIFORM | BufferUsages.COPY_DST);
            _wgil.WriteBuffer(layerBuffer, new Vector4(renderLayerIndex, 0, 0, 0));
        }

        private void PipelineMaterial_onPipelineChanged(PipelineAsset pipeline)
        {
            // Signal remove from  Pipeline Asset Pairs
            onDelete?.Invoke(this);

            Game.spriteBatchSystem.UpdateBatchPipeline(this);
        }

        public void AddSpriteEntity(Transform trans, Sprite spriteData)
        {
            if(!spriteData.sizeSet)
                spriteData.Resize(imageSize.ToVector2());

            QuadVertex quad = new QuadVertex(trans.worldPosition, spriteData.GetSize(), trans.worldScale);
            verticesList.Add(quad);

            sprites.Add(new SpriteTransformPair {  spriteData = spriteData, transform = trans});
            active = true;
        }

        public int RemoveSpriteEntity(Sprite sprite)
        {
            var spriteEntry = sprites.FirstOrDefault(sp => sp.spriteData == sprite);
            if(spriteEntry != null)
            {
                verticesList.RemoveAt(sprites.IndexOf(spriteEntry));
                sprites.Remove(spriteEntry);

                if(sprites.Count == 0) // Deactivate batch
                {
                    active = false;

                    if (autoDestroy) // Destroy batch
                    {
                        vertexBuffer.Dispose();
                        layerBuffer.Dispose();
                        texSet.Dispose();
                        onDelete?.Invoke(this);
                        material.onPipelineChanged -= PipelineMaterial_onPipelineChanged;
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

        internal void DeleteBatch()
        {
            verticesList.Clear();
            sprites.Clear();

            vertexBuffer.Dispose();
            layerBuffer.Dispose();
            texSet.Dispose();
            onDelete?.Invoke(this);
            material.onPipelineChanged -= PipelineMaterial_onPipelineChanged;
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

            if (isStatic)
            {
                // Static one time sort
                sprites = sprites.OrderBy(sp => sp.transform.worldPosition.Z).ToList();
                maxZ = sprites.Last().transform.worldPosition.Z;
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
            if(vertexBuffer != null)
                vertexBuffer.Dispose();

            vertexBuffer = _wgil.CreateBuffer(vertices.Length * (int)QuadVertex.VertexSize, BufferUsages.VERTEX | BufferUsages.COPY_DST);
            _wgil.WriteBuffer(vertexBuffer, vertices);
        }

        int runC = 0;
        public void UpdateBatch()
        {
            // Dynamic batches only
            if (!active || isStatic)
            {
                return;
            }

            // Write to GPU buffer
            QuadVertex[] writemap = new QuadVertex[sprites.Count];

            var sorted = sprites.Where(sp => sp.transform.enabled).OrderBy(sp => sp.transform.worldPosition.Z);
            int renderCount = sorted.Count();
            int index = 0;

            if (renderCount > 0)
            {
                maxZ = sorted.Last().transform.worldPosition.Z;
                foreach (var spritePair in sorted)
                {
                    Transform spriteTrans = spritePair.transform;
                    Sprite spriteData = spritePair.spriteData;

                    writemap[index++] = new QuadVertex(spriteTrans.worldPosition,
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
                writemap[index++] = new QuadVertex(Vector3.Zero,
                                         Vector2.Zero,
                                         Vector3.Zero,
                                         Vector4.Zero,
                                         0f,
                                         Vector2.Zero,
                                         Vector2.Zero,
                                         Vector2.Zero);
            }

            _wgil.WriteBuffer(vertexBuffer, writemap);
        }
    }

    class SpriteTransformPair
    {
        public Sprite spriteData { get; set; }
        public Transform transform { get; set; }
    }
}
