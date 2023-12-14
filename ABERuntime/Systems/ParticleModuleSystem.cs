using System;
using ABEngine.ABERuntime.Components;
using Arch.Core;

namespace ABEngine.ABERuntime
{
	public class ParticleModuleSystem : BaseSystem
	{
		public ParticleModuleSystem()
		{
		}

        public override void Start()
        {
            base.Start();

            var query = new QueryDescription().WithAll<Transform, ParticleModule>();

            Game.GameWorld.Query(in query, (ref ParticleModule pm, ref Transform transform) =>
            {
                pm.Init(transform);
            });
        }

        public override void Update(float gameTime, float deltaTime)
        {
            var query = new QueryDescription().WithAll<Transform, ParticleModule>();

            Game.GameWorld.Query(in query, (ref ParticleModule pm, ref Transform transform) =>
            {
                pm.Update(deltaTime, transform);
            });

            //query.Foreach((Entity rbEnt, ref ParticleModule particleModule, ref Transform moduleTrans) =>
            //{
            //    particleModule.Update(deltaTime, moduleTrans);
            //}
            //);
        }
    }
}

