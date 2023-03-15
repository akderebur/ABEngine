using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.ECS;
using Halak;

namespace ABEngine.ABERuntime.Tweening
{
	public class Tweener
	{
		internal List<Tween> tweens { get; set; }
		internal List<Tween> toRemoves = new List<Tween>();

		public bool paused { get; set; }

		public Tweener()
		{
			tweens = new List<Tween>();
		}

		public void SetTween(Tween tween)
		{
			tween.SetTweener(this);
			tweens.Add(tween);
		}

        public void RemoveTween(Tween tween)
        {
			if (tweens.Contains(tween))
				toRemoves.Add(tween);
        }

		public void Pause(bool isPaused)
		{
			paused = isPaused;
            int tweenC = tweens.Count;
            for (int i = 0; i < tweenC; i++)
            {
                tweens[i].Pause(isPaused);
            }
        }

        internal void UpdateTime(float time)
		{
			if (paused)
				return;


			while(toRemoves.Count > 0)
			{
				Tween toRemove = toRemoves[0];
				tweens.Remove(toRemove);
				toRemoves.Remove(toRemove);
			}

			int tweenC = tweens.Count;
			for (int i = 0; i < tweenC; i++)
			{
				tweens[i].UpdateTween(time);
			}
		}
    }
}

