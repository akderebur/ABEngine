using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Components;

public class SpriteLight
{
    internal Texture2D texture;
    internal AnimationState state;

    public bool hasAnimation { get; set; }
    public bool isAnimPlaying { get; set; }
    
    
}