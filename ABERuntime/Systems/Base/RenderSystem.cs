using System;
using Veldrid;
using Veldrid.Utilities;

namespace ABEngine.ABERuntime
{
    public class RenderSystem : BaseSystem
    {
        protected GraphicsDevice gd;
        protected DisposeCollectorResourceFactory rf;
        protected CommandList cl;

        protected PipelineAsset pipelineAsset;

        public RenderSystem()
        {
            this.pipelineAsset = null;
            SetGraphics();
        }

        public RenderSystem(PipelineAsset pipelineAsset)
        {
            this.pipelineAsset = pipelineAsset;
            SetGraphics();
        }

        void SetGraphics()
        {
            this.gd = GraphicsManager.gd;
            this.rf = GraphicsManager.rf;
            this.cl = GraphicsManager.cl;
        }

        public virtual void Render()
        {

        }

        public virtual void Render(int renderLayer)
        {

        }

        public virtual void Render(float interpolation)
        {

        }


        public virtual void UIRender()
        {

        }

       
    }
}
