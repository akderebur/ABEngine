using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Components;
using Arch.Core;

namespace ABEngine.ABERuntime.Tweening
{
	public class TweenSystem : BaseSystem
	{
        private readonly QueryDescription tweenQUery = new QueryDescription().WithAll<Tweener, Transform>();

        internal static List<Tween> tweensList = new List<Tween>();

        public override void Update(float gameTime, float deltaTime)
        {
            Game.GameWorld.Query(in tweenQUery, (ref Tweener tweener) =>
            {
                tweener.UpdateTime(gameTime);
            });
        }
    }
}

