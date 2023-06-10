using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.ECS;
using Box2D.NetStandard.Common;
using Box2D.NetStandard.Dynamics.Bodies;

namespace ABEngine.ABERuntime
{
    public class RigidbodyMoveSystem : BaseSystem
    {
        public RigidbodyMoveSystem()
        {
        }

        public void ResetSmoothStates()
        {
            var query = Game.GameWorld.CreateQuery().Has<Rigidbody>().Has<Transform>();

            foreach (var rbEnt in query.GetEntities())
            {
                var rb = rbEnt.Get<Rigidbody>();
                if (rb.b2dBody == null || rb.bodyType == BodyType.Static)
                    continue;

                //rb.smoothedPosition = rb.previousPosition = rb.b2dBody.GetPosition();
                rb.start = rb.current = rb.target = rb.b2dBody.GetPosition();
            }
        }

        public void PreFixedUpdate()
        {
            var query = Game.GameWorld.CreateQuery().Has<Rigidbody>().Has<Transform>();

            query.Foreach((Entity rbEnt, ref Rigidbody rb, ref Transform transform) =>
            {
                if (rb.b2dBody != null)
                {

                    if (transform.transformMove)
                        rb.SetPosition(transform.worldPosition);

                    transform.transformMove = false;
                }
            }
            );
        }

        public override void FixedUpdate(float gameTime, float fixedDeltaTime)
        {
            var query = Game.GameWorld.CreateQuery().Has<Rigidbody>().Has<Transform>();

            query.Foreach((Entity rbEnt, ref Rigidbody rb, ref Transform transform) =>
            {
               
                    
                if (rb.b2dBody != null && rb.entity.enabled && rb.bodyType != BodyType.Static)
                {

                    //Vector3 locPos = Vector3.Transform(new Vector3(rb.b2dBody.GetPosition() * 100f, 0f), transform.parent != null ? transform.parent.worldToLocaMatrix : Matrix4x4.Identity);

                    //Vector3 locPos = Vector3.Transform(new Vector3(rb.smoothedPosition * 100f, 0f), transform.parent != null ? transform.parent.worldToLocaMatrix : Matrix4x4.Identity);
                    //transform.localPosition = locPos;

                    //Vector2 pos = rb.b2dBody.GetPosition();
                    //rb.target = pos;
                    //rb.start = rb.current;


                    //Vector3 locPos = Vector3.Transform(new Vector3(rb.b2dBody.GetPosition() * 100f, 0f), transform.parent != null ? transform.parent.worldToLocaMatrix : Matrix4x4.Identity);
                    //transform.localPosition = locPos;

                    if (rb.interpolationType == RBInterpolationType.None)
                    {
                        Vector3 locPos = Vector3.Transform(new Vector3(rb.b2dBody.GetPosition(), transform.localPosition.Z), transform.parent != null ? transform.parent.worldToLocaMatrix : Matrix4x4.Identity);
                        transform.localPosition = locPos;
                    }
                    else
                    {
                        //rb.start = rb.current;
                        rb.target = rb.b2dBody.GetPosition();
                    }

                    transform.transformMove = false;
                }
            }
            );
        }

        public override void Update(float gameTime, float ratio)
        {
            var query = Game.GameWorld.CreateQuery().Has<Rigidbody>().Has<Transform>();

            query.Foreach((Entity rbEnt, ref Rigidbody rb, ref Transform transform) =>
            {
                if (rb.entity.enabled && rb.bodyType != BodyType.Static)
                {
                    if (rb.interpolationType == RBInterpolationType.Interpolate)
                    {
                        bool oldMovestate = transform.transformMove;
                        rb.current = Vector2.Lerp(rb.start, rb.target, ratio);

                        Vector3 locPos = Vector3.Transform(new Vector3(rb.current, transform.localPosition.Z), transform.parent != null ? transform.parent.worldToLocaMatrix : Matrix4x4.Identity);
                        transform.localPosition = locPos;
                        transform.transformMove = oldMovestate;
                    }
                }
            }
            );
        }
    }
}
