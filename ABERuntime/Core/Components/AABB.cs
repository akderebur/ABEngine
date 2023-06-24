using System;
using System.Linq;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Box2D.NetStandard.Common;
using Halak;
using Newtonsoft.Json;

namespace ABEngine.ABERuntime.Components
{
    public class AABB : ABComponent
    {
        public Vector2 size { get; set; }
        public Vector2 center { get; set; }

        public bool sizeSet { get; set; }

        public AABB(float width, float height)
        {
            size = new Vector2(width, height);
            sizeSet = true;
        }

        public AABB()
        {
            size = new Vector2(1f, 1f);
        }

        public bool CheckCollisionMouse(Transform transform, Vector2 mousePos)
        {
            if (Game.activeCam == null)
                return false;

            var camEnt = Game.activeCam.entity;
            if (camEnt == Entity.Null)
                return false;

            Matrix4x4 camRelTrans = transform.worldMatrix * Game.activeCam.worldToLocaMatrix;

            Vector3 camRelPos = Vector3.Zero;
            Vector3 camRelSca = Vector3.One;
            Quaternion camRelRot = Quaternion.Identity;

            Matrix4x4.Decompose(camRelTrans, out camRelSca, out camRelRot, out camRelPos);

            Vector2 resScale = Game.screenSize / Game.canvas.canvasSize * 100f;
            camRelPos *= new Vector3(resScale.X, resScale.Y, 1f);

            Vector3 centerOff = new Vector3(center, 0f) * transform.worldScale;
            camRelPos += (centerOff * new Vector3(resScale.X, resScale.Y, 1f));


            float extentX = size.X / 2f * transform.worldScale.X * resScale.X;
            float extentY = size.Y / 2f * transform.worldScale.Y * resScale.Y;

            // Adjust mouse position according to zoom
            Vector2 zoomedMousePos = mousePos.MouseToZoomed();

            bool isClicked = zoomedMousePos.X > camRelPos.X - extentX &&
                             zoomedMousePos.X < camRelPos.X + extentX &&
                             zoomedMousePos.Y > camRelPos.Y - extentY &&
                             zoomedMousePos.Y < camRelPos.Y + extentY;

            //bool isClicked = mousePos.X > camRelPos.X - extentX &&
            //        mousePos.X < camRelPos.X + extentX &&
            //        mousePos.Y > camRelPos.Y - extentY &&
            //        mousePos.Y < camRelPos.Y + extentY;



            return isClicked; 
        }

        public Vector4 GetMinMax(Transform transform)
        {
            Vector3 centerOff = new Vector3(center, 0f) * transform.worldScale;

            float extentX = size.X / 2f * transform.worldScale.X;
            float extentY = size.Y / 2f * transform.worldScale.Y;

            return new Vector4()
            {
                X = transform.worldPosition.X + centerOff.X - extentX, // MinX
                Y = transform.worldPosition.X + centerOff.X + extentX, // MaxX,
                Z = transform.worldPosition.Y + centerOff.Y - extentY,// MinY
                W = transform.worldPosition.Y + centerOff.Y + extentY // MaxY
            };

        }
    }
}
