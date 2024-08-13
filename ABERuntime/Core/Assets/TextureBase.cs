using System.Numerics;
using WGIL;

namespace ABEngine.ABERuntime.Core.Assets
{
    public abstract class TextureBase : Asset
    {
        public Texture texture { get; set; }
        public Sampler textureSampler { get; set; }
        public Vector2 imageSize { get; set; }
        public bool isLinear { get; set; }
        
        public virtual TextureView GetView()
        {
            return Assets.GetOrCreateTextureView(texture);
        }
    }
}