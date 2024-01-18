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
    public class AABB : ABComponent, ICollider
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

        public Box2D.NetStandard.Collision.AABB ToB2D(Transform transform)
        {
            Vector3 centerOff = new Vector3(center, 0f) * transform.worldScale + transform.worldPosition;

            float extentX = size.X / 2f * transform.worldScale.X;
            float extentY = size.Y / 2f * transform.worldScale.Y;

            Vector2 lower = new Vector2(centerOff.X - extentX, centerOff.Y - extentY);
            Vector2 upper = new Vector2(centerOff.X + extentX, centerOff.Y + extentY);

            return new Box2D.NetStandard.Collision.AABB(lower, upper);
        }

        public bool CheckCollisionMouse(Transform transform, Vector2 mousePos)
        {
            if (Game.activeCamTrans == null)
                return false;

            var camEnt = Game.activeCamTrans.entity;
            if (camEnt == Entity.Null)
                return false;

            Vector3 mouseWP = mousePos.ScreenToWorld();

            Vector3 centerWP = new Vector3(center, 0f) * transform.worldScale + transform.worldPosition;

            float extentX = size.X / 2f * transform.worldScale.X;
            float extentY = size.Y / 2f * transform.worldScale.Y;

            bool isClicked = mouseWP.X > centerWP.X - extentX &&
                             mouseWP.X < centerWP.X + extentX &&
                             mouseWP.Y > centerWP.Y - extentY &&
                             mouseWP.Y < centerWP.Y + extentY;


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
