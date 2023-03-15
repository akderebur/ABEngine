using System;
using ABEngine.ABERuntime;
using System.Numerics;

namespace ABEngine.ABEUI
{
    public class UIAnchor : AutoSerializable
    {
        private Vector2 _anchorPos;
        public Vector2 anchorPos { get { return _anchorPos; } set { _anchorPos = value.ToImGuiRefVector2(); } }

        public UIAnchor()
        {
        }
    }
}
