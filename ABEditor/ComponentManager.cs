using System;
using ABEngine.ABERuntime;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Animation;
using System.Security.Principal;
using Arch.Core;
using Arch.Core.Extensions;

namespace ABEngine.ABEditor
{
    public static class ComponentManager
    {
        static Texture2D squareTex;

        static ComponentManager()
        {
            squareTex = AssetCache.GetDefaultTexture();
            //squareTex = AssetCache.CreateTexture2D()
            //squareTex = new Texture2D(Editor.EditorAssetPath, "Sprites/square-128.png", GraphicsManager.linearSampleClamp);
        }

        public static void AddSprite(in Entity entity)
        {
            Sprite sprite = new Sprite();
            entity.Add<Sprite>(sprite);
            //sprite.SetDrawable();
            //entity.Set(sprite);
        }

        public static void AddAABB(in Entity entity)
        {
            AABB aabb = new AABB();
            if (entity.Has<Sprite>())
            {
                Sprite sprite = entity.Get<Sprite>();
                Vector2 spriteSize = sprite.GetSize();
                aabb.size = spriteSize;
            }
            entity.Add(aabb);
        }

        public static void AddRigidbody(in Entity entity)
        {
            entity.Add(new Rigidbody());
        }

        public static void AddAnimator(in Entity entity)
        {
            entity.Add(new Animator());
        }

        public static void AddParticleModule(in Entity entity)
        {
            entity.Get<Transform>().tag = "NoChild";
            entity.Add(new ParticleModule());
        }

        public static void AddSpriteAnimation(in Entity entity)
        {
            if (entity.Has<Sprite>())
                entity.Add(new SpriteAnimation(entity.Get<Sprite>()));
        }
    }
}
