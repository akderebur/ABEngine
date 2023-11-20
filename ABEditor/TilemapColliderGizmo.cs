using System;
using ABEngine.ABERuntime.Debug;
using System.Numerics;
using System.Collections.Generic;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABEditor.ComponentDrawers;
using System.Linq;
using WGIL;
using Buffer = WGIL.Buffer;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEditor.Debug
{
    class ChunkBufferInfo
    {
        public Buffer buffer { get; set; }
        public int drawC { get; set; }
    }

    internal class TilemapColliderGizmo : RenderSystem
    {
        Dictionary<CollisionChunk, ChunkBufferInfo> chunkBuffers;

        public TilemapColliderGizmo(PipelineAsset asset) : base(asset) { }

        Vector4 normalColor = new Vector4(0, 1, 0, 1);
        Vector4 selColor = new Vector4(1, 0, 0, 1);
        Vector4 noCollisionColor = new Vector4(0, 0, 1, 1);

        public bool render = false;

        public override void Awake()
        {
            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            chunkBuffers = new Dictionary<CollisionChunk, ChunkBufferInfo>();
            TilemapDrawer.onCollisionUpdate += UpdateChunk;
            //linePointsBuffer = rf.CreateBuffer(new BufferDescription((uint)(5 * LinePoint.VertexSize), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        internal void ResetGizmo()
        {
            foreach (var chunkBuffer in chunkBuffers.Values)
            {
                chunkBuffer.buffer.Dispose();
            }

            chunkBuffers.Clear();
        }

        internal void InitBuffers()
        {
            var copy = chunkBuffers.Keys.ToList();
            foreach (var chunk in copy)
            {
                UpdateChunk(chunk);
            }
        }

        internal void UpdateChunk(CollisionChunk chunk)
        {
            if (chunk == null)
                return;

            Vector4 color = normalColor;
            if (chunk == TilemapDrawer.selectedChunk)
                color = selColor;
            else if (!chunk.collisionActive)
                color = noCollisionColor;

            var points = chunk.GetCollisionShape();

            if (points == null || points.Count == 0)
            {
                if (chunkBuffers.ContainsKey(chunk))
                {
                    chunkBuffers[chunk].buffer.Dispose();
                    chunkBuffers.Remove(chunk);
                }
                return;
            }

            Buffer linePointsBuffer = null;
            if (chunkBuffers.ContainsKey(chunk))
            {
                linePointsBuffer = chunkBuffers[chunk].buffer;
                linePointsBuffer.Dispose();

                linePointsBuffer = wgil.CreateBuffer(LinePoint.VertexSize * points.Count, BufferUsages.VERTEX | BufferUsages.COPY_DST);
                chunkBuffers[chunk] = new ChunkBufferInfo() { buffer = linePointsBuffer, drawC = (points.Count)};
            }
            else
            {
                linePointsBuffer = wgil.CreateBuffer(LinePoint.VertexSize * points.Count, BufferUsages.VERTEX | BufferUsages.COPY_DST);
                chunkBuffers[chunk] = new ChunkBufferInfo() { buffer = linePointsBuffer, drawC = (points.Count) };
            }

            unsafe
            {
                LinePoint* writemap = stackalloc LinePoint[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 point = points[i];
                    writemap[i] = new LinePoint(color, new Vector3(point.X, point.Y, 0f));
                }

                wgil.WriteBuffer(linePointsBuffer, writemap);
            }
        }

        public override void Render(RenderPass pass)
        {
            if (!render || !TilemapDrawer.renderGizmo)
                return;

            pipelineAsset.BindPipeline(pass);

            foreach (var chunkKV in chunkBuffers)
            {
                pass.SetVertexBuffer(0, chunkKV.Value.buffer);

                pass.Draw(chunkKV.Value.drawC - 1, 1);
            }
          
        }

        public void UpdatePipeline(PipelineAsset newPipeline)
        {
            this.pipelineAsset = newPipeline;
        }
    }
}

