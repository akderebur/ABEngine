using System;
namespace ABEngine.ABERuntime.Animation
{
	public class AnimTransCondition
	{
		public string parameterKey { get; set; }
		public float targetValue { get; set; }
        public string conditionSymbol { get; set; }

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

        public AnimTransCondition(string parameterKey, AnimTransCompareType compareType, float targetValue)
		{
            this.parameterKey = parameterKey;
            this.targetValue = targetValue;
            paramCompareType = compareType;
		}
        public static AnimTransCondition Default()
        {
            return new AnimTransCondition("", AnimTransCompareType.Equals, 0f);
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
                default:
                    conditionSymbol = "=";
                    _paramComparer = new AnimTransEqualsComparer();
                    break;
            }
        }

        public bool IsConditionMet(float curValue)
        {
            return _paramComparer.CompareResult(curValue, targetValue);
        }

        public string GetTransitionText()
        {
            if (string.IsNullOrEmpty(parameterKey))
                return "No condition";

            return parameterKey + " " + conditionSymbol + " " + targetValue;
        }

    }
}

