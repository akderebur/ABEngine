using System;
using System.Numerics;
using Friflo.Engine.ECS;

namespace ABEngine.ABERuntime.Components
{
    public struct PointLight2D : IComponent
    {
        public Vector4 color { get; set; }
        public float radius { get; set; }
        public float intensity { get; set; }
        public uint renderLayerIndex { get; set; }
        public float volume { get; set; }

        public PointLight2D()
        {
            color = new Vector4(1f, 1f, 1f, 1f);
            radius = 1f;
            intensity = 1f;
            volume = 0f;
            renderLayerIndex = 0;
        }
    }

    public struct LightInfo
    {
        public const int VertexSize = 52;

        public Vector3 Position;
        public Vector4 Color;
        public Vector4 SizeIntVol;
        public Vector2 LayerType;
        
        public LightInfo(Vector3 position, Vector4 color, Vector4 sizeIntVol, float layer, float global = 0)
        {
            Position = position;
            Color = color;
            SizeIntVol = sizeIntVol;
            LayerType = new Vector2(layer, global);
        }
    }
}

