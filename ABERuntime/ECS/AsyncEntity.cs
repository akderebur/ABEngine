using System;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using System.Collections.Generic;
using Arch.Core.Utils;

namespace ABEngine.ABERuntime.ECS
{
	public class AsyncEntity
	{
		Entity entity;
		Dictionary<Type, object> components;

        public AsyncEntity(in Entity entity, Dictionary<Type, object> compList)
		{
			this.entity = entity;
			components = compList;
		}

		public async Task Add<T>(T component)
		{
            await EntityManager.creationSemaphore.WaitAsync();
            EntityManager.cmdBuffer.Add<T>(in entity, component);
            EntityManager.creationSemaphore.Release();
	
		}

		public async Task Set<T>(T component)
		{
            await EntityManager.creationSemaphore.WaitAsync();
			EntityManager.cmdBuffer.Set<T>(in entity, component);
            EntityManager.creationSemaphore.Release();
        }

		public bool Has<T>()
		{
			if (entity.Has<T>())
				return true;

			if (components.ContainsKey(typeof(T)))
				return true;

			return false;
		}

        public T Get<T>()
        {
			if (entity.Has<T>())
				return entity.Get<T>();

			if (components.TryGetValue(typeof(T), out object component))
				return (T)component;
			else
				return default(T);

			//if (entity.Has<T>())
			//	return entity.Get<T>();

			//int failC = 0;
			//Entity resolveEnt = Entity.Null;
			//while(failC < 20)
			//{
			//	var query = new QueryDescription().WithAll<T>();
			//	Game.GameWorld.Query(in query, (in Entity qEnt) =>
			//	{
			//		if (qEnt.Id == entity.Id)
			//		{
			//			resolveEnt = qEnt;
			//			return;
			//		}
			//	});

			//	if (resolveEnt != Entity.Null)
			//	{
			//		entity = resolveEnt;
			//		break;
			//	}


   //             failC++;

   //             await EntityManager.frameSemaphore.WaitAsync();
			//	EntityManager.frameSemaphore.Release();
			//}

			//if (failC >= 20)
			//	return default(T);
			//else
			//	return entity.Get<T>();
        }
    }
}

