using System;
using System.Numerics;
using Halak;

namespace ABEngine.ABERuntime.Components
{
    public class Canvas : JSerializable
    {
        private Guid _guid;
        private Vector2 _canvasSize;
        private Vector2 _lastScreenSetSize;
        private Vector2 _lastScreenPixelSetSize;

        private Vector2 _lastFixedSize;
        private Vector2 _lastFixedPixelSize;

        private bool _isDynamicSize;
        private Vector2 _worldSize;

        public Vector2 referenceSize;

        Vector2 pvRatio = Vector2.One;

        public Vector2 canvasSize
        {
            get { return _canvasSize; }
            private set
            {
                _canvasSize = value;
                _worldSize = _canvasSize.PixelToWorld();
            }
        }
        public Vector2 canvasPixelSize { get; set; }

        public bool isDynamicSize {
            get { return _isDynamicSize; }
            set
            {
                _isDynamicSize = value;
                if (value)
                    canvasSize = _lastScreenSetSize;
                else
                    canvasSize = _lastFixedSize;

                Game.RefreshProjection(this);
            }
        }

        public Canvas()
        {

        }

        public Canvas(float width, float height)
        {
            _canvasSize = new Vector2(width, height);
            _lastScreenSetSize = _canvasSize;
            _lastFixedSize = _canvasSize;
            _guid = Guid.NewGuid();
            referenceSize = _canvasSize;
        }

        public void UpdateScreenSize(Vector2 screenSize, Vector2 pixelSize)
        {
            _lastScreenSetSize = screenSize;
            _lastScreenPixelSetSize = pixelSize;

            pvRatio = pixelSize / screenSize;

            if (isDynamicSize)
            {
                canvasSize = _lastScreenSetSize;
                canvasPixelSize = _lastScreenPixelSetSize;
                Game.RefreshProjection(this);
            }
        }

        public void UpdateCanvasSize(Vector2 newSize)
        {
            _lastFixedSize = newSize;
            _lastFixedPixelSize = newSize * pvRatio;
            if(!isDynamicSize)
            {
                canvasSize = _lastFixedSize;
                canvasPixelSize = _lastFixedPixelSize;
                Game.RefreshProjection(this);
            }
        }

        public Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            Vector2 factor = canvasSize / Game.pixelSize;
            return screenPos * factor - canvasSize / 2f; ;
        }

        public Vector2 GetWorldSize()
        {
            return _worldSize;
        }

        public JValue Serialize()
        {
            JsonObjectBuilder canvasJ = new JsonObjectBuilder(200);
            canvasJ.Put("type", GetType().ToString());
            canvasJ.Put("Guid", _guid.ToString());
            canvasJ.Put("CanvasSizeX", canvasSize.X);
            canvasJ.Put("CanvasSizeY", canvasSize.Y);
            canvasJ.Put("CanvasDynamic", isDynamicSize);
            canvasJ.Put("ReferenceX", referenceSize.X);
            canvasJ.Put("ReferenceY", referenceSize.Y);
            return canvasJ.Build();
        }

        public void Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json) || json.ToLower().Equals("null"))
                return;

            var data = JValue.Parse(json);
            _guid = Guid.Parse(data["Guid"]);
            _isDynamicSize = data["CanvasDynamic"];
            _canvasSize = new Vector2(data["CanvasSizeX"], data["CanvasSizeY"]);
            _lastFixedSize = _canvasSize;
            _lastScreenSetSize = _canvasSize;
            referenceSize = new Vector2(data["ReferenceX"], data["ReferenceY"]);
        }

        public void SetReferences()
        {
        }

        public JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }
    }
}
