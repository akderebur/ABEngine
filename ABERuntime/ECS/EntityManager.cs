using System;
using Microsoft.DotNet.PlatformAbstractions;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using System.Linq;
using System.Collections.Generic;
using Halak;
using Box2D.NetStandard.Dynamics.Bodies;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using System.Collections;
using Box2D.NetStandard.Common;
using Arch.CommandBuffer;
using Arch.Core.Utils;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
    public static class EntityManager
    {
        public static SemaphoreSlim creationSemaphore = new SemaphoreSlim(1);
        public static SemaphoreSlim frameSemaphore = new SemaphoreSlim(1);

        private static bool immediateDestroy;
        //private static List<EntityDestroyInfo> destroyList = new List<EntityDestroyInfo>();
        private static Dictionary<int, EntityDestroyInfo> destroyMap = new Dictionary<int, EntityDestroyInfo>();

        public static CommandBuffer cmdBuffer;

        static EntityManager()
        {
        }

        public static void Init()
        {
            cmdBuffer = new CommandBuffer(Game.GameWorld);

            frameSemaphore.Wait();
        }

        public static void CheckEntityChanges()
        {
            foreach (var entID in destroyMap.Keys)
            {
                var destroyInfo = destroyMap[entID];
                bool canDestroy = destroyInfo.rb == null ? true : destroyInfo.rb.destroyed;

                if (canDestroy)
                {
                    var entity = destroyInfo.entity;
                    CheckSubscribers(in entity, false);
                    Game.GameWorld.Destroy(entity);
                    destroyMap.Remove(entID);
                }
            }

            creationSemaphore.Wait();
            if (cmdBuffer.Size > 0)
            {
                cmdBuffer.Playback();
            }
            creationSemaphore.Release();

            frameSemaphore.Release();
            frameSemaphore.Wait();
        }

        private static void TempToWorld(in Entity entity, Transform parent = null)
        {
            Entity copy = Game.GameWorld.Create();
            var comps = entity.GetAllComponents();
            var types = entity.GetComponentTypes();

            int transformIndex = Array.IndexOf(types, typeof(Transform));
            if (transformIndex < 0)
                return;

            // Set transform
            var transComp = comps[transformIndex];
            copy.Set(types[transformIndex], transComp);

            for (int i = 0; i < comps.Length; i++)
            {
                if (i == transformIndex)
                    continue;

                var comp = comps[i];
                var type = types[i];

                copy.Set(type, comp);
            }

            copy.Get<Transform>().SetParent(parent, false);

            CheckSubscribers(in copy, true);

            foreach (var child in entity.Get<Transform>().children.ToList())
            {
                TempToWorld(child.entity, copy.Get<Transform>());
            }
        }

        // Instantiate scene objects async
        public static async Task<AsyncEntity> InstantiateAsync(Entity entity, Transform parent = null)
        {
            bool locked = false;
            try
            {
                await creationSemaphore.WaitAsync();
                locked = true;
                return InstantiateBuffer(entity, parent);
                //return new AsyncEntity(newEnt);
            }
            finally
            {
                if(locked)
                    creationSemaphore.Release();
            }
        }


        // Instantiate scene objects
        public static Entity Instantiate(in Entity entity, Transform parent = null)
        {
            //creationSemaphore.Wait();
           
                Entity newEnt = InstantiateCore(entity, parent, Game.GameWorld);
                return newEnt;
            
            //finally
            //{
            //    creationSemaphore.Release();
            //}
        }

        // Instantiate prefabs
        //public static Entity Instantiate(string prefabName, Transform parent = null)
        //{
        //    Transform prefabTrans = PrefabManager.GetPrefabTransform(prefabName);

        //    if(prefabTrans != null)
        //        return InstantiateCore(prefabTrans.entity, parent);

        //    return default(Entity);
        //}

        internal static AsyncEntity InstantiateBuffer(in Entity entity, Transform parent)
        {

            var comps = entity.GetAllComponents();
            var types = entity.GetComponentTypes();

            Dictionary<Type, object> compCopyList = new Dictionary<Type, object>();

            int transformIndex = Array.IndexOf(types, typeof(Transform));
            if (transformIndex < 0)
                return new AsyncEntity(default(Entity), compCopyList);

            // Set transform
            Transform transComp = ((Transform)comps[transformIndex]).GetCopy() as Transform;
            Entity copy = Game.GameWorld.Create(transComp);
            compCopyList.Add(typeof(Transform), transComp);


            //AddComponentToBuffer(typeof(Transform), copy, transComp);
            //cmdBuffer.Add(in copy, transComp);

            Sprite newSprite = null;
            Rigidbody newRb = null;
            ParticleModule newPm = null;

            for (int i = 0; i < comps.Length; i++)
            {
                if (i == transformIndex)
                    continue;

                var comp = comps[i];
                var type = types[i].Type;

                if (typeof(JSerializable).IsAssignableFrom(type))
                {
                    var newComp = GetCopiedComponent(type, (JSerializable)comp);
                    AddComponentToBuffer(type, copy, newComp);
                    compCopyList.Add(type, newComp);

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

                    AddComponentToBuffer(type, copy, newComp);
                    compCopyList.Add(type, newComp);
                }
                else if (type == typeof(Guid))
                {
                    var guid = Guid.NewGuid();
                    cmdBuffer.Add<Guid>(in copy, guid);
                    compCopyList.Add(typeof(Guid), guid);
                }
                else if (type.IsValueType || type == typeof(string))
                {
                    AddComponentToBuffer(type, copy, comp);
                    compCopyList.Add(type, comp);
                }
            }

            transComp.SetParent(parent, false);

            foreach (var child in transComp.children.ToList())
            {
                InstantiateBuffer(child.entity, transComp);
            }

            return new AsyncEntity(copy, compCopyList);
        }

        internal static Entity InstantiateCore(in Entity entity, Transform parent, World world)
        {
            Entity copy = world.Create();

            var comps = entity.GetAllComponents();
            var types = entity.GetComponentTypes();

            int transformIndex = Array.IndexOf(types, typeof(Transform));
            if (transformIndex < 0)
                return default(Entity);

            // Set transform
            var transComp = GetCopiedComponent(typeof(Transform), (JSerializable)comps[transformIndex]);
            copy.Add(transComp);

            Sprite newSprite = null;
            Rigidbody newRb = null;
            ParticleModule newPm = null;

            for (int i = 0; i < comps.Length; i++)
            {
                if (i == transformIndex)
                    continue;

                var comp = comps[i];
                var type = types[i].Type;

                if (typeof(JSerializable).IsAssignableFrom(type))
                {
                    var newComp = GetCopiedComponent(type, (JSerializable)comp);
                    copy.Add(newComp);

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

                    copy.Add(newComp);
                }
                else if (type == typeof(Guid))
                {
                    copy.Add(Guid.NewGuid());
                }
                else if (type.IsValueType || type == typeof(string))
                {
                    copy.Add(comp);
                }
            }

            copy.Get<Transform>().SetParent(parent, false);


            //if (newRb != null)
            //    Game.b2dInitSystem.AddRBRuntime(copy);


            //if (newSprite != null)
            //    Game.spriteBatchSystem.UpdateSpriteBatch(newSprite, newSprite.renderLayerIndex, newSprite.texture, newSprite.sharedMaterial.instanceID);


            CheckSubscribers(in copy, true);


            foreach (var child in entity.Get<Transform>().children.ToList())
            {
                InstantiateCore(child.entity, copy.Get<Transform>(), world);
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
                Entity newEnt = PrefabManager.PrefabWorld.Create(entName, Guid.Parse(guid));

                foreach (var component in entity["Components"].Array())
                {
                    Type type = Type.GetType(component["type"]);

                    if (type == null)
                        type = Game.UserTypes.FirstOrDefault(t => t.ToString().Equals(component["type"]));

                    if (type == null)
                        continue;

                    if (typeof(JSerializable).IsAssignableFrom(type))
                    {
                        var comp = DeserializeComponent(type, component.ToString());
                        newEnt.Add(comp);
                    }
                    else if (type.IsSubclassOf(typeof(ABComponent)))
                    {
                        var comp = ABComponent.Deserialize(component.ToString(), type);
                        newEnt.Add(comp);
                    }
                }

                newEntities.Add(newEnt.Get<Transform>());
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

                    var query = new QueryDescription().WithAll<Transform>();
                    var entities = new List<Entity>();
                    PrefabManager.PrefabWorld.GetEntities(query, entities);


                    entity.SetParent(entities.FirstOrDefault(e => e.Get<Guid>().Equals(parGuid)).Get<Transform>(), false);
                }
            }

            rootEnt.entity.Set<Guid>(prefabAsset.prefabGuid);
            return rootEnt;
        }

        private static void CheckSubscribers(in Entity ent, bool create)
        {
            if (Game.notifySystems == null)
                return;

            var archBitSet = ent.GetArchetype().BitSet;
            foreach (var notifyKP in Game.notifySystems)
            {
                if (notifyKP.Key.All(archBitSet))
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
                if (notifyKP.Key.Any(archBitSet))
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
            var ent = Game.GameWorld.Create("New Entity", true, Guid.NewGuid(), new Transform());
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity(string entName)
        {
            var ent = Game.GameWorld.Create(entName, true, Guid.NewGuid(), new Transform());
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity(string entName, string tag, bool isStatic = false)
        {
            var ent = Game.GameWorld.Create(entName, true, Guid.NewGuid(), new Transform(tag, isStatic));
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1>(string entName, string tag, C1 c1, bool isStatic = false)
        {
            var ent = Game.GameWorld.Create(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2>(string entName, string tag, C1 c1, C2 c2, bool isStatic = false)
        {
            var ent = Game.GameWorld.Create(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2, C3>(string entName, string tag, C1 c1, C2 c2, C3 c3, bool isStatic = false)
        {
            var ent = Game.GameWorld.Create(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2, c3);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2, C3, C4>(string entName, string tag, C1 c1, C2 c2, C3 c3, C4 c4, bool isStatic = false)
        {
            var ent = Game.GameWorld.Create(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2, c3, c4);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2, C3, C4, C5>(string entName, string tag, C1 c1, C2 c2, C3 c3, C4 c4, C5 c5, bool isStatic = false)
        {
            var ent = Game.GameWorld.Create(entName, true, Guid.NewGuid(), new Transform(tag, isStatic), c1, c2, c3, c4, c5);
            CheckSubscribers(in ent, true);
            return ent;
        }


        public static void DestroyEntity(this in Entity entity)
        {
            if (EntityManager.immediateDestroy)
            {
                CheckSubscribers(in entity, false);
                Game.GameWorld.Destroy(entity);
            }
            else
            {
                if (destroyMap.ContainsKey(entity.Id))
                    return;

                var destroyInfo = new EntityDestroyInfo() { entity = entity };
                if (entity.Has<Rigidbody>())
                {
                    destroyInfo.rb = entity.Get<Rigidbody>();
                    destroyInfo.rb.Destroy();
                }
                destroyMap.Add(entity.Id, destroyInfo);
            }
        }


        internal static void SetImmediateDestroy(bool imDestroy)
        {
            EntityManager.immediateDestroy = imDestroy;
        }

        public static Transform FindTransformByName(string name)
        {
            Transform found = null;

            var query = new QueryDescription().WithAll<Transform>();
            var entities = new List<Entity>();
            Game.GameWorld.GetEntities(query, entities);

            foreach (var ent in entities)
            {
                if (ent.Get<Transform>().name.Equals(name))
                {
                    found = ent.Get<Transform>();
                    break;
                }
            }

            //var allQuery = new QueryDescription().WithAll<Transform>();
            //tmpWorld.Query(in allQuery, (ref Transform transform) =>
            //{
            //    if(transform.name.Equals(name))
            //    {
            //        found = transform;
            //        return;
            //    }
            //});

            return found;
        }

        public static object DeserializeComponent(Type type, string serializedComponent)
        {
            var method = typeof(EntityManager).GetMethod(nameof(DeserializeComponentGeneric)).MakeGenericMethod(type);
            return method.Invoke(null, new object[] { serializedComponent });
        }

        public static T DeserializeComponentGeneric<T>(string serializedComponent) where T : JSerializable, new()
        {
            T component = new T();
            component.Deserialize(serializedComponent);
            return component;
        }

        public static object GetCopiedComponentGeneric<T>(JSerializable comp) where T : JSerializable
        {
            T newComp = (T)comp.GetCopy();
            return newComp;
        }

        public static object GetCopiedComponent(Type type, JSerializable comp)
        {
            var method = typeof(EntityManager).GetMethod(nameof(GetCopiedComponentGeneric)).MakeGenericMethod(type);
            return method.Invoke(null, new object[] { comp });
        }

        public static void AddComponentToBuffer(Type type, in Entity ent, object component)
        {
            try
            {
                var method = typeof(CommandBuffer).GetMethod("Add").MakeGenericMethod(type);
                method.Invoke(cmdBuffer, new object[] { ent, component });
            }
            catch(Exception ex)
            {

            }
        }

        public static void SetComponentToBuffer(Type type, in Entity ent, object component)
        {
            try
            {
                var method = typeof(CommandBuffer).GetMethod("Set").MakeGenericMethod(type);
                method.Invoke(cmdBuffer, new object[] { ent, component });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ex");
            }
        }

    }

    class EntityDestroyInfo
    {
        public Entity entity;
        public Rigidbody rb;
    }
}
