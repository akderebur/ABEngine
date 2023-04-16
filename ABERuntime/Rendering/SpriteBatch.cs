using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.ECS;
using Veldrid;

namespace ABEngine.ABERuntime.Rendering
{
    public class SpriteBatch
    {
        protected GraphicsDevice _gd;
        protected CommandList _cl;
        public PipelineMaterial material;

        public DeviceBuffer vertexBuffer;
   
        protected ResourceFactory rsFactory;

        public ResourceSet texSet;

        public uint instanceCount;
        public bool isStatic { get; set; }

        //TextureView view = null;
        //string imgPath;

        Texture2D texture2d;

        //List<Sprite> sprites = new List<Sprite>();
        //List<Transform> transforms = new List<Transform>();

        List<SpriteTransformPair> sprites = new List<SpriteTransformPair>();

        //Dictionary<Sprite, Transform> sprites = new Dictionary<Sprite, Transform>();

        List<QuadVertex> verticesList = new List<QuadVertex>();

        QuadVertex[] vertices = null;

        public event Action onPropertyChanged;

        public float maxZ = 0f;
        public int renderLayerIndex = 0;

        internal event Action<SpriteBatch> onDelete;

        Vector3 imageSize;

        public SpriteBatch(Texture2D texture, PipelineMaterial pipelineMaterial, int renderLayerIndex, bool isStatic)
        {
            this.texture2d = texture;
            imageSize = new Vector3(texture.imageSize.X, texture.imageSize.Y, 1f);

            _gd = GraphicsManager.gd;
            _cl = GraphicsManager.cl;
            rsFactory = GraphicsManager.rf;
            this.material = pipelineMaterial;
            this.renderLayerIndex = renderLayerIndex;
            this.isStatic = isStatic;

            pipelineMaterial.onPipelineChanged += PipelineMaterial_onPipelineChanged;
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

            //QuadVertex quad = new QuadVertex(trans.worldPosition, trans.worldScale * spriteData.GetSize());
            QuadVertex quad = new QuadVertex(trans.worldPosition, trans.worldScale * spriteData.GetSize());
            verticesList.Add(quad);

            sprites.Add(new SpriteTransformPair {  spriteData = spriteData, transform = trans});
            //sprites.Add(spriteData, trans);
            //sprites.Add(spriteEnt);
        }

        public int RemoveSpriteEntity(Sprite sprite)
        {
            var spriteEntry = sprites.FirstOrDefault(sp => sp.spriteData == sprite);
            if(spriteEntry != null)
            {
                verticesList.RemoveAt(sprites.IndexOf(spriteEntry));
                sprites.Remove(spriteEntry);

                if(sprites.Count == 0) // Remove batch
                {
                    vertexBuffer.Dispose();
                    texSet.Dispose();
                    onDelete?.Invoke(this);
                    material.onPipelineChanged -= PipelineMaterial_onPipelineChanged;
                    return 0;
                }

                InitBatch();
            }

            return sprites.Count;
        }

        internal void DeleteBatch()
        {
            verticesList.Clear();
            sprites.Clear();

            vertexBuffer.Dispose();
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
                //view = AssetCache.GetTextureView(texture2d.texture);
                texSet = rsFactory.CreateResourceSet(new ResourceSetDescription(
                    GraphicsManager.sharedTextureLayout,
                    texture2d.texture,
                    texture2d.textureSampler));

                //defSize = new Vector2(texData.Width, texData.Height);
            }

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
                                           spriteTrans.worldScale * spriteData.GetSize(),
                                           spriteData.tintColor,
                                           spriteTrans.localEulerAngles.Z,
                                           spriteData.uvPos,
                                           spriteData.uvScale);
            }

            // Buffer resources
            if(vertexBuffer != null)
                vertexBuffer.Dispose();
            vertexBuffer = rsFactory.CreateBuffer(new BufferDescription((uint)vertices.Length * QuadVertex.VertexSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));

            _gd.UpdateBuffer(vertexBuffer, 0, vertices);           
        }

        int runC = 0;
        public void UpdateBatch()
        {
            //if (runC++ > 100)
            //    return;

            // Dynamic batches only
            if (isStatic)
            {
                return;
            }

            // Write to GPU buffer
            MappedResourceView<QuadVertex> writemap = _gd.Map<QuadVertex>(vertexBuffer, MapMode.Write);

            sprites = sprites.OrderBy(sp => sp.transform.worldPosition.Z).ToList();
            maxZ = sprites.Last().transform.worldPosition.Z;
            int index = 0;
            for (int i = 0; i < sprites.Count; i++)
            {
                SpriteTransformPair spritePair = sprites[i];
                Transform spriteTrans = spritePair.transform;
                Sprite spriteData = spritePair.spriteData;

                writemap[index++] = new QuadVertex(spriteTrans.worldPosition,
                                           spriteTrans.worldScale * spriteData.GetSize(),
                                           spriteData.tintColor,
                                           spriteTrans.localEulerAngles.Z,
                                           spriteData.uvPos,
                                           spriteData.uvScale);
            }

            //foreach (var sprite in sprites)
            //{
            //    Transform spriteTrans = sprite.entity.Get<Transform>();
            //    Sprite spriteData = sprite;

            //    //vertices[i] = new QuadVertexUber(spriteTrans.worldPosition,
            //    //                             spriteTrans.worldScale * spriteData.GetSize(),
            //    //                             RgbaFloat.White,
            //    //                             0f,
            //    //                             spriteData.uvPos,
            //    //                             spriteData.uvScale,
            //    //                             0f,
            //    //                             RgbaFloat.White,
            //    //                             0.8f);


            //    if (spriteTrans.worldPosition.Z > maxZ)
            //        maxZ = spriteTrans.worldPosition.Z;

            //}

         

            _gd.Unmap(vertexBuffer);
        }
    }

    class SpriteTransformPair
    {
        public Sprite spriteData { get; set; }
        public Transform transform { get; set; }
    }
}
