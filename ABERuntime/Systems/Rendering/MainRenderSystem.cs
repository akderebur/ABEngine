using System;
using ABEngine.ABERuntime.Pipelines;
using Veldrid;
using System.Collections.Generic;

namespace ABEngine.ABERuntime
{
	public class MainRenderSystem : RenderSystem
	{
        public Texture mainRenderTexture;
        public Texture spriteNormalsTexture;

        public Texture mainDepthTexture;
        Framebuffer mainRenderFB;

        List<PipelineAsset> pipelines = new List<PipelineAsset>();

        public override void SetupResources(params Texture[] sampledTextures)
        {
            Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;
            TextureSampleCount sampleCount = GraphicsManager.msaaSampleCount;

            mainRenderTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
               mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled, sampleCount));

            spriteNormalsTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
               mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled, sampleCount));

            mainDepthTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                        mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                        PixelFormat.R16_UNorm, TextureUsage.DepthStencil | TextureUsage.Sampled, sampleCount));

            mainRenderFB = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(mainDepthTexture, mainRenderTexture, spriteNormalsTexture));

        }

        public override void SceneSetup()
        {
          
        }

        public override void Start()
        {
           
        }

        public override void Render(int renderLayer)
        {
            Render();
        }

        public override void Render()
        {
            cl.SetFramebuffer(mainRenderFB);
            cl.SetFullViewports();
            cl.ClearColorTarget(0, new RgbaFloat(0f, 0f, 0f, 0f));
            cl.ClearColorTarget(1, new RgbaFloat(0f, 0f, 0f, 0f));
            cl.ClearDepthStencil(1f);
        }

        public void LateRender(int layer)
        {
            cl.End();
            gd.SubmitCommands(cl);
            gd.WaitForIdle();
            cl.Begin();

            cl.SetFramebuffer(mainRenderFB);
            cl.SetFullViewports();
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            if(resize)
            {
                mainRenderTexture.Dispose();
                spriteNormalsTexture.Dispose();
                mainDepthTexture.Dispose();
                mainRenderFB.Dispose();
            }
        }

        public OutputDescription GetFBOutputDesc()
        {
            return mainRenderFB.OutputDescription;
        }

        internal override Texture GetMainColorAttachent()
        {
            return mainRenderTexture;
        }

        internal override Texture GetSecondaryColorAttachment()
        {
            return spriteNormalsTexture;
        }

        internal override Texture GetDepthAttachment()
        {
            return mainDepthTexture;
        }

        public override Framebuffer GetMainFramebuffer()
        {
            return mainRenderFB;
        }
    }
}

