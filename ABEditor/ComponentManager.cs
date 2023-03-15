using System;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Animation;

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

        public static void AddSprite(Entity entity)
        {
            Sprite sprite = new Sprite(squareTex);
            entity.Set(sprite);
            //sprite.SetDrawable();
            //entity.Set(sprite);
        }

        public static void AddAABB(Entity entity)
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

        public static void AddRigidbody(Entity entity)
        {
            entity.Set(new Rigidbody());
        }

        public static void AddAnimator(Entity entity)
        {
            entity.Set(new Animator());
        }

    }
}
