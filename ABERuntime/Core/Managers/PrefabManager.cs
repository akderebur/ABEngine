using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Components;
using System.Linq;
using ABEngine.ABERuntime.ECS;
using System.Threading.Tasks;

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
            Entity copy = PrefabWorld.CreateEntity();

            var comps = entity.GetAllComponents();
            var types = entity.GetAllComponentTypes();

            int transformIndex = Array.IndexOf(types, typeof(Transform));
            if (transformIndex < 0)
                return default(Entity);

            // Set transform
            var transComp = ((JSerializable)comps[transformIndex]).GetCopy();
            copy.Set(types[transformIndex], transComp);

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
                    copy.Set(type, comp);
                }
                else if (type.IsValueType || type == typeof(string))
                {
                    copy.Set(type, comp);
                }
            }

            copy.transform.SetParent(parent, false);

           
            foreach (var child in entity.transform.children.ToList())
            {
                EntityToPrefab(child.entity, copy.transform);
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

        public static async Task<Entity> InstantiateAsync(string prefabName, TaskInfo taskInfo)
        {
            uint hash = prefabName.ToHash32();

            // Check local instance
            if (prefabInstances.TryGetValue(hash, out Transform entityTrans))
                return await EntityManager.InstantiateAsync(entityTrans.entity, taskInfo, null);

            // Check local prefab
            if (prefabMap.TryGetValue(hash, out PrefabAsset prefabAsset))
            {
                Transform prefabIns = EntityManager.LoadSerializedPrefab(prefabAsset);
                prefabInstances.Add(hash, prefabIns);
                return await EntityManager.InstantiateAsync(prefabIns.entity, taskInfo, null);
            }

            // Check shared prefab
            if (sharedPrefabMap.TryGetValue(hash, out PrefabAsset sharedPrefabAsset))
            {
                Transform sharedIns = EntityManager.LoadSerializedPrefab(sharedPrefabAsset);
                prefabInstances.Add(hash, sharedIns);
                return await EntityManager.InstantiateAsync(sharedIns.entity, taskInfo, null);

            }

            return default(Entity);
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
            entity.Transfer(PrefabWorld);
            prefabInstances.Add(hash, entity.transform);
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
            PrefabWorld = World.Create("Prefabs");
            PrefabWorld.OnSet((Entity entity, ref Transform newTrans) => newTrans.SetEntity(entity));
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
            PrefabWorld.Destroy();
		}

        internal static void ClearSharedPrefabs()
		{
			sharedPrefabMap.Clear();
		}
	}
}

