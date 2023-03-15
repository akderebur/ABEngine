using System;
using Microsoft.DotNet.PlatformAbstractions;
using System.Numerics;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABERuntime
{
    public static class EntityManager
    {

        public static Entity Instantiate(in Entity entity)
        {
            Entity copy = Game.GameWorld.CreateEntity();

            var comps = entity.GetAllComponents();
            var types = entity.GetAllComponentTypes();

            Sprite newSprite = null;
            Rigidbody newRb = null;

            for (int i = 0; i < comps.Length; i++)
            {
                var comp = comps[i];
                var type = types[i];

                if (typeof(JSerializable).IsAssignableFrom(type))
                {
                    var newComp = ((JSerializable)comp).GetCopy(ref copy);
                    copy.Set(type, newComp);

                    if (type == typeof(Sprite))
                        newSprite = (Sprite)newComp;
                    else if (type == typeof(Rigidbody))
                        newRb = (Rigidbody)newComp;

                }
                else if (type.IsSubclassOf(typeof(AutoSerializable)))
                {
                    var serialized = AutoSerializable.Serialize((AutoSerializable)comps[i]);
                    var newComp = AutoSerializable.Deserialize(serialized.Serialize(), type);
                    AutoSerializable.SetReferences(((AutoSerializable)newComp));

                    copy.Set(type, newComp);
                }
                else if(type == typeof(Guid))
                {
                    copy.Set(type, Guid.NewGuid());
                }
                else if(type.IsValueType || type == typeof(string))
                {
                    copy.Set(type, comp);
                }
            }


            if (newRb != null)
                Game.b2dInit.AddRBRuntime(ref copy);

            if (entity.enabled)
            {
                if (newSprite != null)
                    Game.spriteBatcher.UpdateSpriteBatch(newSprite, newSprite.renderLayerIndex, newSprite.texture, newSprite.sharedMaterial.instanceID);

            }

            CheckSubscribers(in copy, true);

            return copy;
        }

        private static void CheckSubscribers(in Entity ent, bool create)
        {
            TypeSignature typeSig = ent.archetype.GetTypeSignature();
            foreach (var notifyKP in Game.notifySystems)
            {
                if (typeSig.HasAll(notifyKP.Key))
                {
                    foreach (var system in notifyKP.Value)
                    {
                        if (create)
                            system.OnEntityCreated(in ent);
                        else
                            system.OnEntityDestroyed(in ent);
                    }
                }
            }

            foreach (var notifyKP in Game.notifyAnySystems)
            {
                if (typeSig.HasAny(notifyKP.Key))
                {
                    foreach (var system in notifyKP.Value)
                    {
                        if (create)
                            system.OnEntityCreated(in ent);
                        else
                            system.OnEntityDestroyed(in ent);
                    }
                }
            }
        }

        public static Entity CreateEntity()
        {
            var ent = Game.GameWorld.CreateEntity("New Entity", true, Guid.NewGuid(), new Transform());
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity(string entName)
        {
            var ent = Game.GameWorld.CreateEntity(entName, true, Guid.NewGuid(), new Transform());
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity(string entName, string tag, bool isStatic = false)
        {
            var ent = Game.GameWorld.CreateEntity(entName, true, Guid.NewGuid(), new Transform(tag, isStatic));
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1>(string entName, string tag, C1 c1, bool isStatic = false)
        {
            var ent = Game.GameWorld.CreateEntity(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2>(string entName, string tag, C1 c1, C2 c2, bool isStatic = false)
        {
            var ent = Game.GameWorld.CreateEntity(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2, C3>(string entName, string tag, C1 c1, C2 c2, C3 c3, bool isStatic = false)
        {
            var ent = Game.GameWorld.CreateEntity(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2, c3);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2, C3, C4>(string entName, string tag, C1 c1, C2 c2, C3 c3, C4 c4, bool isStatic = false)
        {
            var ent = Game.GameWorld.CreateEntity(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2, c3, c4);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2, C3, C4, C5>(string entName, string tag, C1 c1, C2 c2, C3 c3, C4 c4, C5 c5, bool isStatic = false)
        {
            var ent = Game.GameWorld.CreateEntity(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2, c3, c4, c5);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static void DestroyEntity(this in Entity entity)
        {
            CheckSubscribers(in entity, false);
            entity.Destroy();
        }

    }
}
