using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Animation;

namespace ABEngine.ABERuntime.Core.Animation.StateMatch
{
	public class MatchState
	{
        public MatchStateCondition[] conditions { get; }

        public bool isConditionMet
        {
            get
            {
                if (inverseState != null)
                    return !inverseState.isConditionMet;
                return _isConditionMet;
            }
            private set {  _isConditionMet = value; }
        }
        private bool _isConditionMet;
        internal MatchState inverseState;

        public MatchState(params MatchStateCondition[] conditions)
        {
            if (conditions == null || conditions.Length == 0)
            {
                this.conditions = new MatchStateCondition[0];
                return;
            }

            this.conditions = conditions;
        }

        public void CheckConditions()
        {
            if (inverseState != null)
                return;

            _isConditionMet = true;
            foreach (var condition in conditions)
                _isConditionMet &= condition.isConditionMet;
        }

        public MatchState Inverse()
        {
            MatchState invState = new MatchState();
            invState.inverseState = this;
            return invState;
        }
    }
}

