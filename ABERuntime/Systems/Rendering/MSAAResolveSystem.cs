using System;
using Veldrid;

namespace ABEngine.ABERuntime
{
	public class MSAAResolveSystem : RenderSystem
	{
        Texture resolvedColor;
        Texture resolvedDepth;

        Texture msRenderTexture;
        Texture msDepthTexture;

        public override void SetupResources(params Texture[] sampledTextures)
        {
            msRenderTexture = sampledTextures[0];
            msDepthTexture = sampledTextures[1];

            Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;

            if (GraphicsManager.msaaSampleCount != TextureSampleCount.Count1)
            {
                resolvedColor = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                   mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                    mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled));

                resolvedDepth = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                            mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                            PixelFormat.R16_UNorm, TextureUsage.DepthStencil | TextureUsage.Sampled));
            }
            else
            {
                resolvedColor = msRenderTexture;
                resolvedDepth = msDepthTexture;
            }
        }

        public override void Render(int renderLayer)
        {
            if (GraphicsManager.msaaSampleCount != TextureSampleCount.Count1)
            {
                cl.ResolveTexture(msRenderTexture, resolvedColor);
                cl.ResolveTexture(msDepthTexture, resolvedDepth);
            }
        }

        public void ResolveDepth(int renderLayer)
        {
            if(GraphicsManager.msaaSampleCount != TextureSampleCount.Count1)
                cl.ResolveTexture(msDepthTexture, resolvedDepth);
        }

        internal override Texture GetMainColorAttachent()
        {
            return resolvedColor;
        }

        internal override Texture GetDepthAttachment()
        {
            return resolvedDepth;
        }
    }
}

