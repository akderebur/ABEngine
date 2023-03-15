using System;
using System.Collections.Generic;

namespace ABEngine.ABERuntime.Tweening
{
	public class TweenSystem : BaseSystem
	{
        internal static List<Tween> tweensList = new List<Tween>();

        public override void Update(float gameTime, float deltaTime)
        {
            var query = Game.GameWorld.CreateQuery().Has<Tweener>();
            query.Foreach((ref Tweener tweener) =>
            {
                tweener.UpdateTime(gameTime);
            }
            );
        }
    }
}

