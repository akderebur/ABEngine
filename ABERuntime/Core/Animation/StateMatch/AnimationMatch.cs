﻿using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Animation;

namespace ABEngine.ABERuntime.Core.Animation.StateMatch
{
	public class AnimationMatch
	{
		public List<MatchState> matchStates;
		public AnimationState animationState;

		public float lockTime = -1f;
		public float chainLockTime = -1f;
		public bool chainOnly;
		public List<AnimationChain> chainStates;

		public string startTag = "";
		public string endTag = "";

		public AnimationMatch()
		{
			matchStates = new List<MatchState>();
			chainStates = new List<AnimationChain>();
		}

		public bool IsStatesMatched()
		{
			bool result = true;
			foreach (var state in matchStates)
				result &= state.isConditionMet;
			return result;
		}

		internal bool IsLocked()
		{
			return animationState.normalizedTime < lockTime;
		}

        internal bool IsChainLocked(AnimationChain chain)
        {
            return animationState.normalizedTime < chain.chainLockTime;
        }
    }
}

