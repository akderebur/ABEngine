using System;
using System.Numerics;

namespace ABEngine.ABERuntime.Physics
{
    public static class PhysicsExtensions
    {
        public static Vector2 ToB2DVector(this Vector3 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static Vector2 ToB2DVector(this Vector2 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }
    }
}
