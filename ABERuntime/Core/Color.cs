using System;
using System.Numerics;

namespace ABEngine.ABERuntime
{
	public class Color
	{
        private Vector4 internalValue;

		public Color()
		{
		}

		internal Color(float r, float g, float b, float a)
		{
			internalValue = new Vector4(r, g, b, a);
		}

		public Vector4 ToVector4()
		{
			return internalValue;
		}

		public Vector3 ToVector3()
		{
			return internalValue.ToVector3();
		}
	}
}

