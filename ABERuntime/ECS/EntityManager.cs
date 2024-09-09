using System;
using ABEngine.ABERuntime.Components;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Box2D.NetStandard.Dynamics.World;
using Friflo.Engine.ECS;
using Transform = ABEngine.ABERuntime.Components.Transform;

namespace ABEngine.ABERuntime.ECS
{
    public static class Entities
    {
        internal static SemaphoreSlim creationSemaphore = new SemaphoreSlim(1);
        internal static SemaphoreSlim frameSemaphore = new SemaphoreSlim(1);

        private static bool immediateDestroy;
        //private static List<EntityDestroyInfo> destroyList = new List<EntityDestroyInfo>();
        private static Dictionary<int, EntityDestroyInfo> destroyMap = new Dictionary<int, EntityDestroyInfo>();
        
        static Entities()
        {
        }

        public static void Init()
        {
            frameSemaphore.Wait();
        }

        public static void CheckEntityChanges()
        {
            if(destroyMap.Count > 0)
            {
                foreach (var entID in destroyMap.Keys.ToList())
                {
                    var destroyInfo = destroyMap[entID];
                    bool canDestroy = destroyInfo.rb == null ? true : destroyInfo.rb.destroyed;

                    if (canDestroy)
                    {
                        var entity = destroyInfo.entity;
                        CheckSubscribers(in entity, false);
                        entity.DeleteEntity();
                        destroyMap.Remove(entID);
                    }
                }
            }
           
            creationSemaphore.Wait();
            /*if (cmdBuffer.Size > 0)
            {
                cmdBuffer.Playback(Game.GameWorld);
            }*/
            creationSemaphore.Release();

            frameSemaphore.Release();
            // Operations that can't disturb the update will wait and run here
            // e.g. Tweener addition
            frameSemaphore.Wait();
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
           
                /*
                Entity newEnt = InstantiateCore(entity, parent, Game.GameWorld);
                return newEnt;
                */
            
            //finally
            //{
            //    creationSemaphore.Release();
            //}
            return default;
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

            /*var comps = entity.GetAllComponents();
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
            */

            return null;
        }

        internal static Entity InstantiateCore(in Entity entity, Transform parent, World world)
        {
            /*Entity copy = world.Create();

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
            }*/

            return default;
        }

        private static void CheckSubscribers(in Entity ent, bool create)
        {
            /*if (Game.notifySystems == null)
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
            }*/
        }

        public static Entity WithTRS(this in Entity entity)
        {
            entity.AddComponent(new TRS());
            entity.AddComponent(new WorldTransform());
            return entity;
        }

        public static Entity CreateEntity()
        {
            var ent = Game.GameWorld.CreateEntity(new EntityName("New Entity"), new EntityGuid());
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity(string entName)
        {
            var ent = Game.GameWorld.CreateEntity(new EntityName(entName), new EntityGuid());
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1>(string entName, in C1 c1)
            where C1 : struct, IComponent
        {
            var ent = Game.GameWorld.CreateEntity(new EntityName(entName), new EntityGuid(), c1);
            CheckSubscribers(in ent, true);
            return ent;
        }

        public static Entity CreateEntity<C1, C2>(string entName, C1 c1, C2 c2)
            where C1 : struct, IComponent
            where C2 : struct, IComponent
        {
            var ent = Game.GameWorld.CreateEntity(new EntityName(entName), new EntityGuid(), c1, c2);
            CheckSubscribers(in ent, true);
            return ent;
        }


        public static void DestroyEntity(this in Entity entity)
        {
            /*if (Entities.immediateDestroy)
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
            }*/
        }


        internal static void SetImmediateDestroy(bool imDestroy)
        {
            Entities.immediateDestroy = imDestroy;
        }

        public static Transform FindTransformByName(string name)
        {
            Transform found = null;

            /*var query = new QueryDescription().WithAll<Transform>();
            var entities = new List<Entity>();
            Game.GameWorld.GetEntities(query, entities);

            foreach (var ent in entities)
            {
                if (ent.Get<Transform>().name.Equals(name))
                {
                    found = ent.Get<Transform>();
                    break;
                }
            }*/

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
            var method = typeof(Entities).GetMethod(nameof(DeserializeComponentGeneric)).MakeGenericMethod(type);
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
            var method = typeof(Entities).GetMethod(nameof(GetCopiedComponentGeneric)).MakeGenericMethod(type);
            return method.Invoke(null, new object[] { comp });
        }

        public static void AddComponentToBuffer(Type type, in Entity ent, object component)
        {
            /*try
            {
                var method = typeof(CommandBuffer).GetMethod("Add").MakeGenericMethod(type);
                method.Invoke(cmdBuffer, new object[] { ent, component });
            }
            catch(Exception ex)
            {

            }*/
        }

        public static void SetComponentToBuffer(Type type, in Entity ent, object component)
        {
            /*try
            {
                var method = typeof(CommandBuffer).GetMethod("Set").MakeGenericMethod(type);
                method.Invoke(cmdBuffer, new object[] { ent, component });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ex");
            }*/
        }

    }

    class EntityDestroyInfo
    {
        public Entity entity;
        public Rigidbody rb;
    }
}
