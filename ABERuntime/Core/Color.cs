using System;
using Veldrid;
using System.Numerics;

namespace ABEngine.ABERuntime
{
	public class Color
	{
        public static Color Black => new Color(RgbaFloat.Black);
        public static Color White => new Color(RgbaFloat.White);
        public static Color Gray => new Color(RgbaFloat.Grey);
        public static Color Red => new Color(RgbaFloat.Red);
        public static Color Green => new Color(RgbaFloat.Green);
        public static Color Blue => new Color(RgbaFloat.Blue);
        public static Color Yellow => new Color(RgbaFloat.Yellow);
        public static Color Orange => new Color(RgbaFloat.Orange);
        public static Color Pink => new Color(RgbaFloat.Pink);

        private Vector4 internalValue;

		public Color()
		{
		}

		internal Color(RgbaFloat color)
		{
			internalValue = color.ToVector4();
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

