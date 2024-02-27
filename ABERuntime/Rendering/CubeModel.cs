using System;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABERuntime.Rendering
{
    public static class CubeModel
    {
        private static Mesh _cubeMesh = null;

        public static Mesh GetCubeMesh()
        {
            if (_cubeMesh == null)
            {
                _cubeMesh = new Mesh();
                _cubeMesh.CreateFromStandard(Vertices, Indices);
            }

            return _cubeMesh;
        }

        public static readonly VertexStandard[] Vertices = new VertexStandard[]
        {
            // Top
            new VertexStandard(new Vector3(-.5f,.5f,-.5f),     new Vector3(0,1,0),     new Vector2(0, 0)),
            new VertexStandard(new Vector3(.5f,.5f,-.5f),      new Vector3(0,1,0),     new Vector2(1, 0)),
            new VertexStandard(new Vector3(.5f,.5f,.5f),       new Vector3(0,1,0),     new Vector2(1, 1)),
            new VertexStandard(new Vector3(-.5f,.5f,.5f),      new Vector3(0,1,0),     new Vector2(0, 1)),
            // Bottom                                                             
            new VertexStandard(new Vector3(-.5f,-.5f,.5f),     new Vector3(0,-1,0),     new Vector2(0, 0)),
            new VertexStandard(new Vector3(.5f,-.5f,.5f),      new Vector3(0,-1,0),     new Vector2(1, 0)),
            new VertexStandard(new Vector3(.5f,-.5f,-.5f),     new Vector3(0,-1,0),     new Vector2(1, 1)),
            new VertexStandard(new Vector3(-.5f,-.5f,-.5f),    new Vector3(0,-1,0),     new Vector2(0, 1)),
            // Left                                                               
            new VertexStandard(new Vector3(-.5f,.5f,-.5f),     new Vector3(-1,0,0),    new Vector2(0, 0)),
            new VertexStandard(new Vector3(-.5f,.5f,.5f),      new Vector3(-1,0,0),    new Vector2(1, 0)),
            new VertexStandard(new Vector3(-.5f,-.5f,.5f),     new Vector3(-1,0,0),    new Vector2(1, 1)),
            new VertexStandard(new Vector3(-.5f,-.5f,-.5f),    new Vector3(-1,0,0),    new Vector2(0, 1)),
            // Right                                                              
            new VertexStandard(new Vector3(.5f,.5f,.5f),       new Vector3(1,0,0),     new Vector2(0, 0)),
            new VertexStandard(new Vector3(.5f,.5f,-.5f),      new Vector3(1,0,0),     new Vector2(1, 0)),
            new VertexStandard(new Vector3(.5f,-.5f,-.5f),     new Vector3(1,0,0),     new Vector2(1, 1)),
            new VertexStandard(new Vector3(.5f,-.5f,.5f),      new Vector3(1,0,0),     new Vector2(0, 1)),
            // Back                                                               
            new VertexStandard(new Vector3(.5f,.5f,-.5f),      new Vector3(0,0,-1),    new Vector2(0, 0)),
            new VertexStandard(new Vector3(-.5f,.5f,-.5f),     new Vector3(0,0,-1),    new Vector2(1, 0)),
            new VertexStandard(new Vector3(-.5f,-.5f,-.5f),    new Vector3(0,0,-1),    new Vector2(1, 1)),
            new VertexStandard(new Vector3(.5f,-.5f,-.5f),     new Vector3(0,0,-1),    new Vector2(0, 1)),
            // Front                                                              
            new VertexStandard(new Vector3(-.5f,.5f,.5f),      new Vector3(0,0,1),     new Vector2(0, 0)),
            new VertexStandard(new Vector3(.5f,.5f,.5f),       new Vector3(0,0,1),     new Vector2(1, 0)),
            new VertexStandard(new Vector3(.5f,-.5f,.5f),      new Vector3(0,0,1),     new Vector2(1, 1)),
            new VertexStandard(new Vector3(-.5f,-.5f,.5f),     new Vector3(0,0,1),     new Vector2(0, 1)),
        };

        public static readonly ushort[] Indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };

        public static readonly VertexStandard[] TopVertices = new VertexStandard[]
        {
            // Top
            new VertexStandard(new Vector3(-.5f,.5f,-.5f),     new Vector3(0,1,0),     new Vector2(0, 0)),
            new VertexStandard(new Vector3(.5f,.5f,-.5f),      new Vector3(0,1,0),     new Vector2(1, 0)),
            new VertexStandard(new Vector3(.5f,.5f,.5f),       new Vector3(0,1,0),     new Vector2(1, 1)),
            new VertexStandard(new Vector3(-.5f,.5f,.5f),      new Vector3(0,1,0),     new Vector2(0, 1)),
          
        };

        public static readonly ushort[] TopIndices = new ushort[]
       {
            0,1,2, 0,2,3,
       };
    }
}

