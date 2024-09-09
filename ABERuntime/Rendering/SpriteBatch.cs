using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Friflo.Engine.ECS;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Rendering
{
    public class SpriteBatch
    {
        // GPU Resources
        public Buffer vertexBuffer;
        QuadVertex[] vertices = null;

        bool autoDestroy = true;

        Vector3 imageSize;

        private PipelineMaterial material;
        private WGILContext _wgil;

        public int batchID = 0;
        private int spriteCount = 0;
        private int instanceCount = 0;
        private int renderCount = 0;

        public SpriteBatch(PipelineMaterial pipelineMaterial)
        {
            _wgil = Game.wgil;
            material = pipelineMaterial;
            vertices = new QuadVertex [10];
        }

        public void AddSprite()
        {
            spriteCount++;
            if (vertices.Length < spriteCount)
            {
                Array.Resize(ref vertices, (int)MathF.Floor(spriteCount * 1.5f));
                if (vertexBuffer != null)
                {
                    vertexBuffer.Dispose();
                    vertexBuffer = _wgil.CreateBuffer(vertices.Length * (int)QuadVertex.VertexSize,
                        BufferUsages.VERTEX | BufferUsages.COPY_DST);
                }
            }
        }
        

        public void UpdateSprite(in Sprite sprite, in Vector3 position, in Vector3 scale)
        {
            vertices[instanceCount++] = new QuadVertex(position,
                sprite.GetSize(),
                scale * 0.01f,
                sprite.tintColor,
                0f,
                sprite.uvPos,
                sprite.uvScale,
                sprite.pivot);
        }

        internal void DeleteBatch()
        {
            vertexBuffer.Dispose();
            vertices = null;
        }

        public void InitBatch()
        {
            // Buffer resources
            if (vertexBuffer != null)
                vertexBuffer.Dispose();

            vertexBuffer = _wgil.CreateBuffer(vertices.Length * (int)QuadVertex.VertexSize,
                BufferUsages.VERTEX | BufferUsages.COPY_DST);
        }

        public void UpdateBatch()
        {
            // Write to GPU buffer
            _wgil.WriteBuffer(vertexBuffer, vertices, 0, instanceCount * (int)QuadVertex.VertexSize);
            renderCount = instanceCount;
            instanceCount = 0;
        }

        internal void Render(RenderPass pass)
        {
            pass.SetPipeline(material.pipelineAsset.pipeline);
            pass.SetBindGroup(0, Game.pipelineSet);
            pass.SetVertexBuffer(0, vertexBuffer);

            // Material Resource Sets
            foreach (var setKV in material.bindableSets)
            {
                pass.SetBindGroup(setKV.Key, setKV.Value);
            }

            pass.Draw(6, renderCount);
        }
    }
}
