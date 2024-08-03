using System.Numerics;

namespace ABEngine.ABERuntime.Core.MathA
{
    public class BezierCurve : ABComponent
    {
        public Vector2 startPoint { get; set; }
        public Vector2 endPoint { get; set; }
        public Vector2 controlPoint1 { get; set; }
        public Vector2 controlPoint2 { get; set; }

        public float offset
        {
            get => _offset;
            set { _offset = value; _offsetVec = Vector2.One * _offset; }
        }

        public float scale
        {
            get => _scale;
            set { _scale = value; _scaleVec = Vector2.One * _scale; }
        }

        private float _offset;
        private float _scale;

        private Vector2 _offsetVec;
        private Vector2 _scaleVec;

        public BezierCurve(Vector2 startPoint, Vector2 endPoint, Vector2 controlPoint1, Vector2 controlPoint2)
        {
            this.startPoint = startPoint;
            this.endPoint = endPoint;
            this.controlPoint1 = controlPoint1;
            this.controlPoint2 = controlPoint2;

            offset = 0f;
            scale = 1f;
        }

        public Vector2 Evaluate(float t)
        {
            if (t < 0.0f || t > 1.0f)
                return Vector2.Zero;

            float invT = 1.0f - t;

            return invT * invT * invT * (startPoint * _scaleVec + _offsetVec) +
                   3 * invT * invT * t * (controlPoint1 * _scaleVec + _offsetVec) +
                   3 * invT * t * t * (controlPoint2 * _scaleVec + _offsetVec) +
                   t * t * t * (endPoint * _scaleVec + _offsetVec);
        }
    }

}

