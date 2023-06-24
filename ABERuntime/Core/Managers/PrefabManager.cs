using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Components;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
	public static class PrefabManager
	{
		static Dictionary<uint, PrefabAsset> prefabMap = new Dictionary<uint, PrefabAsset>();
        static Dictionary<uint, PrefabAsset> sharedPrefabMap = new Dictionary<uint, PrefabAsset>();

        static Dictionary<uint, Transform> prefabInstances = new Dictionary<uint, Transform>();

        internal static World PrefabWorld;

        internal static Entity EntityToPrefab(in Entity entity, Transform parent)
        {
            Entity copy = PrefabWorld.Create();

            var comps = entity.GetAllComponents();
            var types = entity.GetComponentTypes();

            int transformIndex = Array.IndexOf(types, typeof(Transform));
            if (transformIndex < 0)
                return default(Entity);

            // Set transform
            var transComp = EntityManager.GetCopiedComponent(typeof(Transform), (JSerializable)comps[transformIndex]);
            copy.Add(transComp);

            for (int i = 0; i < comps.Length; i++)
            {
                if (i == transformIndex)
                    continue;

                var comp = comps[i];
                var type = types[i].GetType();

                if (typeof(JSerializable).IsAssignableFrom(type))
                {
                    var newComp = EntityManager.GetCopiedComponent(type, (JSerializable)comp);
                    copy.Add(newComp);
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
                    copy.Add(comp);
                }
                else if (type.IsValueType || type == typeof(string))
                {
                    copy.Add(comp);
                }
            }

            copy.Get<Transform>().SetParent(parent, false);

           
            foreach (var child in entity.Get<Transform>().children.ToList())
            {
                EntityToPrefab(child.entity, copy.Get<Transform>());
            }

            return copy;
        }

        //public static async Task<Entity> InstantiateAsync(string prefabName)
        //{
        //    uint hash = prefabName.ToHash32();

        //    // Check local instance
        //    if (prefabInstances.TryGetValue(hash, out Transform entityTrans))
        //        return EntityManager.Instantiate(entityTrans.entity, null);

        //    // Check local prefab
        //    if (prefabMap.TryGetValue(hash, out PrefabAsset prefabAsset))
        //    {
        //        Transform prefabIns = EntityManager.LoadSerializedPrefab(prefabAsset);
        //        prefabInstances.Add(hash, prefabIns);
        //        return EntityManager.Instantiate(prefabIns.entity, null);
        //    }

        //    // Check shared prefab
        //    if (sharedPrefabMap.TryGetValue(hash, out PrefabAsset sharedPrefabAsset))
        //    {
        //        Transform sharedIns = EntityManager.LoadSerializedPrefab(sharedPrefabAsset);
        //        prefabInstances.Add(hash, sharedIns);
        //        return EntityManager.Instantiate(sharedIns.entity, null);

        //    }

        //    return default(Entity);
        //}

        public static async Task<AsyncEntity> InstantiateAsync(string prefabName)
        {
            uint hash = prefabName.ToHash32();


            // Check local instance
            if (prefabInstances.TryGetValue(hash, out Transform entityTrans))
                return await EntityManager.InstantiateAsync(entityTrans.entity, null);

            // Check local prefab
            if (prefabMap.TryGetValue(hash, out PrefabAsset prefabAsset))
            {
                Transform prefabIns = EntityManager.LoadSerializedPrefab(prefabAsset);
                prefabInstances.Add(hash, prefabIns);
                return await EntityManager.InstantiateAsync(prefabIns.entity, null);
            }

            // Check shared prefab
            if (sharedPrefabMap.TryGetValue(hash, out PrefabAsset sharedPrefabAsset))
            {
                Transform sharedIns = EntityManager.LoadSerializedPrefab(sharedPrefabAsset);
                prefabInstances.Add(hash, sharedIns);
                return await EntityManager.InstantiateAsync(sharedIns.entity, null);

            }

            return null;
        }

        public static Entity Instantiate(string prefabName)
        {
            uint hash = prefabName.ToHash32();

            // Check local instance
            if (prefabInstances.TryGetValue(hash, out Transform entityTrans))
                return EntityManager.Instantiate(entityTrans.entity, null);

            // Check local prefab
            if(prefabMap.TryGetValue(hash, out PrefabAsset prefabAsset))
            {
                Transform prefabIns = EntityManager.LoadSerializedPrefab(prefabAsset);
                prefabInstances.Add(hash, prefabIns);
                return EntityManager.Instantiate(prefabIns.entity, null);
            }

            // Check shared prefab
            if (sharedPrefabMap.TryGetValue(hash, out PrefabAsset sharedPrefabAsset))
            {
                Transform sharedIns = EntityManager.LoadSerializedPrefab(sharedPrefabAsset);
                prefabInstances.Add(hash, sharedIns);
                return EntityManager.Instantiate(sharedIns.entity, null);

            }

            return default(Entity);
        }

        internal static void AddPrefabEntity(in Entity entity, uint hash)
        {
            //entity.Transfer(PrefabWorld);
            //prefabInstances.Add(hash, entity.transform);
        }

        public static void AddPrefabEntity(in Entity entity, string prefabName)
		{
			AddPrefabEntity(entity, prefabName.ToHash32());
		}

        public static void AddPrefabAsset(PrefabAsset prefabAsset, string prefabName)
        {
            uint hash = prefabName.ToHash32();
            if (prefabMap.ContainsKey(hash))
                return;

            prefabMap.Add(hash, prefabAsset);
        }

        public static void AddSharedPrefabAsset(PrefabAsset prefabAsset, string prefabName)
        {
            uint hash = prefabName.ToHash32();
            if (sharedPrefabMap.ContainsKey(hash))
                return;

            sharedPrefabMap.Add(hash, prefabAsset);
        }

        internal static Transform GetPrefabTransform(uint hash)
        {
            if (prefabInstances.TryGetValue(hash, out Transform entityTrans))
                return entityTrans;

            return null;
        }

        internal static Transform GetPrefabTransform(string prefabName)
		{
			return GetPrefabTransform(prefabName.ToHash32());
		}


        internal static void SceneInit()
        {
            PrefabWorld = World.Create();
            PrefabWorld.SubscribeComponentAdded((in Entity entity, ref Transform transform) =>
            {
                transform.SetEntity(entity);
            });
        }

        internal static void UpdatePrefab(uint oldHash, uint newHash)
		{
			if(prefabInstances.TryGetValue(oldHash, out Transform prefabTrans))
			{
                prefabInstances.Remove(oldHash);
                prefabInstances.Add(newHash, prefabTrans);
			}
        }

		internal static void ClearScene()
		{
			prefabMap.Clear();
            prefabInstances.Clear();
            if(PrefabWorld != null)
                World.Destroy(PrefabWorld);
		}

        internal static void ClearSharedPrefabs()
		{
			sharedPrefabMap.Clear();
		}
	}
}

