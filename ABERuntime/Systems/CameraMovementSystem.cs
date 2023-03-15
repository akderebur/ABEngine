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
            var query = _world.CreateQuery().Has<Camera>().Has<Transform>();
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
            var query = _world.CreateQuery().Has<Camera>().Has<Transform>();
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
            var query = _world.CreateQuery().Has<Camera>().Has<Transform>();
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
            Vector3 newPos = Vector3.Lerp(camTrans.localPosition, cam.followTarget.worldPosition + cam.offset, cam.speed * deltaTime);
            newPos.Z = 0f;
            //Vector3 newPos = cam.followTarget.worldPosition + cam.offset;
            if (cam.ignoreY)
                newPos.Y = camTrans.localPosition.Y;
            camTrans.localPosition = newPos;
        }
    }
}
