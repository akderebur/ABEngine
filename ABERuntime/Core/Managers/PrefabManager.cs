using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
	public static class PrefabManager
	{
		static Dictionary<uint, Transform> prefabMap = new Dictionary<uint, Transform>();
        static Dictionary<uint, Transform> sharedPrefabMap = new Dictionary<uint, Transform>();

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

