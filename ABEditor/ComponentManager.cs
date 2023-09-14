using System;
using ABEngine.ABERuntime;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Animation;
using System.Security.Principal;
using Arch.Core;
using Arch.Core.Extensions;
using ABEngine.ABERuntime.Rendering;
using ABEngine.ABERuntime.Core.Components;
using System.Collections.Generic;
using ABEngine.ABERuntime.ECS;

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


        public static void AddTilemap(in Entity entity)
        {
            if (!entity.Has<Tilemap>())
            {
                entity.Get<Transform>().tag = "NoChild";
                entity.Add(new Tilemap());
            }
        }

        public static void AddMeshRenderer(in Entity entity)
        {
            if (!entity.Has<MeshRenderer>())
            {
                MeshRenderer mr = new MeshRenderer(CubeModel.GetCubeMesh());
                entity.Add(mr);

                var query = new QueryDescription().WithAll<Transform, DirectionalLight>();
                int dirLightCount = Game.GameWorld.CountEntities(query);

                if (dirLightCount == 0)
                {
                    var sunLight = EntityManager.CreateEntity("DirLight", "", new DirectionalLight()
                    { color = Color.White.ToVector4(), direction = Vector3.Normalize(-Vector3.UnitZ), Intensity = 1f });
                    Editor.AddToHierList(sunLight.Get<Transform>());
                }
            }
        }
    }
}
