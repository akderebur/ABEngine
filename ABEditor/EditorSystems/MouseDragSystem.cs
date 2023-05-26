using System;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;

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
                var query = _world.CreateQuery().Has<AABB>();
                foreach (var entity in query.GetEntities())
                {
                    AABB bbox = entity.Get<AABB>();
                    Transform transform = entity.Get<Transform>();
                   
                    if (bbox.CheckCollisionMouse(transform, Input.GetMousePosition()))
                    {
                        selectedTransform = transform;
                        dragDelta = selectedTransform.localPosition - new Vector3(Input.GetMousePosition().PixelToWorld() * Game.zoomFactor, 0f);
                        _world.SetData(entity);

                        break;
                    }
                }
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
