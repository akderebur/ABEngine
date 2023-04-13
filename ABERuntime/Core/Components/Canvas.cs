using System;
using System.Numerics;
using Halak;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime.Components
{
    public class Canvas : JSerializable
    {
        private Guid _guid;
        private Vector2 _canvasSize;
        private Vector2 _lastCanvasSetSize;
        private bool _isDynamicSize;

        public Vector2 referenceSize;

        public Vector2 canvasSize
        {
            get { return _canvasSize; }
            set
            {
                if (isDynamicSize)
                    _canvasSize = value;
                _lastCanvasSetSize = value;
            }
        }

        public bool isDynamicSize { get { return _isDynamicSize; } set { _isDynamicSize = value; if(value)canvasSize = _lastCanvasSetSize; } }

        public Canvas()
        {

        }

        public Canvas(float width, float height)
        {
            _canvasSize = new Vector2(width, height);
            _lastCanvasSetSize = _canvasSize;
            _guid = Guid.NewGuid();
            referenceSize = _canvasSize;
        }

        public void UpdateScreenSize(Vector2 newSize)
        {
            if(isDynamicSize)
                _canvasSize = newSize;
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
            _lastCanvasSetSize = _canvasSize;
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
