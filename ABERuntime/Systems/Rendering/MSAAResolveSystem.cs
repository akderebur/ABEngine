using System;
using Veldrid;

namespace ABEngine.ABERuntime
{
	public class MSAAResolveSystem : RenderSystem
	{
        Texture resolvedColor;
        Texture resolvedSpriteNormal;
        Texture resolvedDepth;

        Texture msRenderTexture;
        Texture msSpriteNormalTexture;
        Texture msDepthTexture;

        public override void SetupResources(params Texture[] sampledTextures)
        {
            msRenderTexture = sampledTextures[0];
            msSpriteNormalTexture = sampledTextures[1];
            msDepthTexture = sampledTextures[2];

            Texture mainFBTexture = gd.MainSwapchain.Framebuffer.ColorTargets[0].Target;

            if (GraphicsManager.msaaSampleCount != TextureSampleCount.Count1)
            {
                resolvedColor = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                   mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                    mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled));


                resolvedSpriteNormal = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                   mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                    mainFBTexture.Format, TextureUsage.RenderTarget | TextureUsage.Sampled));


                resolvedDepth = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                            mainFBTexture.Width, mainFBTexture.Height, mainFBTexture.MipLevels, mainFBTexture.ArrayLayers,
                            PixelFormat.R16_UNorm, TextureUsage.DepthStencil | TextureUsage.Sampled));
            }
            else
            {
                resolvedColor = msRenderTexture;
                resolvedSpriteNormal = msSpriteNormalTexture;
                resolvedDepth = msDepthTexture;
            }
        }

        public override void Render(int renderLayer)
        {
            if (GraphicsManager.msaaSampleCount != TextureSampleCount.Count1)
            {
                cl.ResolveTexture(msRenderTexture, resolvedColor);
                cl.ResolveTexture(msSpriteNormalTexture, resolvedSpriteNormal);
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

        internal override Texture GetSecondaryColorAttachment()
        {
            return resolvedSpriteNormal;
        }

        internal override Texture GetDepthAttachment()
        {
            return resolvedDepth;
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            if(resize)
            {
                resolvedColor.Dispose();
                resolvedDepth.Dispose();
                resolvedSpriteNormal.Dispose();
            }
        }
    }
}

