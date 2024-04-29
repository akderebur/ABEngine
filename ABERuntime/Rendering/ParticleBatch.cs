using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Rendering
{
	public class ParticleBatch : RenderBatch
	{
        // GPU Resources
        public Buffer particleBuffer;
        public Buffer drawBuffer;
        public BindGroup texSet;

        ParticleDrawData drawData;

        ScriptableParticleModule pm;

        ParticleVertex[] vertices = null;
        
        public ParticleBatch(ScriptableParticleModule pm, int layer, float z)
			 : base(pm.particleTexture, pm.particleMaterial, layer, false, z)
		{
            this.pm = pm;

            vertices = new ParticleVertex[pm.maxParticles];
            particleBuffer = _wgil.CreateBuffer(pm.maxParticles * ParticleVertex.VertexSize, BufferUsages.VERTEX | BufferUsages.COPY_DST);
            drawBuffer = _wgil.CreateBuffer(4, BufferUsages.UNIFORM | BufferUsages.COPY_DST);

            var texSetDesc = new BindGroupDescriptor()
            {
                BindGroupLayout = Graphics.sharedParticleLayout,
                Entries = new BindResource[]
                       {
                            drawBuffer,
                            texture2d.GetView(),
                            texture2d.textureSampler,
                       }
            };

            texSet = _wgil.CreateBindGroup(ref texSetDesc);

            drawData.totalParticles = 1;
            _wgil.WriteBuffer(drawBuffer, drawData);
        }

        internal void SetParticleInstance(int instanceCount)
        {
            base.instanceCount = instanceCount;
            _wgil.WriteBuffer(particleBuffer, vertices, 0, instanceCount * ParticleVertex.VertexSize);
        }

        internal ParticleVertex[] GetParticleVertices()
        {
            return vertices;
        }

        public override void UpdateBatch()
        {
           
        }

        internal override void Render(RenderPass pass)
        {
            material.pipelineAsset.BindPipeline(pass);
            pass.SetBindGroup(1, texSet);

            pass.SetVertexBuffer(0, particleBuffer);

            pass.Draw(6, instanceCount);
        }

        internal override void DeleteBatch()
        {
            particleBuffer.Dispose();
            texSet.Dispose();
            base.DeleteBatch();
        }

        protected override void PipelineMaterial_onPipelineChanged(PipelineAsset pipeline)
        {
            throw new NotImplementedException();
        }
    }

    struct ParticleVertex
    {
        public const int VertexSize = 48;

        public Vector3 Position;
        public float Size;
        public Vector4 Tint;
        public Vector2 UvStart;
        public Vector2 UvScale;

        public ParticleVertex(Vector3 position, float size) : this(position, size, Vector4.One, Vector2.Zero, Vector2.One) { }
        public ParticleVertex(Vector3 position, float size, Vector4 tint, Vector2 uvStart, Vector2 uvScale)
        {
            Position = position;
            Size = size;
            Tint = tint;
            UvStart = uvStart;
            UvScale = uvScale;
        }
    }

    public struct ParticleDrawData
    {
        public float totalParticles;
    }
}

