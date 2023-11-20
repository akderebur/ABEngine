using System;
using ABEngine.ABERuntime;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;
using WGIL;

namespace ABEngine.ABEditor
{
	public static class EditorAssetCache
	{
        private static Texture2D gridTexture = null;
        private static Texture2D gridLineTexture = null;

        internal static Texture2D GetGridTexture()
        {
            if (gridTexture != null)
                return gridTexture;

            // Load texture
            Texture tex = Game.wgil.CreateTexture(128, 128, TextureFormat.Rgba8UnormSrgb, TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_DST);
            var pixelData = new byte[128 * 128 * 4];
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                pixelData[i] = 127;
                pixelData[i + 1] = 127;
                pixelData[i + 2] = 127;
                pixelData[i + 3] = 255;
            }

            Game.wgil.WriteTexture(tex, pixelData.AsSpan(), pixelData.Length, 4);

            gridTexture = new Texture2D(1, tex, GraphicsManager.linearSampleClamp, Vector2.Zero); ;
            return gridTexture;
        }

        internal static Texture2D GetGridLineTexture()
        {
            if (gridLineTexture != null)
                return gridLineTexture;

            // Load texture
            Texture tex = Game.wgil.CreateTexture(3000, 1, TextureFormat.Rgba8UnormSrgb, TextureUsages.TEXTURE_BINDING | TextureUsages.COPY_DST);
            var pixelData = new byte[3000 * 1 * 4];
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                pixelData[i] = 255;
                pixelData[i + 1] = 255;
                pixelData[i + 2] = 255;
                pixelData[i + 3] = 255;
            }

            Game.wgil.WriteTexture(tex, pixelData.AsSpan(), pixelData.Length, 4);

            gridLineTexture = new Texture2D(2, tex, GraphicsManager.linearSampleClamp, Vector2.Zero); ;
            return gridLineTexture;
        }
    }
}

