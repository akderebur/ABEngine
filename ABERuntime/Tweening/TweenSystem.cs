using System;
using System.Collections.Generic;
using Arch.Core;

namespace ABEngine.ABERuntime.Tweening
{
	public class TweenSystem : BaseSystem
	{
        internal static List<Tween> tweensList = new List<Tween>();

        public override void Update(float gameTime, float deltaTime)
        {
            var query = new QueryDescription().WithAll<Tweener>();
            Game.GameWorld.Query(in query, (ref Tweener tweener) =>
            {
                tweener.UpdateTime(gameTime);
            });
        }
    }
}

