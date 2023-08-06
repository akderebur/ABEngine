using System;
using System.Numerics;
using ABEngine.ABERuntime;
using Veldrid;

namespace ABEngine.ABEditor
{
    public class EditorCamMoveSystem : BaseSystem
    {
        public static float camMoveSpeed = 1f;

        public override void Update(float gameTime, float deltaTime)
        {
            if (Game.activeCam == null)
                return;

            Transform transform = Game.activeCam;
            Vector3 endPos = transform.localPosition;

            bool modKey = Input.GetKey(Key.ShiftLeft) || Input.GetKey(Key.AltLeft) || Input.GetKey(Key.ControlLeft);
            if (Input.GetKey(Key.D))
            {
                if(!modKey)
                    endPos += new Vector3(Vector2.UnitX, 0f);
            }

            if (Input.GetKey(Key.A))
            {
                if (!modKey)
                    endPos -= new Vector3(Vector2.UnitX, 0f);
            }

            if (Input.GetKey(Key.W))
            {
                if (!modKey)
                    endPos += new Vector3(Vector2.UnitY, 0f);
            }

            if (Input.GetKey(Key.S))
            {
                if (!modKey)
                    endPos -= new Vector3(Vector2.UnitY, 0f);
            }


            Vector3 newPos = Vector3.Lerp(transform.localPosition, endPos, deltaTime * camMoveSpeed);
            //newPos.Z = 0f;
            transform.localPosition = newPos;

        }
    }
}
