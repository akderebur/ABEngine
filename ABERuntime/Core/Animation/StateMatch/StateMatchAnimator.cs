using System;
using System.Collections.Generic;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Core.Animation.StateMatch
{
	public class StateMatchAnimator
	{
        public float Time { get; internal set; }

        public Transform transform { get; private set; }
        public Dictionary<string, float> parameters;
        private AnimationMatch _currentAnimMatch;

        private List<AnimationMatch> _animMatches;
        private HashSet<MatchState> _matchStates;
        private HashSet<MatchStateCondition> _conditions;

        private Dictionary<string, Trigger> _triggers { get; set; }

        public event Action<StateMatchAnimator, AnimationMatch> OnAnimationFinished;
        public event Action<StateMatchAnimator, AnimationMatch> OnAnimationStarted;

        public StateMatchAnimator()
		{
			parameters = new Dictionary<string, float>();
            _animMatches = new List<AnimationMatch>();
            _matchStates = new HashSet<MatchState>();
            _conditions = new HashSet<MatchStateCondition>();
            _triggers = new Dictionary<string, Trigger>();
		}

        public float GetParameter(string key)
        {
            if (parameters.ContainsKey(key))
                return parameters[key];
            else
                return float.NaN;
        }

        public void SetParameter(string key, float value)
        {
            if (parameters.ContainsKey(key))
                parameters[key] = value;
            else
                parameters.Add(key, value);
        }

        public void AddAnimationMatch(AnimationMatch animMatch)
        {
            _animMatches.Add(animMatch);
        }

        public void Init()
        {
            foreach (var animMatch in _animMatches)
            {
                foreach (var matchState in animMatch.matchStates)
                {
                    //_matchStates.Add(matchState);
                    MatchState targetState = matchState;

                    if (matchState.inverseState != null)
                        targetState = matchState.inverseState;

                    _matchStates.Add(targetState);
                    foreach (var condition in targetState.conditions)
                    {
                        _conditions.Add(condition);
                    }
                }
            }

            _currentAnimMatch = _animMatches[0];
        }

        public bool CheckStates()
        {
            foreach (var condition in _conditions)
            {
                condition.CheckCondition(parameters[condition.parameterKey]);
            }

            foreach (var matchState in _matchStates)
            {
                matchState.CheckConditions();
            }

            // Check chain states
            foreach (var animChain in _currentAnimMatch.chainStates)
            {
                if (animChain.animationMatch.IsStatesMatched() && !_currentAnimMatch.IsChainLocked(animChain))
                {
                    _currentAnimMatch.animationState.completed = false;
                    _currentAnimMatch = animChain.animationMatch;
                    return true;
                }
            }
            
            // Can't transition to non-chain states if locked
            if (_currentAnimMatch.IsLocked())
                return false;

            foreach (var animMatch in _animMatches)
            {
                if(!animMatch.chainOnly && animMatch.IsStatesMatched())
                {
                    if (_currentAnimMatch != animMatch)
                    {
                        _currentAnimMatch.animationState.completed = false;
                        _currentAnimMatch = animMatch;
                        return true;
                    }
                    else
                        return false;
                }
            }

            return false;
        }

        public void CheckTriggers(float deltaTime)
        {
            if (_triggers.Count < 1)
                return;

            List<string> delKeys = new List<string>();
            foreach (var triggerKV in _triggers)
            {
                Trigger trigger = triggerKV.Value;
                if (trigger.timeLeft > 0)
                    trigger.timeLeft -= deltaTime;
                else if (trigger.timeLeft <= 0)
                {
                    parameters[trigger.key] = trigger.orgValue;
                    trigger.resetAction?.Invoke();
                    delKeys.Add(triggerKV.Key);
                }
            }

            foreach (var delKey in delKeys)
            {
                _triggers.Remove(delKey);
            }
        }

        public AnimationMatch GetCurrentAnimMatch()
        {
            return _currentAnimMatch;
        }

        // Triggers - Timed parameters

        public void SetTimedParameter(string key, float value, float resetTime)
        {
            SetParameterTrigger(key, value, parameters.ContainsKey(key) ? parameters[key] : value, resetTime, null);
        }

        public void SetTimedParameter(string key, float value, float endValue, float resetTime)
        {
            SetParameterTrigger(key, value, endValue, resetTime, null);
        }

        public void SetTimedParameter(string key, float value, float resetTime, Action triggerResetAction)
        {
            SetParameterTrigger(key, value, parameters.ContainsKey(key) ? parameters[key] : value, resetTime, triggerResetAction);
        }

        public void SetParameterTrigger(string key, float value, float endValue, float resetTime, Action triggerResetAction)
        {
            if (!_triggers.ContainsKey(key))
            {
                if (parameters.ContainsKey(key))
                {
                    parameters[key] = value;
                }
                else
                    parameters.Add(key, value);

                Trigger trigger = new Trigger()
                {
                    key = key,
                    orgValue = endValue,
                    timeLeft = resetTime,
                    resetAction = triggerResetAction
                };
                _triggers.Add(key, trigger);
            }
        }

        internal void AnimationComplete(AnimationMatch animationMatch)
        {
            OnAnimationFinished?.Invoke(this, animationMatch);
        }

        internal void AnimationStarted(AnimationMatch animationMatch)
        {
            OnAnimationStarted?.Invoke(this, animationMatch);
        }

        internal void SetTransform(Transform transform)
        {
            this.transform = transform;
        }

        internal HashSet<IClip> GetAllClips()
        {
            HashSet<IClip> clips = new HashSet<IClip>();

            foreach (var animMatch in _animMatches)
            {
                IClip clip = animMatch.animationState?.clip;
                if (clip != null)
                    clips.Add(clip);
            }

            return clips;
        }
    }
}

