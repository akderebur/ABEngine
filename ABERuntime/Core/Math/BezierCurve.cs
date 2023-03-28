using System;
using System.Net;
using System.Numerics;

namespace ABEngine.ABERuntime.Core.Math
{
    public class BezierCurve
    {
        public Vector2 StartPoint { get; set; }
        public Vector2 EndPoint { get; set; }
        public Vector2 ControlPoint1 { get; set; }
        public Vector2 ControlPoint2 { get; set; }

        public Vector2 offset { get; set; }
        public Vector2 scale { get; set; }

        public BezierCurve(Vector2 startPoint, Vector2 endPoint, Vector2 controlPoint1, Vector2 controlPoint2)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            ControlPoint1 = controlPoint1;
            ControlPoint2 = controlPoint2;

            offset = Vector2.Zero;
            scale = Vector2.One;
        }

        public Vector2 Evaluate(float t)
        {
            if (t < 0.0f || t > 1.0f)
                return Vector2.Zero;

            float invT = 1.0f - t;

            return invT * invT * invT * (StartPoint * scale + offset) +
                   3 * invT * invT * t * (ControlPoint1 * scale + offset) +
                   3 * invT * t * t * (ControlPoint2 * scale + offset) +
                   t * t * t * (EndPoint * scale + offset);
        }
    }

}

