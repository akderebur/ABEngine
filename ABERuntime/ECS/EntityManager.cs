using System;
using Microsoft.DotNet.PlatformAbstractions;
using System.Numerics;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Components;
using System.Linq;
using System.Collections.Generic;
using Halak;
using Box2D.NetStandard.Dynamics.Bodies;

namespace ABEngine.ABERuntime
{
    public static class EntityManager
    {
        private static List<EntityDestroyInfo> destroyList = new List<EntityDestroyInfo>();

        public static void CheckEntityChanges()
        {
            for (int i = 0; i < destroyList.Count; i++)
            {
                var destroyInfo = destroyList[i];
                bool canDestroy = destroyInfo.rb == null ? true : destroyInfo.rb.destroyed;

                if(canDestroy)
                {
                    var entity = destroyInfo.entity;
                    CheckSubscribers(in entity, false);
                    entity.Destroy();
                }
            }
        }

        // Instantiate scene objects
        public static Entity Instantiate(in Entity entity, Transform parent = null)
        {
            return InstantiateCore(entity, parent);
        }

        // Instantiate prefabs
        public static Entity Instantiate(string prefabName, Transform parent = null)
        {
            Transform prefabTrans = PrefabManager.GetPrefabTransform(prefabName);

            if(prefabTrans != null)
                return InstantiateCore(prefabTrans.entity, parent);

            return default(Entity);
        }

        internal static Entity InstantiateCore(in Entity entity, Transform parent)
        {
            Entity copy = Game.GameWorld.CreateEntity();

            var comps = entity.GetAllComponents();
            var types = entity.GetAllComponentTypes();

            int transformIndex = Array.IndexOf(types, typeof(Transform));
            if (transformIndex < 0)
                return default(Entity);

            // Set transform
            var transComp = ((JSerializable)comps[transformIndex]).GetCopy();
            copy.Set(types[transformIndex], transComp);

            Sprite newSprite = null;
            Rigidbody newRb = null;
            ParticleModule newPm = null;

            for (int i = 0; i < comps.Length; i++)
            {
                if (i == transformIndex)
                    continue;

                var comp = comps[i];
                var type = types[i];

                if (typeof(JSerializable).IsAssignableFrom(type))
                {
                    var newComp = ((JSerializable)comp).GetCopy();
                    copy.Set(type, newComp);

                    if (type == typeof(Sprite))
                        newSprite = (Sprite)newComp;
                    else if (type == typeof(Rigidbody))
                        newRb = (Rigidbody)newComp;
                    else if (type == typeof(ParticleModule))
                        newPm = (ParticleModule)newComp;

                }
                else if (type.IsSubclassOf(typeof(ABComponent)))
                {
                    var serialized = ABComponent.Serialize((ABComponent)comps[i]);
                    var newComp = ABComponent.Deserialize(serialized.Serialize(), type);
                    ABComponent.SetReferences(((ABComponent)newComp));

                    copy.Set(type, newComp);
                }
                else if (type == typeof(Guid))
                {
                    copy.Set(type, Guid.NewGuid());
                }
                else if (type.IsValueType || type == typeof(string))
                {
                    copy.Set(type, comp);
                }
            }

            copy.transform.SetParent(parent, false);


            if (newRb != null)
                Game.b2dInitSystem.AddRBRuntime(copy);

            if (entity.enabled)
            {
                if (newSprite != null)
                    Game.spriteBatchSystem.UpdateSpriteBatch(newSprite, newSprite.renderLayerIndex, newSprite.texture, newSprite.sharedMaterial.instanceID);
            }

            CheckSubscribers(in copy, true);


            foreach (var child in entity.transform.children.ToList())
            {
                InstantiateCore(child.entity, copy.transform);
            }

            return copy;
        }

        internal static Transform LoadSerializedPrefab(PrefabAsset prefabAsset)
        {
            JValue prefab = JValue.Parse(prefabAsset.serializedData);

            // Assets
            var jAssets = prefab["Assets"];
            AssetCache.ClearSerializeDependencies();
            AssetCache.DeserializeAssets(jAssets);

            List<Transform> newEntities = new List<Transform>();
            foreach (var entity in prefab["Entities"].Array())
            {
                string entName = entity["Name"];
                string guid = entity["GUID"];
                Entity newEnt = Game.PrefabWorld.CreateEntity(entName, Guid.Parse(guid));

                foreach (var component in entity["Components"].Array())
                {
                    Type type = Type.GetType(component["type"]);

                    if (type == null)
                        type = Game.UserTypes.FirstOrDefault(t => t.ToString().Equals(component["type"]));

                    if (type == null)
                        continue;

                    if (typeof(JSerializable).IsAssignableFrom(type))
                    {
                        var serializedComponent = (JSerializable)Activator.CreateInstance(type);
                        serializedComponent.Deserialize(component.ToString());
                        newEnt.Set(type, serializedComponent);
                    }
                    else if (type.IsSubclassOf(typeof(ABComponent)))
                    {
                        var comp = ABComponent.Deserialize(component.ToString(), type);
                        newEnt.Set(type, comp);
                    }
                }

                newEntities.Add(newEnt.transform);
            }

            var rootEnt = newEntities.First();

            // Parenting

            foreach (var entity in newEntities)
            {
                if (entity == rootEnt)
                    continue;

                if (!string.IsNullOrEmpty(entity.parentGuidStr))
                {
                    Guid parGuid = Guid.Parse(entity.parentGuidStr);
                    entity.SetParent(Game.PrefabWorld.GetEntities().FirstOrDefault(e => e.Get<Guid>().Equals(parGuid)).Get<Transform>(), false);
                }
            }

            rootEnt.entity.Set<Guid>(prefabAsset.prefabGuid);
            return rootEnt;
        }

        private static void CheckSubscribers(in Entity ent, bool create)
        {
            if (Game.notifySystems == null)
                return;

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
            var destroyInfo = new EntityDestroyInfo() { entity = entity };
            if (entity.Has<Rigidbody>())
            {
                destroyInfo.rb = entity.Get<Rigidbody>();
                destroyInfo.rb.Destroy();
            }
            destroyList.Add(destroyInfo);
        }

        public static Transform FindTransformByName(string name)
        {
            foreach (var ent in Game.GameWorld.GetEntities())
            {
                if (ent.transform.name.Equals(name))
                    return ent.transform;
            }

            return null;
        }
    }

    class EntityDestroyInfo
    {
        public Entity entity;
        public Rigidbody rb;
    }
}
