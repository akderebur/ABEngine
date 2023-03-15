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
   

        public DeviceBuffer _drawBuffer;

        protected ResourceFactory rsFactory;

        public ResourceSet drawSet;
        public ResourceSet texSet;

        public uint instanceCount;
        public bool isStatic { get; set; }

        TextureView view = null;
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

        //static Vector2[] unitVertices = new Vector2[] {
        //    new Vector2(0f, 0f),
        //    new Vector2(0f, 1f),
        //    new Vector2(1f, 1f),
        //    new Vector2(0f, 0f),
        //    new Vector2(1f, 1f),
        //    new Vector2(1f, 0f)
        //};

        //Vector2[] scaledVertices;
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
        }

        public void AddSpriteEntity(Entity spriteEnt)
        {
            Transform trans = spriteEnt.Get<Transform>();
            Sprite spriteData = spriteEnt.Get<Sprite>();

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
                    view.Dispose();
                    onDelete?.Invoke(this);
                    return 0;
                }

                InitBatch();
            }

            return sprites.Count;
        }

        public void InitBatch()
        {
            vertices = verticesList.ToArray();
            instanceCount = (uint)verticesList.Count;
            //indices = indicesList.ToArray();

            // Texture
            if (texture2d.fPathHash != 0 && view == null)
            {
                view = AssetCache.GetTextureView(texture2d.texture);
                texSet = rsFactory.CreateResourceSet(new ResourceSetDescription(
                    GraphicsManager.sharedTextureLayout,
                    view,
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



            //_indexBuffer = rsFactory.CreateBuffer(new BufferDescription((uint)indices.Length * sizeof(uint), BufferUsage.IndexBuffer));
            //_drawBuffer = rsFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            //_gd.UpdateBuffer(
            // _drawBuffer,
            // 0,
            // _drawData);
            //drawSet = rsFactory.CreateResourceSet(new ResourceSetDescription(PipelineManager.SpriteLayouts.Item2, _drawBuffer));

            _gd.UpdateBuffer(vertexBuffer, 0, vertices);
            //_gd.UpdateBuffer(propBuffer, 0, material.shaderPropData);

           
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



        //public void UpdateBatch(float deltaTime)
        //{
        //    for (int i = 0; i < sprites.Count; i++)
        //    {
        //        Entity spriteEnt = sprites[i];
        //        Transform spriteTrans = spriteEnt.Get<Transform>();
        //        Sprite spriteData = spriteEnt.Get<Sprite>();

        //        //Matrix4x4 lerpMat = Matrix4x4.Lerp(spriteTrans.lastWorldMatrix, spriteTrans.worldMatrix, interpolation);
        //        //Vector3 worldPos, worldScale;

        //        //Matrix4x4.Decompose(lerpMat, out worldScale, out _, out worldPos);


        //        vertices[i] = new QuadVertex(spriteTrans.worldPosition,
        //                                     spriteTrans.worldScale * spriteData.GetSize(),
        //                                     RgbaFloat.White,
        //                                     0f,
        //                                     spriteData.uvPos,
        //                                     spriteData.uvScale);
        //    }

        //    // Write to GPU buffer
        //    MappedResourceView<QuadVertex> writemap = _gd.Map<QuadVertex>(vertexBuffer, MapMode.Write);
        //    for (int i = 0; i < sprites.Count; i++)
        //    {
        //        writemap[i] = vertices[i];
        //    }
        //    _gd.Unmap(vertexBuffer);
        //}

        //public void Draw(ref Matrix4x4 projectionMatrix, ref Matrix4x4 viewMatrix)
        //{

        //    for (int s = 0; s < sprites.Count; s++)
        //    {
        //        Entity spriteEnt = sprites[s];
        //        Transform spriteTrans = spriteEnt.Get<Transform>();

        //        if (spriteTrans.isDirty)
        //        {
        //            int vertStart = 4 * s;
        //            int locInd = 0;
        //            for (int vInd = vertStart; vInd < vertStart + 4; vInd++)
        //            {
        //                vertices[vInd].Position = Vector2.Transform(scaledVertices[locInd++], spriteTrans.worldMatrix);
        //            }
        //            spriteTrans.isDirty = false;
        //        }
        //    }

        //    //ort.projection = Matrix4x4.CreateOrthographicOffCenter(0, _gd.MainSwapchain.Framebuffer.Width, 0, _gd.MainSwapchain.Framebuffer.Height, 0, 1);
        //    ort.projection = projectionMatrix;
        //    ort.model = viewMatrix;

        //    _gd.UpdateBuffer(
        //        _orthoBuffer,
        //        0,
        //        ort);
        //    //_gd.UpdateBuffer(
        //    //  _tintBuffer,
        //    //  0,
        //    //  tintC);

        //    // Set vertices
        //    _gd.UpdateBuffer(_vertexBuffer, 0, vertices);

        //    _cl.SetPipeline(_pipeline);
        //    _cl.SetGraphicsResourceSet(0, _orthoSet);
        //    _cl.SetGraphicsResourceSet(1, _texSet);
        //    _cl.SetVertexBuffer(0, _vertexBuffer);
        //    _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
        //    _cl.DrawIndexed((uint)indices.Length, 1, 0, 0, 0);
        //}
    }

    class SpriteTransformPair
    {
        public Sprite spriteData { get; set; }
        public Transform transform { get; set; }
    }
}
