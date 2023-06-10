using System;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABERuntime
{
    public class CameraMovementSystem : BaseSystem
    {
        public override void Start()
        {
            var query = Game.GameWorld.CreateQuery().Has<Camera>().Has<Transform>();
            query.Foreach((ref Camera cam, ref Transform camTrans) =>
            {
                if (cam.followTarget != null)
                {
                    var destPos = cam.followTarget.worldPosition + cam.offset;

                    if (cam.ignoreY)
                        destPos.Y = camTrans.localPosition.Y;

                    camTrans.localPosition = destPos;
                }
            }
            );
        }

        public override void Update(float gameTime, float deltaTime)
        {
            var query = Game.GameWorld.CreateQuery().Has<Camera>().Has<Transform>();
            query.Foreach((ref Camera cam, ref Transform camTrans) =>
            {
                if (!cam.followInFixedUpdate && cam.followTarget != null)
                {
                    FollowTarget(camTrans, cam, deltaTime);
                }
            }
            );
        }

        public override void FixedUpdate(float gameTime, float deltaTime)
        {
            var query = Game.GameWorld.CreateQuery().Has<Camera>().Has<Transform>();
            query.Foreach((ref Camera cam, ref Transform camTrans) =>
            {
                if (cam.followInFixedUpdate && cam.followTarget != null)
                {
                    FollowTarget(camTrans, cam, deltaTime);
                }
            }
            );
        }

        void FollowTarget(Transform camTrans, Camera cam, float deltaTime)
        {
            //Vector3 targPos = cam.followTarget.worldPosition + cam.offset;
            //if (targPos.Y < cam.cutoffY)
            //    targPos.Y = cam.cutoffY;

            //// Define a smoothTime (analogous to Unity's smoothTime in SmoothDamp)
            //float smoothTime = 0.05f;
            //// Calculate the difference between the current and target positions
            //Vector3 diff = targPos - camTrans.localPosition;
            //// Calculate a "dampened" velocity based on the difference, the current velocity, and the smooth time
            //cam.velocity = cam.velocity + diff * (2f / smoothTime) * deltaTime - cam.velocity * (1f / smoothTime) * deltaTime;
            //// Update the new position based on the dampened velocity
            //Vector3 newPos = camTrans.localPosition + cam.velocity * deltaTime;
            //newPos.Z = 0f;

            //if (cam.ignoreY)
            //    newPos.Y = camTrans.localPosition.Y;

            //camTrans.localPosition = newPos;

            Vector3 targPos = cam.followTarget.worldPosition + cam.offset;
            if (targPos.Y < cam.cutoffY)
                targPos.Y = cam.cutoffY;

            Vector3 newPos = Vector3.Lerp(camTrans.localPosition, targPos, cam.speed * deltaTime);
            newPos.Z = 0f;

            //Vector3 newPos = cam.followTarget.worldPosition + cam.offset;
            if (cam.ignoreY)
                newPos.Y = camTrans.localPosition.Y;

            camTrans.localPosition = newPos;
        }
    }
}
