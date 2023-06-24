using System;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using Arch.Core;

namespace ABEngine.ABEditor
{
    public class MouseDragSystem : BaseSystem
    {
        Transform selectedTransform;
        Vector3 dragDelta;

        public override void Update(float gameTime, float deltaTime)
        {
            if (!Input.GetKey(Veldrid.Key.ControlLeft) && Input.GetMouseButtonDown(Veldrid.MouseButton.Left))
            {
                var query = new QueryDescription().WithAll<AABB>();

                Game.GameWorld.Query(in query, (in Entity entity, ref AABB bbox, ref Transform transform) =>
                {
                    if (bbox.CheckCollisionMouse(transform, Input.GetMousePosition()))
                    {
                        selectedTransform = transform;
                        dragDelta = selectedTransform.localPosition - new Vector3(Input.GetMousePosition().PixelToWorld() * Game.zoomFactor, 0f);
                        Editor.selectedEntity = entity;

                        return; ;
                    }
                });
            }
            else if (Input.GetMouseButton(Veldrid.MouseButton.Left))
            {
                if (selectedTransform != null)
                {
                    selectedTransform.localPosition = new System.Numerics.Vector3(Input.GetMousePosition().PixelToWorld() * Game.zoomFactor, 0f) + dragDelta;
                }
            }
            else
                selectedTransform = null;

            //// Selection options
            //if (selectedTransform != null && Game.activeCam != null)
            //{
            //    // BBOX
            //    var selEnt = selectedTransform.entity;
            //    if(selEnt.Has<AABB>())
            //    {
            //        bboxSprite.Draw(Game.activeCam.worldToLocaMatrix * selEnt.Get<Transform>().worldMatrix);
            //    }
            //}
        }
    }
}
