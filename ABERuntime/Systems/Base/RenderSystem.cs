using System;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABERuntime
{
    public class RenderSystem : BaseSystem
    {
        protected WGILContext wgil;

        protected PipelineAsset pipelineAsset;

        public RenderSystem()
        {
            this.pipelineAsset = null;
            SetGraphics();
        }

        public RenderSystem(bool dontDestroyOnLoad) : base(dontDestroyOnLoad)
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
            this.wgil = Game.wgil;
        }

        public virtual void SetupResources(params TextureView[] sampledTextures)
        {

        }

        public virtual void SceneSetup()
        {

        }

        public virtual void RenderUpdate()
        {

        }

        public virtual void Render(RenderPass pass)
        {

        }

        public virtual void Render(RenderPass pass, int renderLayer)
        {

        }

        public virtual void Render(float interpolation)
        {

        }


        public virtual void UIRender()
        {

        }

        internal virtual TextureView GetMainColorAttachent()
        {
            return null;
        }

        internal virtual TextureView GetSecondaryColorAttachment()
        {
            return null;
        }

        internal virtual TextureView GetDepthAttachment()
        {
            return null;
        }

        public virtual TextureView GetMainView()
        {
            return Game.resourceContext.mainRenderView;
        }

    }
}
