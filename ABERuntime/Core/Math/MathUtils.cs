using System;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Math
{
	public static class MathUtils
	{
        //public static Vector3 ToEulerAngles(this Quaternion q)
        //{
        //    Vector3 pitchYawRoll = new Vector3();

        //    float sqw = q.W * q.W;
        //    float sqx = q.X * q.X;
        //    float sqy = q.Y * q.Y;
        //    float sqz = q.Z * q.Z;

        //    pitchYawRoll.X = (float)MathF.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (sqz + sqw));     // Yaw 
        //    pitchYawRoll.Y = (float)MathF.Asin(2f * (q.X * q.Z - q.W * q.Y));                             // Pitch 
        //    pitchYawRoll.Z = (float)MathF.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (sqy + sqz));      // Roll

        //    return pitchYawRoll;
        //}

        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            Vector3 angles = new();

            // roll (x-axis rotation)
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = MathF.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (MathF.Abs(sinp) >= 1)
            {
                angles.Y = MathF.CopySign(MathF.PI / 2, sinp);
            }
            else
            {
                angles.Y = MathF.Asin(sinp);
            }

            // yaw (z-axis rotation)
            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = MathF.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
    }
}

