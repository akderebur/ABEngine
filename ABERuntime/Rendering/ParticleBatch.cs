using System;
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
        public BindGroup texSet;

        int maxParticleCount;
		int particleCount;

		public ParticleBatch(Texture2D tex2d, PipelineMaterial mat, int layer, float z, int maxPartCount)
			 : base(tex2d, mat, layer, false, z)
		{
			maxParticleCount = maxPartCount;
			particleCount = 100;
		}

        public override void UpdateBatch()
        {
            throw new NotImplementedException();
        }

        internal override void DeleteBatch()
        {
            base.DeleteBatch();
        }

        protected override void PipelineMaterial_onPipelineChanged(PipelineAsset pipeline)
        {
            throw new NotImplementedException();
        }
    }
}

