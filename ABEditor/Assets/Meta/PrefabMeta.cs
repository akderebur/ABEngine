﻿using System;
using System.IO;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Arch.Core;
using Arch.Core.Extensions;
using Halak;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class PrefabMeta : AssetMeta
	{
		public PrefabMeta()
		{
		}

        public override void DrawMeta()
        {

        }

        public override JValue Serialize()
        {
            base.Serialize();
            return jObj.Build();
        }

        public override void Deserialize(string json)
        {
            base.Deserialize(json);
        }


        // Asset Serialization

        internal static void CreatePrefabAsset(string savePath, in Entity entity)
        {
            string assetPath = savePath.Replace(Game.AssetPath, "");
            uint fileHash = assetPath.ToHash32();

            var prefabEntity = PrefabManager.EntityToPrefab(entity, null);
            PrefabManager.AddPrefabEntity(prefabEntity, fileHash);

            PrefabAsset prefabAsset = new PrefabAsset(fileHash);

            JsonObjectBuilder jPrefab = new JsonObjectBuilder(10000);
            JsonArrayBuilder entArr = new JsonArrayBuilder(10000);

            // Clear assets references
            AssetCache.ClearSerializeDependencies();

            RecurseEntity(entity.Get<Transform>(), entArr);

            // Save assets
            jPrefab.Put("Assets", AssetCache.SerializeAssets());
            jPrefab.Put("Entities", entArr.Build());

            prefabAsset.serializedData = jPrefab.Build().Serialize();
            //entity.Get<Prefab>().prefabAsset = prefabAsset;

            File.WriteAllBytes(savePath, PrefabToRAW(prefabAsset));
            AssetCache.AddPrefab(prefabAsset, assetPath);
        }

        public override void MetaCreated()
        {
            // Handle prefab guid
            var prefab = PrefabManager.GetPrefabTransform(fPathHash);
            if (prefab != null)
                prefab.entity.Set<Guid>(base.uniqueID);
        }

        private static void RecurseEntity(Transform transform, JsonArrayBuilder entArr)
        {
            entArr.Push(SerializeEntity(transform.entity));
            foreach (var child in transform.children)
                RecurseEntity(child, entArr);
        }

        private static JValue SerializeEntity(in Entity entity)
        {
            JsonObjectBuilder entObj = new JsonObjectBuilder(10000);
            entObj.Put("GUID", entity.Get<Guid>().ToString());
            entObj.Put("Name", entity.Get<string>());

            JsonArrayBuilder compArr = new JsonArrayBuilder(10000);
            var comps = entity.GetAllComponents();
            var types = entity.GetComponentTypes();

            // Serialize transform first
            int transIndex = Array.IndexOf(types, typeof(Transform));
            compArr.Push(((JSerializable)comps[transIndex]).Serialize());

            for (int i = 0; i < comps.Length; i++)
            {
                Type type = types[i].Type;
                if (type == typeof(Transform))
                    continue;

                if (typeof(JSerializable).IsAssignableFrom(type))
                {
                    compArr.Push(((JSerializable)comps[i]).Serialize());
                }
                else if (type.IsSubclassOf(typeof(ABComponent)))
                {
                    //ompArr.Push(((AutoSerializable)comps[i]).Serialize());
                    compArr.Push(ABComponent.Serialize((ABComponent)comps[i]));

                }
            }

            entObj.Put("Components", compArr.Build());
            return entObj.Build();
        }


        internal static byte[] PrefabToRAW(PrefabAsset prefabAsset)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(prefabAsset.serializedData);
                return ms.ToArray();
            }
        }

        public override PrefabAsset CreateAssetBinding()
        {
            PrefabAsset prefabAsset = AssetCache.CreatePrefabAsset(base.fPath);
            return prefabAsset;
        }

        public override JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }

        public override void SetReferences()
        {
            throw new NotImplementedException();
        }
    }
}

