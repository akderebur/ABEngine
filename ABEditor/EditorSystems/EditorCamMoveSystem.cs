using System;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using Arch.Core.Extensions;
using ImGuiNET;
using ABEngine.ABERuntime.Core.MathA;
using WGIL.IO;

namespace ABEngine.ABEditor
{
    public class EditorCamMoveSystem : BaseSystem
    {
        public static float camMoveSpeed = 1f;

        bool dragging = false;
        Vector2 lastPos = Vector2.Zero;

        public override void Update(float gameTime, float deltaTime)
        {
            if (Game.activeCamTrans == null)
                return;

            bool imguiCapture = ImGui.GetIO().WantCaptureMouse || ImGui.GetIO().WantCaptureKeyboard;

            if (Input.GetMouseButtonDown(MouseButton.Right))
            {
                lastPos = Input.GetMousePosition();
                dragging = true;
            }
            else if (Input.GetMouseButtonUp(MouseButton.Right))
            {
                dragging = false;
            }

            if (Input.HasInput() && !imguiCapture)
            {
                Transform transform = Game.activeCamTrans;
                Vector3 endPos = transform.localPosition;
                Camera cam = transform.entity.Get<Camera>();

                bool modKey = Input.GetKey(Key.ShiftLeft) || Input.GetKey(Key.AltLeft) || Input.GetKey(Key.ControlLeft);

                if (cam.cameraProjection == CameraProjection.Orthographic)
                {
                    if (Input.GetKey(Key.KeyD))
                    {
                        if (!modKey)
                            endPos += new Vector3(Vector2.UnitX, 0f);
                    }

                    if (Input.GetKey(Key.KeyA))
                    {
                        if (!modKey)
                            endPos -= new Vector3(Vector2.UnitX, 0f);
                    }

                    if (Input.GetKey(Key.KeyW))
                    {
                        if (!modKey)
                            endPos += new Vector3(Vector2.UnitY, 0f);
                    }

                    if (Input.GetKey(Key.KeyS))
                    {
                        if (!modKey)
                            endPos -= new Vector3(Vector2.UnitY, 0f);
                    }
                }
                else
                {
                    // Move
                    Vector3 forward = Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, transform.localRotation));
                    Vector3 up = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, transform.localRotation));
                    Vector3 right = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, transform.localRotation));

                    endPos += (forward * Input.YAxis + right * Input.XAxis).Normalize();

                    // Rotate
                    if (dragging)
                    {
                        Vector2 mouseDelta = Input.GetMousePosition() - lastPos;
                        lastPos = Input.GetMousePosition();
                        
                        if (mouseDelta.Length() > 0f)
                        {
                            Vector3 euler = transform.localEulerAngles;
                            Vector2 pixelDelta = mouseDelta;
                            mouseDelta = Vector2.Normalize(mouseDelta);
                            if (MathF.Abs(pixelDelta.X) > 2)
                                euler.Y += -mouseDelta.X * deltaTime;
                            if (MathF.Abs(pixelDelta.Y) > 2)
                                euler.X += mouseDelta.Y * deltaTime;
                            transform.localEulerAngles = euler;
                        }

                    }
                }

                Vector3 newPos = Vector3.Lerp(transform.localPosition, endPos, deltaTime * camMoveSpeed);
                //newPos.Z = 0f;
                transform.localPosition = newPos;
            }

        }
    }
}
