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

            bool modKey = Input.GetKey(Key.ShiftLeft) || Input.GetKey(Key.AltLeft) || Input.GetKey(Key.ControlLeft);
            if (Input.GetKey(Key.D))
            {
                if(!modKey)
                    transform.localPosition += new Vector3(Vector2.UnitX * deltaTime * camMoveSpeed, 0f);
            }

            if (Input.GetKey(Key.A))
            {
                if (!modKey)
                    transform.localPosition -= new Vector3(Vector2.UnitX * deltaTime * camMoveSpeed, 0f);
            }

            if (Input.GetKey(Key.W))
            {
                if (!modKey)
                    transform.localPosition += new Vector3(Vector2.UnitY * deltaTime * camMoveSpeed, 0f);
            }

            if (Input.GetKey(Key.S))
            {
                if (!modKey)
                    transform.localPosition -= new Vector3(Vector2.UnitY * deltaTime * camMoveSpeed, 0f);
            }


            
        }
    }
}
