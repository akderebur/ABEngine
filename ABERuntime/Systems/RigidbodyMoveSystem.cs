using System;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Arch.Core;
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
            var query = new QueryDescription().WithAll<Transform, Rigidbody>();

            Game.GameWorld.Query(in query, (ref Rigidbody rb) =>
            {
                if (rb.b2dBody != null && rb.bodyType != BodyType.Static)
                {
                    rb.start = rb.current = rb.target = rb.b2dBody.GetPosition();
                }
            });
        }

        public void PreFixedUpdate()
        {
            var query = new QueryDescription().WithAll<Transform, Rigidbody>();

            Game.GameWorld.Query(in query, (ref Transform transform, ref Rigidbody rb) =>
            {
                if (rb.b2dBody != null && rb.bodyType != BodyType.Static)
                {
                    if (transform.transformMove)
                    {
                        rb.SetPosition(transform.worldPosition);
                    }

                    transform.transformMove = false;

                    rb.start = rb.current = rb.target = rb.b2dBody.GetPosition();
                }
            }
            );
        }

        public override void FixedUpdate(float gameTime, float fixedDeltaTime)
        {
            var query = new QueryDescription().WithAll<Transform, Rigidbody>();

            Game.GameWorld.Query(in query, (ref Transform transform, ref Rigidbody rb) =>
            {


                if (rb.b2dBody != null && rb.bodyType != BodyType.Static)
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
                        Vector3 locPos = Vector3.Transform(new Vector3(rb.b2dBody.GetPosition(), transform.localPosition.Z), transform.parent != null ? transform.parent.worldToLocalMatrix : Matrix4x4.Identity);
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
            var query = new QueryDescription().WithAll<Transform, Rigidbody>();

            Game.GameWorld.Query(in query, (ref Transform transform, ref Rigidbody rb) =>
            {
                if (rb.bodyType != BodyType.Static)
                {
                    if (rb.interpolationType == RBInterpolationType.Interpolate)
                    {
                        bool oldMovestate = transform.transformMove;
                        rb.current = Vector2.Lerp(rb.start, rb.target, ratio);

                        Vector3 locPos = Vector3.Transform(new Vector3(rb.current, transform.localPosition.Z), transform.parent != null ? transform.parent.worldToLocalMatrix : Matrix4x4.Identity);
                        transform.localPosition = locPos;
                        transform.transformMove = oldMovestate;
                    }
                }
            }
            );
        }
    }
}
