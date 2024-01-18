using System;
using WGIL;

namespace ABEngine.ABERuntime
{
	internal class ResourceContext
	{
        // Passes
        // Normal
        private Texture cameraNormalTexture;
        private Texture normalsDepthTexture;

        public TextureView cameraNormalView;
        public TextureView normalsDepthView;

        // Main
        private Texture mainRenderTexture;
        private Texture spriteNormalsTexture;
        private Texture mainDepthTexture;

        public TextureView mainRenderView;
        public TextureView spriteNormalsView;
        public TextureView mainDepthView;

        // Main PP
        private Texture mainPPTexture;
        public TextureView mainPPView;

        // Light
        private Texture lightRenderTexture;
        public TextureView lightRenderView;

        internal ResourceContext()
		{
		}

        internal void RecreateFrameResources(uint width, uint height)
		{
            DisposeFrameResources();

            var wgil = Game.wgil;

            TextureFormat surfaceFormat = wgil.GetSurfaceFormat();


            // Normal
            cameraNormalTexture = wgil.CreateTexture(width, height, TextureFormat.Rgba8Unorm,
                                                     TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING, true);

            normalsDepthTexture = wgil.CreateTexture(width, height, TextureFormat.Depth32Float,
                                                     TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING, true);

            // Main
            mainRenderTexture = wgil.CreateTexture(width, height, TextureFormat.Rgba16Float,
                                                   TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_SRC, true);

            spriteNormalsTexture = wgil.CreateTexture(width, height, TextureFormat.Rgba8Unorm,
                                                      TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING, true);

            mainDepthTexture = wgil.CreateTexture(width, height, TextureFormat.Depth32Float,
                                                  TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING, true);

            mainPPTexture = wgil.CreateTexture(width, height, TextureFormat.Rgba16Float,
                                                   TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_DST, true);

            // Light
            lightRenderTexture = wgil.CreateTexture(width, height, TextureFormat.Rgba16Float,
                                                    TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING, true);

            cameraNormalView = cameraNormalTexture.CreateView(true);
            normalsDepthView = normalsDepthTexture.CreateView(true);
            mainRenderView = mainRenderTexture.CreateView(true);
            spriteNormalsView = spriteNormalsTexture.CreateView(true);
            mainDepthView = mainDepthTexture.CreateView(true);
            mainPPView = mainPPTexture.CreateView(true);
            lightRenderView = lightRenderTexture.CreateView(true);
        }

        internal void DisposeFrameResources()
        {
            cameraNormalTexture?.Dispose();
            normalsDepthTexture?.Dispose();

            cameraNormalView?.Dispose();
            normalsDepthView?.Dispose();

            mainRenderTexture?.Dispose();
            spriteNormalsTexture?.Dispose();
            mainDepthTexture?.Dispose();
            mainPPTexture?.Dispose();

            mainRenderView?.Dispose();
            spriteNormalsView?.Dispose();
            mainDepthView?.Dispose();
            mainPPView?.Dispose();

            lightRenderTexture?.Dispose();
            lightRenderView?.Dispose();
        }

        public void CopyScreenTexture()
        {
            Game.wgil.CopyTexture(mainRenderTexture, mainPPTexture);
        }
    }
}

