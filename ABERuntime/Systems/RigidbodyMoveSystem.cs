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

        //public void ResetSmoothStates()
        //{
        //    var query = _world.CreateQuery().Has<Rigidbody>().Has<Transform>();

        //    foreach (var rbEnt in query.GetEntities())
        //    {
        //        var rb = rbEnt.Get<Rigidbody>();
        //        if (rb.bodyType == BodyType.Static)
        //            continue;

        //        rb.smoothedPosition = rb.b2dBody.GetPosition();
        //    }
        //}

        //public void SmoothStates(float accumulatorRatio)
        //{
        //    float dt = accumulatorRatio * 1f / 60f;


        //    var query = _world.CreateQuery().Has<Rigidbody>().Has<Transform>();

        //    foreach (var rbEnt in query.GetEntities())
        //    {
        //        var rb = rbEnt.Get<Rigidbody>();
        //        if (rb.bodyType == BodyType.Static)
        //            continue;

        //        rb.smoothedPosition = rb.b2dBody.GetPosition() + dt * rb.b2dBody.GetLinearVelocity();
        //    }
        //}

        public void ResetSmoothStates()
        {
            var query = _world.CreateQuery().Has<Rigidbody>().Has<Transform>();

            foreach (var rbEnt in query.GetEntities())
            {
                var rb = rbEnt.Get<Rigidbody>();
                if (rb.bodyType == BodyType.Static)
                    continue;

                //rb.smoothedPosition = rb.previousPosition = rb.b2dBody.GetPosition();
                rb.start = rb.current = rb.target = rb.b2dBody.GetPosition();
            }
        }

        public void SmoothStates(float accumulatorRatio)
        {
            float oneMinusRatio = 1f - accumulatorRatio;

            var query = _world.CreateQuery().Has<Rigidbody>().Has<Transform>();

            foreach (var rbEnt in query.GetEntities())
            {
                var rb = rbEnt.Get<Rigidbody>();
                if (rb.bodyType == BodyType.Static)
                    continue;

                //rb.smoothedPosition = Vector2.Lerp(rb.previousPosition, rb.b2dBody.GetPosition(), oneMinusRatio);
                rb.smoothedPosition = accumulatorRatio * rb.b2dBody.GetPosition() + oneMinusRatio * rb.previousPosition;
            }
        }

        public void PreFixedUpdate()
        {
            var query = _world.CreateQuery().Has<Rigidbody>().Has<Transform>();

            query.Foreach((Entity rbEnt, ref Rigidbody rb, ref Transform transform) =>
            {
                if (transform.transformMove)
                    rb.SetPosition(transform.worldPosition);

                transform.transformMove = false;
            }
            );
        }

        public override void FixedUpdate(float gameTime, float fixedDeltaTime)
        {
            var query = _world.CreateQuery().Has<Rigidbody>().Has<Transform>();

            query.Foreach((Entity rbEnt, ref Rigidbody rb, ref Transform transform) =>
            {
                if (rb.entity.enabled && rb.bodyType != BodyType.Static)
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
                        rb.start = rb.current;
                        rb.target = rb.b2dBody.GetPosition();
                    }

                    transform.transformMove = false;
                }
            }
            );
        }

        public override void Update(float gameTime, float ratio)
        {
            var query = _world.CreateQuery().Has<Rigidbody>().Has<Transform>();

            query.Foreach((Entity rbEnt, ref Rigidbody rb, ref Transform transform) =>
            {
                if (rb.entity.enabled && rb.bodyType != BodyType.Static)
                {
                    if (rb.interpolationType == RBInterpolationType.Interpolate)
                    {
                        rb.current = Vector2.Lerp(rb.start, rb.target, ratio);

                        Vector3 locPos = Vector3.Transform(new Vector3(rb.current, transform.localPosition.Z), transform.parent != null ? transform.parent.worldToLocaMatrix : Matrix4x4.Identity);
                        transform.localPosition = locPos;
                    }
                }
            }
            );
        }
    }
}
