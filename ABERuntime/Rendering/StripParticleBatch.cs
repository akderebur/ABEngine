using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Rendering
{
    public class StripParticleBatch : RenderBatch
    {
        // GPU Resources
        public Buffer particleBuffer;
        public Buffer drawBuffer;
        public BindGroup texSet;

        ParticleDrawData drawData;

        ScriptableParticleModule pm;

        StripVertex[] vertices = null;

        public StripParticleBatch(ScriptableParticleModule pm, int layer, float z)
             : base(pm.particleTexture, pm.particleMaterial, layer, false, z)
        {
            this.pm = pm;

            vertices = new StripVertex[pm.maxParticles * 2];
            particleBuffer = _wgil.CreateBuffer(pm.maxParticles * 2 * StripVertex.VertexSize, BufferUsages.VERTEX | BufferUsages.COPY_DST);
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

        internal void SetParticleInstance(int instanceCount, float totalDistance = 1f)
        {
            base.instanceCount = instanceCount;
            _wgil.WriteBuffer(particleBuffer, vertices, 0, instanceCount * 2 * StripVertex.VertexSize);


            drawData.totalParticles = totalDistance;
            _wgil.WriteBuffer(drawBuffer, drawData);

        }

        internal StripVertex[] GetParticleVertices()
        {
            return vertices;
        }

        public override void UpdateBatch()
        {

        }

        internal override void Render(RenderPass pass)
        {
            if(instanceCount > 1)
            {
                material.pipelineAsset.BindPipeline(pass);
                pass.SetBindGroup(1, texSet);

                pass.SetVertexBuffer(0, particleBuffer);

                // Material Resource Sets
                foreach (var setKV in material.bindableSets)
                {
                    pass.SetBindGroup(setKV.Key, setKV.Value);
                }

                pass.Draw(instanceCount * 2);
            }
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

    struct StripVertex
    {
        public const int VertexSize = 36;

        public Vector3 Position;
        public Vector2 UV;
        public Vector4 Tint;

        public StripVertex(Vector3 position, Vector2 uv, Vector4 tint)
        {
            Position = position;
            UV = uv;
            Tint = tint;
        }
    }
}

