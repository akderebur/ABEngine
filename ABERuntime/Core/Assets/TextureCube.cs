using System.Collections.Generic;
using System.Numerics;
using Halak;
using WGIL;

namespace ABEngine.ABERuntime.Core.Assets
{
    public class TextureCube : TextureBase
    {
        internal TextureCube(uint hash, Texture texture, Sampler sampler, bool isLinear)
        {
            fPathHash = hash;
            this.textureSampler = sampler;
            this.texture = texture;
            this.isLinear = isLinear;
            imageSize = new Vector2(texture.Width, texture.Height);
        }

        internal void UpdateFaceTexture(ImageSharpTexture faceTex, int depth)
        {
            if(texture == null)
                return;
            
            faceTex.WriteToCubemap(texture, (uint)depth);
        }

        internal override JValue SerializeAsset()
        {
            throw new System.NotImplementedException();
        }

        public override TextureView GetView()
        {
            return Assets.GetOrCreateTextureView(texture, true);
        }
    }
}