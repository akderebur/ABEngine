using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Veldrid;

namespace ABEngine.ABERuntime.Debug
{
    public struct LinePoint
    {
        public const uint VertexSize = 28;

        public Vector4 Color;
        public Vector3 Position;

        public LinePoint(Vector4 color, Vector3 position)
        {
            Color = color;
            Position = position;
        }
    }

    public class ColliderDebugSystem : RenderSystem
	{
        Transform lastTrans = null;
        Vector4 lastPoints = Vector4.Zero;
        Vector2 clickPos = Vector2.Zero;
        Vector2 lastPos = Vector2.Zero;
        public static Vector2 selectedSize;
        public static Vector2 selectedCenter;

        LinePoint[] linePoints = new LinePoint[5];
        DeviceBuffer linePointsBuffer;

        Vector4 color = RgbaFloat.Green.ToVector4();

        public ColliderDebugSystem(PipelineAsset asset) : base(asset) { }

        public override void Awake()
        {
            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            linePointsBuffer = rf.CreateBuffer(new BufferDescription((uint)(linePoints.Length * LinePoint.VertexSize), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        public override void Update(float gameTime, float deltaTime)
        {
            base.Update(gameTime, deltaTime);


            if(Input.GetMouseButtonDown(MouseButton.Left))
            {
                clickPos = Input.GetMousePosition();
                lastPos = clickPos;

                var query = Game.GameWorld.CreateQuery().Has<AABB>().Has<Transform>();

                foreach (var ent in query.GetEntities())
                {
                    Transform bboxTrans = ent.Get<Transform>();
                    AABB bbox = ent.Get<AABB>();

                    if (bbox.CheckCollisionMouse(bboxTrans, Input.GetMousePosition()))
                    {
                        MappedResourceView<LinePoint> writemap = gd.Map<LinePoint>(linePointsBuffer, MapMode.Write);

                        Vector4 bboxPoints = bbox.GetMinMax(bboxTrans);
                        writemap[0] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));
                        writemap[1] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.W, 0));
                        writemap[2] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.W, 0));
                        writemap[3] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.Z, 0));
                        writemap[4] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));

                        gd.Unmap(linePointsBuffer);

                        lastTrans = bboxTrans;
                        lastPoints = bboxPoints;

                        selectedSize = bbox.size;
                        selectedCenter = bbox.center;

                        break;
                    }
                }
            }
            else if(Input.GetMouseButtonDown(MouseButton.Right))
            {
                clickPos = Input.GetMousePosition();
                lastPos = clickPos;

            }
            else if(lastTrans != null)
            {
                if (lastTrans.entity.IsValid() && lastTrans.entity.Has<AABB>())
                {
                    AABB bbox = lastTrans.entity.Get<AABB>();

                    MappedResourceView<LinePoint> writemap = gd.Map<LinePoint>(linePointsBuffer, MapMode.Write);

                    Vector4 bboxPoints = bbox.GetMinMax(lastTrans);
                    writemap[0] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));
                    writemap[1] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.W, 0));
                    writemap[2] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.W, 0));
                    writemap[3] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.Z, 0));
                    writemap[4] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));

                    gd.Unmap(linePointsBuffer);


                    if (Input.GetKey(Key.ControlLeft))
                    {
                        if (Input.GetMouseButton(MouseButton.Left))
                        {
                            Vector2 curPos = Input.GetMousePosition();
                            Vector2 delta = (lastPos - curPos) / lastTrans.worldScale.ToVector2() / 100f;

                            bbox.size -= delta;
                            selectedSize = bbox.size;

                            lastPos = curPos;
                        }
                        if (Input.GetMouseButton(MouseButton.Right))
                        {
                            Vector2 curPos = Input.GetMousePosition();
                            Vector2 delta = (lastPos - curPos) / lastTrans.worldScale.ToVector2() / 100f;

                            bbox.center -= delta;
                            selectedCenter = bbox.center;

                            lastPos = curPos;
                        }

                    }

                }

               
            } 

        }

        public override void Render()
        {
            // Light pass
            pipelineAsset.BindPipeline();

            cl.SetVertexBuffer(0, linePointsBuffer);

            cl.Draw(5, 1, 0, 0);

        }
    }
}

