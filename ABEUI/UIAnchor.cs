using System;
using ABEngine.ABERuntime;
using System.Numerics;

namespace ABEngine.ABEUI
{
    public class UIAnchor : ABComponent
    {
        private Vector2 _anchorPos;
        public Vector2 anchorPos { get { return _anchorPos; } set { _anchorPos = value.ToImGuiRefVector2(); } }

        public UIAnchor()
        {
        }

        public UIAnchor(float anchorX, float anchorY)
        {
            anchorPos = new Vector2(anchorX, anchorY);
        }

        public UIAnchor(Vector2 anchorPos)
        {
            this.anchorPos = anchorPos;
        }
    }
}
