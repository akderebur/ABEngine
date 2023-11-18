﻿using System;
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
                                                     TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING).SetManualDispose(true);

            normalsDepthTexture = wgil.CreateTexture(width, height, TextureFormat.Depth32Float,
                                                     TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING).SetManualDispose(true);

            // Main
            mainRenderTexture = wgil.CreateTexture(width, height, surfaceFormat,
                                                   TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING).SetManualDispose(true);

            spriteNormalsTexture = wgil.CreateTexture(width, height, TextureFormat.Rgba8Unorm,
                                                      TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING).SetManualDispose(true);

            mainDepthTexture = wgil.CreateTexture(width, height, TextureFormat.Depth32Float,
                                                  TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING).SetManualDispose(true);

            // Light
            lightRenderTexture = wgil.CreateTexture(width, height, surfaceFormat,
                                                    TextureUsages.RENDER_ATTACHMENT | TextureUsages.TEXTURE_BINDING).SetManualDispose(true);

            cameraNormalView = cameraNormalTexture.CreateView().SetManualDispose(true);
            normalsDepthView = normalsDepthTexture.CreateView().SetManualDispose(true);
            mainRenderView = mainRenderTexture.CreateView().SetManualDispose(true);
            spriteNormalsView = spriteNormalsTexture.CreateView().SetManualDispose(true);
            mainDepthView = mainDepthTexture.CreateView().SetManualDispose(true);
            lightRenderView = lightRenderTexture.CreateView().SetManualDispose(true);
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

            mainRenderView?.Dispose();
            spriteNormalsView?.Dispose();
            mainDepthView?.Dispose();

            lightRenderTexture?.Dispose();
            lightRenderView?.Dispose();
        }


    }
}

