using System;
using ABEngine.ABERuntime.Pipelines;
using Veldrid;
using System.Collections.Generic;

namespace ABEngine.ABERuntime
{
	public class MainRenderSystem : RenderSystem
	{
        public Texture mainRenderTexture;
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

            mainDepthTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                        mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                        PixelFormat.R16_UNorm, TextureUsage.DepthStencil | TextureUsage.Sampled, sampleCount));

            mainRenderFB = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(mainDepthTexture, mainRenderTexture));


            foreach (var pipeline in pipelines)
                pipeline.UpdateFramebuffer(mainRenderFB);

        }

        public override void SceneSetup()
        {
            // Related default pipelines
            pipelines = new List<PipelineAsset>()
            {
                new UberPipelineAsset(mainRenderFB),
                new UberPipelineAdditive(mainRenderFB),
            };

            if(!GraphicsManager.render2DOnly)
            {
                pipelines.AddRange(new List<PipelineAsset>
                {
                    new UberPipeline3D(mainRenderFB),
                    new ToonWaterPipeline(mainRenderFB),
                    new ToonLitPipeline()
                });
            }
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
            cl.ClearDepthStencil(1f);
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            if(resize)
            {
                mainRenderTexture.Dispose();
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

