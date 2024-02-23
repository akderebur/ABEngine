using System;
using ABEngine.ABERuntime.Components;
using Arch.Core;
using Arch.Core.Utils;

namespace ABEngine.ABERuntime
{
	public class ParticleModuleSystem : BaseSystem
	{
		public ParticleModuleSystem()
		{
		}

        readonly QueryDescription pmQuery = new QueryDescription().WithAll<Transform, ParticleModule>();
        readonly QueryDescription spmQuery = new QueryDescription().WithAll<Transform, ScriptableParticleModule>();


        protected override void StartScene()
        {
            Game.GameWorld.Query(in pmQuery, (ref ParticleModule pm, ref Transform transform) =>
            {
                pm.Init(transform);
            });

            Game.GameWorld.Query(in spmQuery, (ref ScriptableParticleModule spm, ref Transform transform) =>
            {
                spm.Init(transform);
            });
        }

        public override void Update(float gameTime, float deltaTime)
        {
            Game.GameWorld.Query(in pmQuery, (ref ParticleModule pm, ref Transform transform) =>
            {
                pm.Update(deltaTime, transform);
            });

            Game.GameWorld.Query(in spmQuery, (ref ScriptableParticleModule spm, ref Transform transform) =>
            {
                spm.Update(deltaTime, transform);
            });

            //var query2 = new QueryDescription { All = new ComponentType[] { typeof(Transform), typeof(ScriptableParticleModule<T>) } };



            //query.Foreach((Entity rbEnt, ref ParticleModule particleModule, ref Transform moduleTrans) =>
            //{
            //    particleModule.Update(deltaTime, moduleTrans);
            //}
            //);
        }
    }
}

