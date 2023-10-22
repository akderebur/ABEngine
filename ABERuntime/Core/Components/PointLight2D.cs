using System;
using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Veldrid;

namespace ABEngine.ABERuntime.Components
{
    public class PointLight2D : ABComponent
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
        }
    }

    public struct LightInfo
    {
        public const uint VertexSize = 48;

        public Vector3 Position;
        public Vector4 Color;
        public float Radius;
        public float Intensity;
        public float Volume;
        public float Layer;
        public float Global;

        //public LightInfo(Vector4 color, float radius, float intensity) : this(Vector3.Zero, color, radius, intensity) { }

        public LightInfo(Vector3 position, Vector4 color, float radius, float intensity, float volume, float layer, float global = 0)
        {
            Position = position;
            Radius = radius;
            Intensity = intensity;
            Color = color;
            Volume = volume;
            Layer = layer;
            Global = global;
        }
    }
}

