using System;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Components
{
	public class PointLight
	{
        public Vector4 color { get; set; }


    }


    public struct PointLightInfo
    {
        public Vector3 Position;
        public float _padding0;
        public Vector3 Color;
        public float _padding1;
    }

}

