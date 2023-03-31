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

        public float offset { get { return _offset; } set { _offset = value; _offsetVec = Vector2.One * _offset; } }
        public float scale { get { return _scale; } set { _scale = value; _scaleVec = Vector2.One * _scale; } }

        private float _offset;
        private float _scale;

        private Vector2 _offsetVec;
        private Vector2 _scaleVec;

        public BezierCurve(Vector2 startPoint, Vector2 endPoint, Vector2 controlPoint1, Vector2 controlPoint2)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            ControlPoint1 = controlPoint1;
            ControlPoint2 = controlPoint2;

            offset = 0f;
            scale = 1f;
        }

        public Vector2 Evaluate(float t)
        {
            if (t < 0.0f || t > 1.0f)
                return Vector2.Zero;

            float invT = 1.0f - t;

            return invT * invT * invT * (StartPoint * _scaleVec + _offsetVec) +
                   3 * invT * invT * t * (ControlPoint1 * _scaleVec + _offsetVec) +
                   3 * invT * t * t * (ControlPoint2 * _scaleVec + _offsetVec) +
                   t * t * t * (EndPoint * _scaleVec + _offsetVec);
        }
    }

}

