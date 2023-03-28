using System;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
	public class ParticleModuleSystem : BaseSystem
	{
		public ParticleModuleSystem()
		{
		}

        public override void Start()
        {
            var query = _world.CreateQuery().Has<ParticleModule>().Has<Transform>();

            query.Foreach((ref ParticleModule pm, ref Transform transform) =>
            {
                pm.Init(transform);
            });
        }

        public override void Update(float gameTime, float deltaTime)
        {
            var query = _world.CreateQuery().Has<ParticleModule>().Has<Transform>();

            query.Foreach((ref ParticleModule pm, ref Transform transform) =>
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

