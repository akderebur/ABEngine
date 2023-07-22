using System;
namespace ABEngine.ABERuntime.Core.Animation.StateMatch
{
	public class AnimationChain
	{
		public AnimationMatch animationMatch { get; set; }
		public float chainLockTime { get; set; }

		public AnimationChain()
		{

		}

		public AnimationChain(AnimationMatch animationMatch)
		{
			this.animationMatch = animationMatch;
		}

		public AnimationChain(AnimationMatch animationMatch, float chainLockTime)
		{
			this.animationMatch = animationMatch;
			this.chainLockTime = chainLockTime;
		}
	}
}

