using System;
using System.Collections.Generic;
using Halak;

namespace ABEngine.ABERuntime.Animation
{
    public class AnimationTransition : JSerializable
    {
        public AnimationState startState { get; set; }
        public AnimationState endState { get; set; }

        public bool hasCondition { get; set; }
        public float exitTime { get; set; }
        public float transitionTime { get; set; }

        private Animator _animator;
        public AnimTransCondition[] conditions { get; }
        internal List<string> transParamKeys { get; set; }


        // References
        internal Guid startStateUID;
        internal Guid endStateUID;


        public AnimationTransition(params AnimTransCondition[] conditions)
        {
            transParamKeys = new List<string>();
            if(conditions == null || conditions.Length == 0)
            {
                this.conditions = new AnimTransCondition[0];
                hasCondition = false;
                return;
            }

            this.conditions = conditions;
            hasCondition = true;

            foreach (var cond in conditions)
            {
                transParamKeys.Add(cond.parameterKey);
            }

        }

        public void SetAnimator(Animator anim)
        {
            _animator = anim;
        }

        public AnimationState GetNextState()
        {
            if(!hasCondition) // No Condition
            {
                if (startState.unclampedNormTime >= exitTime)
                {
                    return endState;
                }
                else
                    return startState;
            }
            else
            {
                bool conditionMet = true;
                foreach (var cond in conditions)
                {
                    float paramValue = _animator.GetParameter(cond.parameterKey);
                    conditionMet &= cond.IsConditionMet(paramValue);
                }

                if (startState.unclampedNormTime >= exitTime && conditionMet)
                {
                    return endState;
                }
                else
                    return startState;
            }
           
        }

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(200);
            //jObj.Put("type", GetType().ToString());
            //jObj.Put("StartState", startState.stateUID.ToString());
            //jObj.Put("EndState", endState.stateUID.ToString());
            //jObj.Put("ParameterKey", string.IsNullOrEmpty(parameterKey) ? "" : parameterKey);
            //jObj.Put("TargetValue", targetValue);
            //jObj.Put("AnimTransCompareType", (int)_paramCompareType);
            //jObj.Put("ExitTime", exitTime);

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            //JValue data = JValue.Parse(json);
            //startStateUID = Guid.Parse(data["StartState"]);
            //endStateUID = Guid.Parse(data["EndState"]);
            //parameterKey = data["ParameterKey"];
            //targetValue = data["TargetValue"];
            //paramCompareType = (AnimTransCompareType)((int)data["AnimTransCompareType"]);
            //exitTime = data["ExitTime"];

            //startState = _animator.GetStateByUID(startStateUID);
            //endState = _animator.GetStateByUID(endStateUID);

            //startState.transitions.Add(this);
        }

        public void SetReferences()
        {
        }

        public JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }
    }

    public enum AnimTransCompareType
    {
        Equals,
        Greater,
        Less,
        NotEquals
    }
}
