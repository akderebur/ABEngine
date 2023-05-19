using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Components;
using System.Linq;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
	public static class PrefabManager
	{
		static Dictionary<uint, Transform> prefabMap = new Dictionary<uint, Transform>();
        static Dictionary<uint, Transform> sharedPrefabMap = new Dictionary<uint, Transform>();

        internal static Entity EntityToPrefab(in Entity entity, Transform parent)
        {
            Entity copy = Game.PrefabWorld.CreateEntity();

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

        internal static void AddPrefabEntity(in Entity entity, uint hash)
        {
            entity.Transfer(Game.PrefabWorld);
            prefabMap.Add(hash, entity.transform);
        }

        public static void AddPrefabEntity(in Entity entity, string prefabName)
		{
			AddPrefabEntity(entity, prefabName.ToHash32());
		}

        public static void AddSharedPrefabEntity(in Entity entity, string prefabName)
        {
            entity.Transfer(Game.PrefabWorld);
            sharedPrefabMap.Add(prefabName.ToHash32(), entity.transform);
        }

        internal static Transform GetPrefabTransform(uint hash)
        {
            if (prefabMap.TryGetValue(hash, out Transform entityTrans))
                return entityTrans;

            return null;
        }

        internal static Transform GetPrefabTransform(string prefabName)
		{
			return GetPrefabTransform(prefabName.ToHash32());
		}

		internal static void Init()
		{
			prefabMap.Clear();
			foreach (var sharedPrefab in sharedPrefabMap)
			{
				prefabMap.Add(sharedPrefab.Key, sharedPrefab.Value);
			}
		}

		internal static void UpdatePrefab(uint oldHash, uint newHash)
		{
			if(prefabMap.TryGetValue(oldHash, out Transform prefabTrans))
			{
				prefabMap.Remove(oldHash);
				prefabMap.Add(newHash, prefabTrans);
			}

            if (sharedPrefabMap.TryGetValue(oldHash, out Transform prefabTransShared))
            {
                sharedPrefabMap.Remove(oldHash);
                sharedPrefabMap.Add(newHash, prefabTransShared);
            }
        }

		public static void ClearPrefabs()
		{
			prefabMap.Clear();
		}

		public static void ClearSharedPrefabs()
		{
			sharedPrefabMap.Clear();
		}
	}
}

