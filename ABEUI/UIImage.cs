using System;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ImGuiNET;

namespace ABEngine.ABEUI
{
	public class UIImage : UIComponent
	{
        public Texture2D texture2d { get; set; }
        public Vector2 size { get; set; }
        public Vector4 tintColor { get; set; }

        internal IntPtr imgPtr;

        public UIImage(Texture2D texture)
        {
            this.texture2d = texture;
            this.size = texture.imageSize;
            this.tintColor = Vector4.One;

            GetTextureBindings();
        }

        public UIImage(Texture2D texture, Vector2 size)
        {
            this.texture2d = texture;
            this.size = size;
            this.tintColor = Vector4.One;

            GetTextureBindings();
        }

        private void GetTextureBindings()
        {
            imgPtr = UIRenderer.Instance.GetImGuiTextureBinding(texture2d.GetView());
        }

        internal override void Render()
        {
            UIImage uiImg = this;
            Transform btnTrans = base.transform;

            Vector2 screenPos = btnTrans.worldPosition.ToImGuiVector2();
            Vector2 endPos = screenPos;
            if (base.anchor != null) // Calculate anchor pos
            {
                endPos = UIRenderer.Instance.CalculateEndPos(anchor, btnTrans.worldPosition);
            }

            Vector2 endSize = btnTrans.worldScale.ToVector2() * uiImg.size * UIRenderer.Instance.screenScale;

            ImGui.SetCursorPos(endPos);
            ImGui.Image(uiImg.imgPtr, endSize, Vector2.Zero, Vector2.One, uiImg.tintColor);
        }
    }
}

