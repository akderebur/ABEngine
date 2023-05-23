using System;
using ABEngine.ABERuntime;
using System.Numerics;
using Veldrid;

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
            var texDesc = TextureDescription.Texture2D(
            128, 128, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            var tex = GraphicsManager.rf.CreateTexture(texDesc);
            var pixelData = new byte[128 * 128 * 4];
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                pixelData[i] = 127;
                pixelData[i + 1] = 127;
                pixelData[i + 2] = 127;
                pixelData[i + 3] = 255;
            }

            unsafe
            {
                fixed (byte* pin = pixelData)
                {
                    GraphicsManager.gd.UpdateTexture(
                    tex,
                    (IntPtr)pin,
                    (uint)pixelData.Length,
                    0,
                    0,
                    0,
                    128,
                    128,
                    1,
                    0,
                    0);
                }
            }

            gridTexture = new Texture2D(1, tex, GraphicsManager.linearSampleClamp, Vector2.Zero); ;
            return gridTexture;
        }

        internal static Texture2D GetGridLineTexture()
        {
            if (gridLineTexture != null)
                return gridLineTexture;

            // Load texture
            var texDesc = TextureDescription.Texture2D(
            3000, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            var tex = GraphicsManager.rf.CreateTexture(texDesc);
            var pixelData = new byte[3000 * 1 * 4];
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                pixelData[i] = 255;
                pixelData[i + 1] = 255;
                pixelData[i + 2] = 255;
                pixelData[i + 3] = 255;
            }

            unsafe
            {
                fixed (byte* pin = pixelData)
                {
                    GraphicsManager.gd.UpdateTexture(
                    tex,
                    (IntPtr)pin,
                    (uint)pixelData.Length,
                    0,
                    0,
                    0,
                    3000,
                    1,
                    1,
                    0,
                    0);
                }
            }

            gridLineTexture = new Texture2D(2, tex, GraphicsManager.linearSampleClamp, Vector2.Zero); ;
            return gridLineTexture;
        }
    }
}

