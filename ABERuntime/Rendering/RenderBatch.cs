using System;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;
using Buffer = WGIL.Buffer;

namespace ABEngine.ABERuntime.Rendering
{
	public abstract class RenderBatch
	{
        protected WGILContext _wgil;

        internal bool isTransparent = false;
        internal bool isDynamicSort = false;

        public float maxZ;
        public int renderOrder = 0;
        public int renderLayerIndex = 0;
        public float zValue = 0;
        public int instanceCount;
        public bool isStatic { get; set; }

        internal event Action<RenderBatch> onDelete;
        internal bool active = false;

        protected Texture2D texture2d;
        public PipelineMaterial material;

        public string key;

        public RenderBatch(Texture2D texture, PipelineMaterial pipelineMaterial, int renderLayerIndex, bool isStatic, float zValue)
		{
            _wgil = Game.wgil;
            pipelineMaterial.OnPipelineChanged += PipelineMaterial_onPipelineChanged;

            this.texture2d = texture;
            this.material = pipelineMaterial;
            this.renderLayerIndex = renderLayerIndex;
            this.isStatic = isStatic;
            this.zValue = zValue;

            renderOrder = pipelineMaterial.renderOrder;
            if (pipelineMaterial.pipelineAsset.renderType == RenderType.Transparent)
                isTransparent = true;
        }

        public abstract void UpdateBatch();
        protected abstract void PipelineMaterial_onPipelineChanged(PipelineAsset pipeline);

        protected virtual void TriggerDelete()
        {
            onDelete?.Invoke(this);
        }

        internal virtual void DeleteBatch()
        {
            material.OnPipelineChanged -= PipelineMaterial_onPipelineChanged;
            TriggerDelete();
        }

        internal virtual void Render(RenderPass pass)
        {

        }
    }
}

