using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Halak;

namespace ABEngine.ABERuntime.Animation
{
    public class Animator : JSerializable
    {
        private string _animGraph;
        public string animGraph
        {
            get { return _animGraph; }
            set
            {
                _animGraph = value;
                LoadAnimGraph();
            }
        }

        public float Time { get; internal set; }
        private AnimationState _currentState;
        private AnimationState _nextState;
        private List<string> _transitionParams;

        private List<AnimationState> _animStates;
        private List<AnimationTransition> _transitions;

        private Dictionary<Guid, AnimationState> _stateDict;
        private Dictionary<string, Trigger> _triggers { get; set; }
        public Dictionary<string, float> parameters { get; set; }

        public Animator()
        {
            ResetFields();
        }

        public Animator(string animGraphPath)
        {
            animGraph = animGraphPath;
            LoadAnimGraph();
        }

        void LoadAnimGraph()
        {
            ResetFields();

            //if (!string.IsNullOrEmpty(animGraph))
            //{
            //    string fullPath = Game.AssetPath + animGraph;
            //    if (File.Exists(fullPath))
            //    {
            //        JValue animData = JValue.Parse(File.ReadAllText(fullPath));

            //        // Params
            //        foreach (var paramData in animData["Params"].Array())
            //        {
            //            SetParameter(paramData["Key"], paramData["Value"]);
            //        }

            //        // States
            //        foreach (var stateKV in animData["States"].IndexedArray())
            //        {
            //            AnimationState newState = new AnimationState();
            //            newState.Deserialize(stateKV.Value.ToString());
            //            if (stateKV.Key == 0)
            //                AddAnimationState(newState, true);
            //            else
            //                AddAnimationState(newState, false);
            //        }

            //        // Transitions
            //        foreach (var transData in animData["Transitions"].Array())
            //        {
            //            AnimationTransition newTrans = new AnimationTransition(AnimTransCompareType.Equals);
            //            newTrans.SetAnimator(this);
            //            newTrans.Deserialize(transData.ToString());
            //            _transitions.Add(newTrans);
            //        }
            //    }

            //}

            Init();
        }

        void ResetFields()
        {
            _animStates = new List<AnimationState>();
            _transitions = new List<AnimationTransition>();
            _stateDict = new Dictionary<Guid, AnimationState>();
            _triggers = new Dictionary<string, Trigger>();
            parameters = new Dictionary<string, float>();
        }

        public void Init()
        {
            foreach (var animState in _animStates)
            {
                var transTimeOrdered = _transitions.Where(t => t.startState == animState).OrderBy(t => t.exitTime);
                var paramTranses = transTimeOrdered.Where(t => t.hasCondition);
                if(paramTranses.Count() > 0)
                    animState.transitions.AddRange(paramTranses);
                var exitTimeTranses = transTimeOrdered.Where(t => !t.hasCondition);
                if (exitTimeTranses.Count() > 0)
                    animState.transitions.AddRange(exitTimeTranses);
            }
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

        public void SetParameterTrigger(string key, float value, float resetTime)
        {
            SetParameterTrigger(key, value, parameters.ContainsKey(key) ? parameters[key] : value, resetTime, null);
        }

        public void SetParameterTrigger(string key, float value, float endValue, float resetTime)
        {
            SetParameterTrigger(key, value, endValue, resetTime, null);
        }

        public void SetParameterTrigger(string key, float value, float resetTime, Action triggerResetAction)
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

        public void AddAnimationState(AnimationState state, bool isEntry)
        {
            if (isEntry)
            {
                _animStates.Insert(0, state);
                _currentState = state;
            }
            else
                _animStates.Add(state);

            _stateDict.Add(state.stateUID, state);
        }

        public void AddAnimationState(AnimationState state)
        {
            _animStates.Add(state);
            _stateDict.Add(state.stateUID, state);
        }

        public void AddTransition(AnimationTransition transition)
        {
            transition.SetAnimator(this);
            _transitions.Add(transition);
        }

        public bool CheckTransitions()
        {
            bool shouldTransition = _currentState.GetNextState(ref _nextState, ref _transitionParams);
            if (shouldTransition)
            {
                _currentState = _nextState;
                _currentState.curFrame = 0;
                _currentState.lastFrameTime = 0f;

                foreach (var param in _transitionParams)
                {
                    if (_triggers.ContainsKey(param))
                    {
                        Trigger trigger = _triggers[param];

                        if (trigger.timeLeft <= 0)
                        {
                            parameters[trigger.key] = trigger.orgValue;
                            trigger.resetAction?.Invoke();
                            _triggers.Remove(trigger.key);
                        }
                    }
                }
            }

            return shouldTransition;
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

        internal void ResetTrigger()
        {

        }

        public AnimationState GetCurrentState()
        {
            return _currentState;
        }

        public AnimationState GetEntryState()
        {
            return _animStates.Count > 0 ? _animStates[0] : null;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(100);
            jObj.Put("type", GetType().ToString());
            jObj.Put("AnimGraph", animGraph);
            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            animGraph = data["AnimGraph"];
            LoadAnimGraph();
        }

        public void SetReferences()
        {
            
        }

        internal AnimationState GetStateByUID(Guid stateUID)
        {
            return _stateDict[stateUID];
        }

        public JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }
    }

    class Trigger
    {
        public string key { get; set; }
        public float orgValue { get; set; }
        public float timeLeft { get; set; }
        public Action resetAction { get; set; }
    }
}
