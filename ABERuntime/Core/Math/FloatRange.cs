using System;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Math
{
	public class FloatRange : AutoSerializable
	{
		public bool isConstant { get; set; }
		public float value { get; set; }
		public Vector2 range { get; set; }

		private Random _rnd;

		public FloatRange()
		{
			isConstant = true;
			_rnd = new Random();
		}

        public FloatRange(float constantValue)
        {
            isConstant = true;
            _rnd = new Random();
			value = constantValue;
        }

        public float NextValue()
		{
			if (isConstant)
				return value;
			else
				return _rnd.NextFloat(range.X, range.Y);
		}
	}
}

