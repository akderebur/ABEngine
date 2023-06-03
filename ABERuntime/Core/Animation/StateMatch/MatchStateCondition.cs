using System;
using ABEngine.ABERuntime.Animation;

namespace ABEngine.ABERuntime.Core.Animation.StateMatch
{
    public class MatchStateCondition
    {
        public string parameterKey { get; set; }
        public float targetValue { get; set; }
        public string conditionSymbol { get; set; }
        public bool isConditionMet = false;

        private AnimTransCompareType _paramCompareType;
        public AnimTransCompareType paramCompareType
        {
            get { return _paramCompareType; }
            set
            {
                _paramCompareType = value;
                SetCompararerFromType();
            }
        }

        private AnimTransComparer _paramComparer;

        public MatchStateCondition(string parameterKey, AnimTransCompareType compareType, float targetValue)
        {
            this.parameterKey = parameterKey;
            this.targetValue = targetValue;
            paramCompareType = compareType;
        }
        public static MatchStateCondition Default()
        {
            return new MatchStateCondition("", AnimTransCompareType.Equals, 0f);
        }

        void SetCompararerFromType()
        {
            switch (_paramCompareType)
            {
                case AnimTransCompareType.Greater:
                    conditionSymbol = ">";
                    _paramComparer = new AnimTransGreaterComparer();
                    break;
                case AnimTransCompareType.Less:
                    conditionSymbol = "<";
                    _paramComparer = new AnimTransLessComparer();
                    break;
                case AnimTransCompareType.NotEquals:
                    conditionSymbol = "!=";
                    _paramComparer = new AnimTransNotEqualsComparer();
                    break;
                default:
                    conditionSymbol = "=";
                    _paramComparer = new AnimTransEqualsComparer();
                    break;
            }
        }

        public void CheckCondition(float curValue)
        {
            isConditionMet = _paramComparer.CompareResult(curValue, targetValue);
        }

        //public bool IsConditionMet(float curValue)
        //{
        //    return _paramComparer.CompareResult(curValue, targetValue);
        //}

        public string GetTransitionText()
        {
            if (string.IsNullOrEmpty(parameterKey))
                return "No condition";

            return parameterKey + " " + conditionSymbol + " " + targetValue;
        }

    }
}

