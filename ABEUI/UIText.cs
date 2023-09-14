using System;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using Arch.Core.Extensions;
using ImGuiNET;

namespace ABEngine.ABEUI
{
    public class UIText : UIComponent
    {
        public string text { get; set; }
        public string fontPath { get; set; }
        public float fontSize { get; set; }

        private Vector4 _textColor;
        public Vector4 textColor
        {
            get { return _textColor; }
            set { _textColor = value; curColor = value; hoverColor = textColor; }
        }

        public Vector4 hoverColor { get; set; }

        internal Func<string> textFunc;

        internal Vector4 curColor;
        internal ImFontPtr font;

        internal UIText()
        {

        }

        public UIText(string textContent)
        {
            text = textContent;
            fontPath = "";
            fontSize = 12;
            textColor = Vector4.One;
            hoverColor = textColor;
            LoadFont();
        }

        public UIText(string textContent, float fontSize)
        {
            text = textContent;
            fontPath = "";
            this.fontSize = fontSize;
            textColor = Vector4.One;
            hoverColor = textColor;
            LoadFont();
        }

        public UIText(Func<string> func, float fontSize)
        {
            text = func();
            textFunc = func;
            fontPath = "";
            this.fontSize = fontSize;
            textColor = Vector4.One;
            hoverColor = textColor;
            LoadFont();
        }
        public UIText(string textContent, string fontPath, float fontSize)
        {
            text = textContent;
            this.fontPath = fontPath;
            this.fontSize = fontSize;
            textColor = Vector4.One;
            hoverColor = textColor;
            LoadFont();
        }

        protected override void PostDeserialize()
        {
            if (string.IsNullOrEmpty(fontPath))
                fontPath = "";

            LoadFont();
        }

        internal void LoadFont()
        {
            curColor = textColor;
            font = UIRenderer.Instance.GetOrCreateFont(fontPath, fontSize);
        }

        public void SetTextColor(Vector4 textColor)
        {
            curColor = textColor;
        }

        public void RevertTextColor()
        {
            curColor = textColor;
        }

        internal override void Render()
        {
            UIText uiText = this;
            Transform txtTrans = base.transform;

            if (uiText.textFunc != null)
                uiText.text = uiText.textFunc();

            Vector2 screenPos = txtTrans.worldPosition.ToImGuiVector2();
            Vector2 endPos = screenPos;
            if (base.anchor != null) // Calculate anchor pos
            {
                endPos = UIRenderer.Instance.CalculateEndPos(base.anchor, txtTrans.worldPosition);
            }
            else if (transform.entity.Has<UIAnchor>())
                base.anchor = transform.entity.Get<UIAnchor>();
            ImGui.PushFont(uiText.font);
            ImGui.SetCursorPos(endPos);
            ImGui.PushStyleColor(ImGuiCol.Text, hovered ? uiText.hoverColor : uiText.curColor);
            ImGui.PushTextWrapPos(0.0f);
            ImGui.TextWrapped(uiText.text);
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }
    }
}
