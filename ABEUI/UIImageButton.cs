using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ABEngine.ABERuntime.Core.Assets;
using ImGuiNET;

namespace ABEngine.ABEUI
{
	public class UIImageButton : UIComponent
	{
        public Texture2D texture2d { get; set; }
        public Texture2D hoverTexture { get; set; }
        public Texture2D clickTexture { get; set; }

        public bool isClicked { get; set; }
        public bool isMouseOn { get; set; }

        public Vector2 size { get; set; }
        public Vector4 hoverColor { get; set; }

        internal Vector4 curColor;

        internal IntPtr imgPtr;
        internal IntPtr imgDefPtr;
        internal IntPtr imgHoverPtr;
        internal IntPtr imgClickPtr;

        public event Action onClicked;
        public event Action onReleased;

        public event Action onMouseEnter;
        public event Action onMouseExit;


        public UIImageButton(Texture2D texture)
		{
            this.texture2d = texture;
            this.hoverTexture = texture;
            this.clickTexture = texture;
            this.size = texture.imageSize;
            this.hoverColor = Vector4.One;

            GetTextureBindings();
		}

        public UIImageButton(Texture2D texture, Vector2 size)
        {
            this.texture2d = texture;
            this.hoverTexture = texture;
            this.clickTexture = texture;
            this.size = size;
            this.hoverColor = Vector4.One;

            GetTextureBindings();
        }

        public UIImageButton(Texture2D texture, Texture2D hoverTexture, Texture2D clickTexture, Vector2 size)
        {
            this.texture2d = texture;
            this.hoverTexture = hoverTexture;
            this.clickTexture = clickTexture;
            this.size = size;
            this.hoverColor = Vector4.One;

            GetTextureBindings();
        }

        private void GetTextureBindings()
        {
            curColor = this.hoverColor;

            imgPtr = UIRenderer.Instance.GetImGuiTextureBinding(texture2d.GetView());
            imgDefPtr = imgPtr;
            imgHoverPtr = UIRenderer.Instance.GetImGuiTextureBinding(hoverTexture.GetView());
            imgClickPtr = UIRenderer.Instance.GetImGuiTextureBinding(clickTexture.GetView());
        }

        internal void ButtonClickEvent()
        {
            if (isClicked)
                return;

            isClicked = true;
            imgPtr = imgClickPtr;
            onClicked?.Invoke();
        }


        internal void ButtonReleaseEvent()
        {
            if (!isClicked)
                return;

            isClicked = false;
            imgPtr = imgDefPtr;
            onReleased?.Invoke();
        }

        internal void MouseEnterEvent()
        {
            if (isMouseOn)
                return;

            isMouseOn = true;
            onMouseEnter?.Invoke();
        }

        internal void MouseExitEvent()
        {
            if (!isMouseOn)
                return;

            isMouseOn = false;
            onMouseExit?.Invoke();
        }

        internal override void Render()
        {
            UIImageButton uiImgBtn = this;
            Transform btnTrans = base.transform;

            Vector2 screenPos = btnTrans.worldPosition.ToImGuiVector2();
            Vector2 endPos = screenPos;
            if (base.anchor != null) // Calculate anchor pos
            {
                endPos = UIRenderer.Instance.CalculateEndPos(anchor, btnTrans.worldPosition);
            }

            Vector2 endSize = btnTrans.worldScale.ToVector2() * uiImgBtn.size * UIRenderer.Instance.screenScale;

            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);

            ImGui.SetCursorPos(endPos);
            if (ImGui.ImageButton(uiImgBtn.GetHashCode().ToString(), uiImgBtn.imgPtr, endSize, Vector2.Zero, Vector2.One, Vector4.Zero, uiImgBtn.curColor))
            {
                uiImgBtn.ButtonClickEvent();
            }
            else
            {
                uiImgBtn.ButtonReleaseEvent();

                if (ImGui.IsItemHovered())
                {
                    uiImgBtn.curColor = uiImgBtn.hoverColor;
                    uiImgBtn.imgPtr = uiImgBtn.imgHoverPtr;
                    uiImgBtn.MouseEnterEvent();
                }
                else
                {
                    uiImgBtn.curColor = Vector4.One;
                    uiImgBtn.imgPtr = uiImgBtn.imgDefPtr;
                    uiImgBtn.MouseExitEvent();
                }
            }

            ImGui.PopStyleColor(3);
        }

    }
}

