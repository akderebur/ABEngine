using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Debug;
using Arch.Core;
using Arch.Core.Extensions;
using ImGuiNET;
using WGIL.IO;

namespace ABEngine.ABEditor
{
    public class MouseDragSystem : BaseSystem
    {
        Transform selectedTransform;
        Vector3 dragDelta;

        private struct Ray
        {
            public Vector3 Origin;
            public Vector3 Direction;
        }

        private Ray GenerateRay(float mouse_x, float mouse_y, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, float width, float height)
        {
            // Transform to normalized device coordinates
            float x = (2.0f * mouse_x) / width - 1.0f;
            float y = 1.0f - (2.0f * mouse_y) / height;

            // Transform to world coordinates
            Matrix4x4 invProj = Matrix4x4.Identity;
            Matrix4x4 invView = Matrix4x4.Identity;

            Matrix4x4.Invert(projectionMatrix, out invProj);
            Matrix4x4.Invert(viewMatrix, out invView);

            Vector3 ray_NDC = new Vector3(x, y, -1.0f); // point on the near plane
            Vector4 ray_clip = new Vector4(ray_NDC.X, ray_NDC.Y, -1.0f, 1.0f);

            // Convert to eye space
            Vector4 ray_eye = Vector4.Transform(ray_clip, invProj);
            ray_eye = new Vector4(ray_eye.X, ray_eye.Y, -1.0f, 0.0f); // Resetting z and setting w to 0

            // Convert to world space
            Vector4 ray_world_4D = Vector4.Transform(ray_eye, invView);
            Vector3 ray_world = new Vector3(ray_world_4D.X, ray_world_4D.Y, ray_world_4D.Z);

            Ray ray;
            ray.Origin = Game.activeCam.worldPosition;

            ray.Direction = Vector3.Normalize(ray_world);


            return ray;
        }

        private bool Intersects(Ray ray, Matrix4x4 modelMatrix, Vector3 Min, Vector3 Max, out float distance)
        {
            //Matrix4x4 inverseTransform = Matrix4x4.Identity;
            //Matrix4x4.Invert(modelMatrix, out inverseTransform);
            //Vector3 transformedOrigin = Vector3.Transform(ray.Origin, inverseTransform);
            //Vector3 transformedDirection = Vector3.Normalize(Vector3.TransformNormal(ray.Direction, inverseTransform));

            //ray.Origin = transformedOrigin;
            //ray.Direction = transformedDirection;

            Min = Vector3.Transform(Min, modelMatrix);
            Max = Vector3.Transform(Max, modelMatrix);


            distance = 0;
            float tMin = float.MinValue;
            float tMax = float.MaxValue;

            // Check for X-axis
            if (Math.Abs(ray.Direction.X) < 1e-5f)
            {
                if (ray.Origin.X < Min.X || ray.Origin.X > Max.X)
                {
                    return false;
                }
            }
            else
            {
                float ood = 1.0f / ray.Direction.X;
                float t1 = (Min.X - ray.Origin.X) * ood;
                float t2 = (Max.X - ray.Origin.X) * ood;

                tMin = Math.Max(tMin, Math.Min(t1, t2));
                tMax = Math.Min(tMax, Math.Max(t1, t2));

                if (tMin > tMax)
                {
                    return false;
                }
            }

            // Check for Y-axis
            if (Math.Abs(ray.Direction.Y) < 1e-5f)
            {
                if (ray.Origin.Y < Min.Y || ray.Origin.Y > Max.Y)
                {
                    return false;
                }
            }
            else
            {
                float ood = 1.0f / ray.Direction.Y;
                float t1 = (Min.Y - ray.Origin.Y) * ood;
                float t2 = (Max.Y - ray.Origin.Y) * ood;

                tMin = Math.Max(tMin, Math.Min(t1, t2));
                tMax = Math.Min(tMax, Math.Max(t1, t2));

                if (tMin > tMax)
                {
                    return false;
                }
            }

            // Check for Z-axis
            if (Math.Abs(ray.Direction.Z) < 1e-5f)
            {
                if (ray.Origin.Z < Min.Z || ray.Origin.Z > Max.Z)
                {
                    return false;
                }
            }
            else
            {
                float ood = 1.0f / ray.Direction.Z;
                float t1 = (Min.Z - ray.Origin.Z) * ood;
                float t2 = (Max.Z - ray.Origin.Z) * ood;

                tMin = Math.Max(tMin, Math.Min(t1, t2));
                tMax = Math.Min(tMax, Math.Max(t1, t2));

                if (tMin > tMax)
                {
                    return false;
                }
            }

            distance = tMin;
            return true;
        }


        public override void Update(float gameTime, float deltaTime)
        {
            if (Game.activeCam == null)
                return;

            bool imguiCapture = ImGui.GetIO().WantCaptureMouse || ImGui.GetIO().WantCaptureKeyboard;
            if (imguiCapture)
                return;

            Camera cam = Game.activeCam.entity.Get<Camera>();
            if (cam.cameraProjection == CameraProjection.Orthographic)
            {

                //if (!Input.GetKey(Veldrid.Key.ControlLeft) && Input.GetMouseButtonDown(Veldrid.MouseButton.Left))
                //{
                //    var query = new QueryDescription().WithAll<Transform>().WithAny<AABB, CircleCollider>();

                //    Game.GameWorld.Query(in query, (in Entity entity, ref AABB bbox, ref Transform transform) =>
                //    {
                //        if (bbox.CheckCollisionMouse(transform, Input.MousePosition))
                //        {
                //            selectedTransform = transform;
                //            dragDelta = selectedTransform.worldPosition - Input.MousePosition.ScreenToWorld();
                //            Editor.selectedEntity = entity;

                //            return; ;
                //        }
                //    });
                //}
                //else


                if(!Input.GetKey(Key.ControlLeft) && Input.GetMouseButtonDown(MouseButton.Left))
                {
                    if(selectedTransform != ColliderDebugSystem.lastTrans)
                    {
                        selectedTransform = ColliderDebugSystem.lastTrans;
                        dragDelta = selectedTransform.worldPosition - Input.GetMousePosition().ScreenToWorld();
                        Editor.selectedEntity = selectedTransform.entity;
                    }
                }
                if (Input.GetMouseButton(MouseButton.Left))
                {
                    if (selectedTransform != null)
                    {
                        selectedTransform.localPosition = Input.GetMousePosition().ScreenToWorld() + dragDelta;
                    }
                }
                else
                    selectedTransform = null;
            }
            else
            {
                // Perspective
                if (!Input.GetKey(Key.ControlLeft) && Input.GetMouseButtonDown(MouseButton.Left))
                {
                    Vector2 mousePos = Input.MousePosition;
                    Ray ray = GenerateRay(mousePos.X, mousePos.Y, Game.pipelineData.View, Game.pipelineData.Projection, Game.virtualSize.X, Game.virtualSize.Y);

                    var query = new QueryDescription().WithAll<MeshRenderer>();

                    SortedDictionary<float, Transform> hits = new SortedDictionary<float, Transform>();

                    Game.GameWorld.Query(in query, (in Entity entity, ref MeshRenderer mr, ref Transform transform) =>
                    {
                        if (selectedTransform == transform)
                            return;

                        if(mr != null && mr.mesh != null)
                        {
                            Mesh mesh = mr.mesh;

                            if(Intersects(ray, transform.worldMatrix, mesh.boundsMin, mesh.boundsMax, out float distance))
                            {
                               

                                if (hits.ContainsKey(distance))
                                    return;

                                hits.Add(distance, transform);
                            }
                        }
                    });

                    if(hits.Count > 0)
                    {
                        Transform selTrans = hits.First().Value;
                        selectedTransform = selTrans;
                        Editor.selectedEntity = selTrans.entity;
                    }
                }
            }
        }
    }
}
