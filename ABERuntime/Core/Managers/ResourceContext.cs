using System;
using Veldrid;

namespace ABEngine.ABERuntime
{
	internal class ResourceContext
	{
        // Passes
        // Normal
        public Texture cameraNormalTexture;
        public Texture normalsDepthTexture;
        public Framebuffer normalsRenderFB;

        // Main
        public Texture mainRenderTexture;
        public Texture spriteNormalsTexture;
        public Texture mainDepthTexture;
        public Framebuffer mainRenderFB;

        // Light
        public Texture lightRenderTexture;
        public Framebuffer lightRenderFB;

        internal ResourceContext()
		{
		}

        internal void RecreateFrameResources()
		{
            DisposeFrameResources();

            GraphicsDevice gd = GraphicsManager.gd;
            Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;

            // Normal
            cameraNormalTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
               mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
               PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget | TextureUsage.Sampled));


            normalsDepthTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                        mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                        PixelFormat.R16_UNorm, TextureUsage.DepthStencil | TextureUsage.Sampled, TextureSampleCount.Count1));

            normalsRenderFB = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(normalsDepthTexture, cameraNormalTexture));

            // Main
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

            // Light
            lightRenderTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
            mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled));

            lightRenderFB = gd.ResourceFactory.CreateFramebuffer(new FramebufferDescription(null, lightRenderTexture));
        }

        internal void DisposeFrameResources()
        {
            cameraNormalTexture?.Dispose();
            normalsDepthTexture?.Dispose();
            normalsRenderFB?.Dispose();

            mainRenderTexture?.Dispose();
            spriteNormalsTexture?.Dispose();
            mainDepthTexture?.Dispose();
            mainRenderFB?.Dispose();

            lightRenderTexture?.Dispose();
            lightRenderFB?.Dispose();
        }

        public Framebuffer GetMainFramebuffer()
        {
            return mainRenderFB;
        }


        public Framebuffer GetNormalFramebuffer()
        {
            return normalsRenderFB;
        }


        public Framebuffer GetLightFramebuffer()
        {
            return lightRenderFB;
        }
    }
}

