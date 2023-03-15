using System;
using System.Numerics;
using Box2D.NetStandard.Collision.Shapes;
using Box2D.NetStandard.Dynamics.Bodies;
using Box2D.NetStandard.Dynamics.Fixtures;
using ABEngine.ABERuntime.ECS;
using ABEngine.ABERuntime.Physics;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABERuntime
{
    public class B2DInitSystem : BaseSystem
    {
        public B2DInitSystem()
        {
        }

        private void CreateBody(ref Entity rbEnt)
        {
            Rigidbody rb = rbEnt.Get<Rigidbody>();
            Transform rbTrans = rbEnt.Get<Transform>();

            BodyDef bodyDef = new BodyDef();
            bodyDef.type = rb.bodyType;
            bodyDef.position = rbTrans.worldPosition.ToB2DVector();
            bodyDef.angle = rbTrans.localEulerAngles.Z * MathF.PI * 2f / 360f;
            bodyDef.linearDamping = rb.linearDamping;
            bodyDef.fixedRotation = true;

            var b2dBody = Game.B2DWorld.CreateBody(bodyDef);
            FixtureDef fixtureDef = new FixtureDef();

            Shape boxShape = null;

            if(rbEnt.Has<PolygonCollider>())
            {
                var points = rbEnt.Get<PolygonCollider>().GetPoints();
                //points.Add(points[0]);
                
                boxShape = new ChainShape();
                ((ChainShape)boxShape).CreateLoop(points.ToArray());

                fixtureDef.isSensor = rb.isTrigger;

            }
            else if (rbEnt.Has<AABB>())
            {
                AABB bbox = rbEnt.Get<AABB>();
                float width = bbox.size.X  * rbTrans.worldScale.X;
                float height = bbox.size.Y  * rbTrans.worldScale.Y;
                Vector2 center = (bbox.center * new Vector2(rbTrans.worldScale.X, rbTrans.worldScale.Y)).ToB2DVector();

                float extentX = width / 2f;
                float extentY = height / 2f;

                boxShape = new PolygonShape();
                Vector2[] vs = new Vector2[4];
                vs[0] = new Vector2(center.X - extentX, center.Y - extentY);
                vs[1] = new Vector2(center.X + extentX, center.Y - extentY);
                vs[2] = new Vector2(center.X + extentX, center.Y + extentY);
                vs[3] = new Vector2(center.X - extentX, center.Y + extentY);
                ((PolygonShape)boxShape).Set(vs);

                fixtureDef.isSensor = rb.isTrigger;
            }


            if (boxShape != null)
                fixtureDef.shape = boxShape;

            fixtureDef.density = rb.density;
            fixtureDef.friction = rb.friction;
            fixtureDef.restitution = 0f;

            b2dBody.CreateFixture(fixtureDef);

            b2dBody.SetUserData(rb);

            MassData mass = new MassData();
            b2dBody.GetMassData(out mass);
            mass.mass = rb.mass;
            b2dBody.SetMassData(mass);

            if (!rb.entity.enabled)
                b2dBody.SetEnabled(false);

            rb.b2dBody = b2dBody;
        }

        public override void Start()
        {
            var rbQuery = Game.GameWorld.CreateQuery().Has<Rigidbody>();
            var rbEnts = rbQuery.GetEntities();
            for (int i = 0; i < rbEnts.Length; i++)
            {
                Entity rbEnt = rbEnts[i];
                CreateBody(ref rbEnt);
            }

            base.Start();
        }

        internal void AddRBRuntime(ref Entity entity)
        {
            if (!started)
                return;

            CreateBody(ref entity);
        }


    }
}
