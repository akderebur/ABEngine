using System;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Animation;
using System.Security.Principal;

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
            Sprite sprite = new Sprite(squareTex);
            entity.Set(sprite);
            //sprite.SetDrawable();
            //entity.Set(sprite);
        }

        public static void AddAABB(in Entity entity)
        {
            AABB aabb = new AABB();
            if (entity.Has<Sprite>())
            {
                Sprite sprite = entity.Get<Sprite>();
                Vector2 spriteSize = sprite.GetSize().ToVector2();
                aabb.size = spriteSize;
            }
            entity.Set(aabb);
        }

        public static void AddRigidbody(in Entity entity)
        {
            entity.Set(new Rigidbody());
        }

        public static void AddAnimator(in Entity entity)
        {
            entity.Set(new Animator());
        }

        public static void AddParticleModule(in Entity entity)
        {
            entity.transform.tag = "NoChild";
            entity.Set(new ParticleModule());
        }

        public static void AddSpriteAnimation(in Entity entity)
        {
            if (entity.Has<Sprite>())
                entity.Set(new SpriteAnimation(entity.Get<Sprite>()));
        }
    }
}
