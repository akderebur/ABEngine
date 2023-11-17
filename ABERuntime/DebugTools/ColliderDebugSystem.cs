using System;
using System.Collections.Generic;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using Arch.Core;
using Arch.Core.Extensions;
using Box2D.NetStandard.Dynamics.World;
using WGIL;
using WGIL.IO;
using Buffer = WGIL.Buffer;

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
        Buffer linePointsBuffer;

        Vector4 color = new Vector4(0f, 1f, 0f, 1f);

        public ColliderDebugSystem(PipelineAsset asset) : base(asset) { }

        public override void Awake()
        {
            base.Awake();
        }

        public override void Start()
        {
            base.Start();

            linePointsBuffer = wgil.CreateBuffer(linePointCount * (int)LinePoint.VertexSize, BufferUsages.VERTEX | BufferUsages.COPY_DST);

            LinePoint[] vertices = new LinePoint[linePointCount];
            for (int i = 0; i < linePointCount; i++)
            {
                vertices[i] = new LinePoint(color, Vector3.Zero);
            }

            wgil.WriteBuffer(linePointsBuffer, vertices);
        }

        public override void Update(float gameTime, float deltaTime)
        {
            base.Update(gameTime, deltaTime);

            if(Input.GetMouseButtonDown(MouseButton.Left))
            {
                bool hit = false;
                clickPos = Input.MousePosition;
                lastPos = clickPos;

                var query = new QueryDescription().WithAll<Transform>().WithAny<AABB, CircleCollider>();
                Game.GameWorld.Query(in query, (in Entity ent, ref Transform transform) =>
                {
                    if (ent.Has<AABB>())
                    {
                        var bbox = ent.Get<AABB>();
                        if (bbox.CheckCollisionMouse(transform, Input.MousePosition))
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
                        if (circleCol.CheckCollisionMouse(transform, Input.MousePosition))
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
                clickPos = Input.MousePosition;
                lastPos = clickPos;

            }
            else if(lastTrans != null)
            {
                if(lastTrans.entity == Entity.Null || !lastTrans.entity.IsAlive())
                {
                    LinePoint[] vertices = new LinePoint[linePointCount];
                    for (int i = 0; i < linePointCount; i++)
                    {
                        vertices[i] = new LinePoint(color, Vector3.Zero);
                    }

                    wgil.WriteBuffer(linePointsBuffer, vertices);
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
                            Vector2 curPos = Input.MousePosition;
                            Vector2 delta = (lastPos - curPos) / lastTrans.worldScale.ToVector2() / 100f;

                            bbox.size -= delta;
                            selectedSize = bbox.size;

                            lastPos = curPos;
                        }
                        if (Input.GetMouseButton(MouseButton.Right))
                        {
                            Vector2 curPos = Input.MousePosition;
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
                            Vector2 curPos = Input.MousePosition;
                            Vector2 delta = (lastPos - curPos) / lastTrans.worldScale.ToVector2() / 100f;

                            circleCol.radius -= delta.X;
                            selectedRadius = circleCol.radius;

                            lastPos = curPos;
                        }
                        if (Input.GetMouseButton(MouseButton.Right))
                        {
                            Vector2 curPos = Input.MousePosition;
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
            Vector4 bboxPoints = bbox.GetMinMax(transform);

            LinePoint[] writemap = new LinePoint[5];
            writemap[0] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));
            writemap[1] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.W, 0));
            writemap[2] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.W, 0));
            writemap[3] = new LinePoint(color, new Vector3(bboxPoints.Y, bboxPoints.Z, 0));
            writemap[4] = new LinePoint(color, new Vector3(bboxPoints.X, bboxPoints.Z, 0));

            wgil.WriteBuffer(linePointsBuffer, writemap, 0, (int)LinePoint.VertexSize * 5);
        }

        void SetupCircleBuffer(CircleCollider circleCol, Transform transform)
        {
            LinePoint[] writemap = new LinePoint[linePointCount];
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

            wgil.WriteBuffer(linePointsBuffer, writemap);
        }

        public override void Render(RenderPass pass)
        {
            if (lastTrans == null)
                return;

            pipelineAsset.BindPipeline(pass);

            pass.SetVertexBuffer(0, linePointsBuffer);

            pass.Draw(drawCount);
        }

        public override void CleanUp(bool reload, bool newScene, bool resize)
        {
            linePointsBuffer.Dispose();
        }
    }
}

