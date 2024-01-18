using System;
using System.Numerics;
using Arch.Core;

namespace ABEngine.ABERuntime.Components
{
	public class CircleCollider : ABComponent, ICollider
	{
		public Vector2 center { get; set; }
		public float radius { get; set; }

        public bool sizeSet { get; set; }

        public CircleCollider()
        {
            radius = 1f;
        }

        public CircleCollider(float radius)
        {
            this.radius = radius;
            sizeSet = true;
        }


        public Box2D.NetStandard.Collision.AABB ToB2D(Transform transform)
        {
            Vector3 centerOff = new Vector3(center, 0f) * transform.worldScale + transform.worldPosition;

            float extentX = radius * transform.worldScale.X;
            float extentY = radius * transform.worldScale.Y;

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

            Vector3 centerWS = new Vector3(center, 0f) * transform.worldScale + transform.worldPosition;
            float radiusWS = radius * transform.worldScale.X;

            return Vector2.Distance(mouseWP.ToVector2(), centerWS.ToVector2()) <= radiusWS;
        }
    }
}

