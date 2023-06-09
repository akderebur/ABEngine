using System;
using ABEngine.ABERuntime.Debug;
using System.Numerics;
using Veldrid;
using System.Collections.Generic;
using Vortice.Mathematics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABEditor.ComponentDrawers;
using System.Linq;

namespace ABEditor.Debug
{
    class ChunkBufferInfo
    {
        public DeviceBuffer buffer { get; set; }
        public uint drawC { get; set; }
    }

    internal class TilemapColliderGizmo : RenderSystem
    {
        Dictionary<CollisionChunk, ChunkBufferInfo> chunkBuffers;

        public TilemapColliderGizmo(PipelineAsset asset) : base(asset) { }

        Vector4 normalColor = RgbaFloat.Green.ToVector4();
        Vector4 selColor = RgbaFloat.Red.ToVector4();
        Vector4 noCollisionColor = RgbaFloat.Blue.ToVector4();


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

            DeviceBuffer linePointsBuffer = null;
            if (chunkBuffers.ContainsKey(chunk))
            {
                linePointsBuffer = chunkBuffers[chunk].buffer;
                linePointsBuffer.Dispose();

                linePointsBuffer = rf.CreateBuffer(new BufferDescription((uint)((points.Count) * LinePoint.VertexSize), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
                chunkBuffers[chunk] = new ChunkBufferInfo() { buffer = linePointsBuffer, drawC = (uint)(points.Count)};
            }
            else
            {
                linePointsBuffer = rf.CreateBuffer(new BufferDescription((uint)((points.Count) * LinePoint.VertexSize), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
                chunkBuffers.Add(chunk, new ChunkBufferInfo() { buffer = linePointsBuffer, drawC = (uint)(points.Count) });
            }

            MappedResourceView<LinePoint> writemap = gd.Map<LinePoint>(linePointsBuffer, MapMode.Write);

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];
                writemap[i] = new LinePoint(color, new Vector3(point.X, point.Y, 0f));
            }

            gd.Unmap(linePointsBuffer);
        }

        public override void Render()
        {
            if (!render || !TilemapDrawer.renderGizmo)
                return;

            pipelineAsset.BindPipeline();

            foreach (var chunkKV in chunkBuffers)
            {
                cl.SetVertexBuffer(0, chunkKV.Value.buffer);

                cl.Draw(chunkKV.Value.drawC - 1, 1, 0, 0);
            }
          
        }

        public void UpdatePipeline(PipelineAsset newPipeline)
        {
            this.pipelineAsset = newPipeline;
        }
    }
}

