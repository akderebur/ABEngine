using System;
using ABEngine.ABERuntime;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using Arch.Core.Extensions;
using Arch.Core;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABEditor
{
    public class AABBGizmoSytem : BaseSystem
    {
        Sprite bboxSprite = null;
        Matrix4x4 projectionMatrix;

        public override void Start()
        {
            Texture2D bboxTex = AssetCache.CreateTexture2D("Sprites/bbox_frame.png");
            bboxSprite = new Sprite(bboxTex);
            bboxSprite.Resize(new Vector2(100f, 100f));

            Game_onWindowResize();
            //Game.onWindowResize += Game_onWindowResize;
        }

        private void Game_onWindowResize()
        {
            //projectionMatrix = Game.projectionMatrix;
        }


        public override void Update(float gameTime, float deltaTime)
        {
            if (Game.activeCamTrans == null)
                return;

            var selectedEntity = Editor.selectedEntity;
            if (selectedEntity != Entity.Null && selectedEntity.Has<AABB>())
            {
                AABB bbox = selectedEntity.Get<AABB>();
                //Vector3 centerPos = new Vector3(bbox.center, 0f);

                //Vector3 aabbScale = new Vector3(bbox.width / 100f, bbox.height / 100f, 0f);

                //Matrix4x4 drawMat = Matrix4x4.CreateScale(aabbScale) * Matrix4x4.CreateTranslation(centerPos) * selectedEntity.Get<Transform>().worldMatrix * Game.activeCam.worldToLocaMatrix;
                //bboxSprite.Draw(ref projectionMatrix, ref drawMat);
            }
        }

    }
}
