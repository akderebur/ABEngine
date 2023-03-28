using System;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Math
{
	public static class MathUtils
	{
        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            Vector3 pitchYawRoll = new Vector3();

            float sqw = q.W * q.W;
            float sqx = q.X * q.X;
            float sqy = q.Y * q.Y;
            float sqz = q.Z * q.Z;

            pitchYawRoll.X = (float)MathF.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (sqz + sqw));     // Yaw 
            pitchYawRoll.Y = (float)MathF.Asin(2f * (q.X * q.Z - q.W * q.Y));                             // Pitch 
            pitchYawRoll.Z = (float)MathF.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (sqy + sqz));      // Roll

            return pitchYawRoll;
        }
    }
}

