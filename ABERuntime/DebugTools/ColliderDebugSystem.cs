using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Arch.Core;
using Arch.Core.Extensions;
using Box2D.NetStandard.Dynamics.World;
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
        public static Transform lastTrans = null;
        Vector2 clickPos = Vector2.Zero;
        Vector2 lastPos = Vector2.Zero;
        public static Vector2 selectedSize;
        public static Vector2 selectedCenter;
        public static float selectedRadius;

        int linePointCount = 73;
        int drawCount = 5;
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

            linePointsBuffer = rf.CreateBuffer(new BufferDescription((uint)(linePointCount * LinePoint.VertexSize), BufferUsage.VertexBuffer | BufferUsage.Dynamic));

            MappedResourceView<LinePoint> writemap = gd.Map<LinePoint>(linePointsBuffer, MapMode.Write);
            for (int i = 0; i < linePointCount; i++)
            {
                writemap[i] = new LinePoint(color, Vector3.Zero);
            }
            gd.Unmap(linePointsBuffer);
        }

        public override void Update(float gameTime, float deltaTime)
        {
            base.Update(gameTime, deltaTime);

            if(Input.GetMouseButtonDown(MouseButton.Left))
            {
                bool hit = false;
                clickPos = Input.GetMousePosition();
                lastPos = clickPos;

                var query = new QueryDescription().WithAll<Transform>().WithAny<AABB, CircleCollider>();
                Game.GameWorld.Query(in query, (in Entity ent, ref Transform transform) =>
                {
                    if (ent.Has<AABB>())
                    {
                        var bbox = ent.Get<AABB>();
                        if (bbox.CheckCollisionMouse(transform, Input.GetMousePosition()))
                        {
                            SetupAABBBuffer(bbox, transform);

                            lastTrans = transform;

                            selectedSize = bbox.size;
                            selectedCenter = bbox.center;
                            drawCount = 5;

                            hit = true;

                            return;
                        }
                    }
                    else if(ent.Has<CircleCollider>())
                    {
                        var circleCol = ent.Get<CircleCollider>();
                        if (circleCol.CheckCollisionMouse(transform, Input.GetMousePosition()))
                        {
                            SetupCircleBuffer(circleCol, transform);

                            lastTrans = transform;

                            selectedRadius = circleCol.radius;
                            selectedCenter = circleCol.center;
                            drawCount = linePointCount;

                            hit = true;

                            return;
                        }
                    }
                });

                if (!hit)
                    lastTrans = null;
            }
            else if(Input.GetMouseButtonDown(MouseButton.Right))
            {
                clickPos = Input.GetMousePosition();
                lastPos = clickPos;

            }
            else if(lastTrans != null)
            {
                if(lastTrans.entity == Entity.Null || !lastTrans.entity.IsAlive())
                {
                    MappedResourceView<LinePoint> writemap = gd.Map<LinePoint>(linePointsBuffer, MapMode.Write);
                    for (int i = 0; i < linePointCount; i++)
                    {
                        writemap[i] = new LinePoint(color, Vector3.Zero);
                    }
                    gd.Unmap(linePointsBuffer);
                    lastTrans = null;
                    return;
                }

                if (lastTrans.entity.Has<AABB>())
                {
                    // Draw last selected AABB
                    AABB bbox = lastTrans.entity.Get<AABB>();

                    SetupAABBBuffer(bbox, lastTrans);
                    drawCount = 5;

                    // Control center and size
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
                else if(lastTrans.entity.Has<CircleCollider>())
                {
                    // Draw last selected circle collider
                    CircleCollider circleCol = lastTrans.entity.Get<CircleCollider>();

                    SetupCircleBuffer(circleCol, lastTrans);
                    drawCount = linePointCount;

                    // Control center and size
                    if (Input.GetKey(Key.ControlLeft))
                    {
                        if (Input.GetMouseButton(MouseButton.Left))
                        {
                            Vector2 curPos = Input.GetMousePosition();
                            Vector2 delta = (lastPos - curPos) / lastTrans.worldScale.ToVector2() / 100f;

                            circleCol.radius -= delta.X;
                            selectedRadius = circleCol.radius;

                            lastPos = curPos;
                        }
                        if (Input.GetMouseButton(MouseButton.Right))
                        {
                            Vector2 curPos = Input.GetMousePosition();
                            Vector2 delta = (lastPos - curPos) / lastTrans.worldScale.ToVector2() / 100f;

                            circleCol.center -= delta;
                            selectedCenter = circleCol.center;

                            lastPos = curPos;
                        }
                    }
                }
            } 
        }

        void SetupAABBBuffer(AABB bbox, Transform transform)
        {
            MappedResourceView<LinePoint> writemap = gd.Map<LinePoint>(linePointsBuffer, MapMode.Write);

            Vector4 bboxPoints = bbox.GetMinMax(transform);
            writemap[0] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));
            writemap[1] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.W, 0));
            writemap[2] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.W, 0));
            writemap[3] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.Z, 0));
            writemap[4] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));

            gd.Unmap(linePointsBuffer);
        }

        void SetupCircleBuffer(CircleCollider circleCol, Transform transform)
        {
            MappedResourceView<LinePoint> writemap = gd.Map<LinePoint>(linePointsBuffer, MapMode.Write);

            float step = MathF.PI * 2f / (linePointCount - 1);

            Vector3 centerOff = new Vector3(circleCol.center, 0f) * transform.worldScale;
            Vector3 centerWS = transform.worldPosition + centerOff;
            centerWS.Z = 0f;

            float radiusWS = circleCol.radius * transform.worldScale.X;

            for (int i = 0; i < linePointCount; i++)
            {
                int index = i;
                if (i == linePointCount - 1)
                    index = 0;

                float angle = step * index;
                float x = MathF.Cos(angle);
                float y = MathF.Sin(angle);

                writemap[i] = new LinePoint(color, centerWS + new Vector3(x, y, 0) * radiusWS);
            }

            gd.Unmap(linePointsBuffer);
        }

        public override void Render()
        {
            if (lastTrans == null)
                return;

            // Light pass
            pipelineAsset.BindPipeline();

            cl.SetVertexBuffer(0, linePointsBuffer);

            cl.Draw((uint)drawCount, 1, 0, 0);

        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            linePointsBuffer.Dispose();
        }
    }
}

