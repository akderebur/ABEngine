using System;
using Veldrid;

namespace ABEngine.ABERuntime.Systems.Rendering
{
	public class MSAAResolveSystem : RenderSystem
	{
        Texture resolvedColor;
        Texture resolvedDepth;

        public override void SetupResources(params Texture[] sampledTextures)
        {
            Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;
            resolvedColor = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
               mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled, TextureSampleCount.Count1));

            resolvedDepth = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                        mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                        PixelFormat.R16_UNorm, TextureUsage.DepthStencil | TextureUsage.Sampled, TextureSampleCount.Count1));

        }

        public override void Render(int renderLayer)
        {
            //resolvedColor
        }
    }
}

