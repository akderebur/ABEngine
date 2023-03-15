using System;
using System.Collections.Generic;
using ABEngine.ABERuntime;
using ImGuiNET;
using Veldrid;
using System.Numerics;
using ABEngine.ABERuntime.ECS;
using System.Linq;
using System.Collections;
using System.Reflection;
using ABEngine.ABERuntime.Components;

namespace ABEngine.ABEUI
{
    [SubscribeAny(typeof(UIText), typeof(UIImageButton))]
    public class UIRenderer : RenderSystem
    {
        private static ImGuiRenderer imguiRenderer;
        private static Dictionary<string, ImFontPtr> fonts;

        static List<UIComponent> uiComponents = new List<UIComponent>();

        internal static Vector2 screenScale = Vector2.One;

        protected override void OnEntityCreated(in Entity entity)
        {
            UIComponent uiComp = null;
            if(entity.Has<UIText>())
                uiComp = entity.Get<UIText>();
            else if(entity.Has<UIImageButton>())
                uiComp = entity.Get<UIImageButton>();
            else if(entity.Has<UISliderImage>())
                uiComp = entity.Get<UISliderImage>();


            if (uiComp != null)
            {
                uiComp.transform = entity.transform;
                if (entity.Has<UIAnchor>())
                    uiComp.anchor = entity.Get<UIAnchor>();
                uiComponents.Add(uiComp);
            }
        }

        private UIRenderer()
        {
        }
        private static UIRenderer instance = null;
        public static UIRenderer Instance
        {
            get
            {
                if (instance == null)
                {
                    // Once in app lifetime
                    UISliderImage.InitSliderAssets();

                    ResetRenderer();

                    instance = new UIRenderer();

                    Game.onWindowResize += Game_onWindowResize;
                    Game.onSceneLoad += Game_onSceneLoad;
                }
             

                return instance;
            }
        }


        static void ResetRenderer()
        {
            if (imguiRenderer != null)
            {
                imguiRenderer.ClearCachedImageResources();
                imguiRenderer.DestroyDeviceObjects();
                imguiRenderer.Dispose();
            }

            imguiRenderer = new ImGuiRenderer(
            GraphicsManager.gd,
            GraphicsManager.gd.MainSwapchain.Framebuffer.OutputDescription,
            (int)Game.screenSize.X,
            (int)Game.screenSize.Y);

            screenScale = Game.screenSize / Game.canvas.referenceSize;

            fonts = new Dictionary<string, ImFontPtr>();
            uiComponents.Clear();

            var defaultFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(Game.AssetPath + "Fonts/OpenSans-Regular.ttf", 20 * screenScale.X);
            imguiRenderer.RecreateFontDeviceTexture();
            //var defRenderer = new VeldridTextRenderer(PipelineManager._gd, PipelineManager._cl, defaultFont);
            fonts.Add("0", defaultFont);
        }

        public override void CleanUp()
        {
            //Game.onWindowResize -= Game_onWindowResize;
            //Game.onSceneLoad -= Game_onSceneLoad;
        }

        private static void Game_onSceneLoad()
        {
            
        }

        private static void Game_onWindowResize()
        {
            imguiRenderer.WindowResized((int)Game.screenSize.X, (int)Game.screenSize.Y);

            screenScale = Game.screenSize / Game.canvas.referenceSize;

            fonts = new Dictionary<string, ImFontPtr>();
            uiComponents.Clear();

            var defaultFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(Game.AssetPath + "Fonts/OpenSans-Regular.ttf", 20 * screenScale.X);
            imguiRenderer.RecreateFontDeviceTexture();
            //var defRenderer = new VeldridTextRenderer(PipelineManager._gd, PipelineManager._cl, defaultFont);
            fonts.Add("0", defaultFont);
        }

        internal ImFontPtr GetOrCreateFont(string fontPath, float fontSize)
        {
            string key = fontPath + fontSize;
            if (fonts.ContainsKey(key))
                return fonts[key];
            else if (string.IsNullOrEmpty(fontPath))
            {
                fontPath = "Fonts/OpenSans-Regular.ttf";
            }

            ImFontPtr font = ImGui.GetIO().Fonts.AddFontFromFileTTF(Game.AssetPath + fontPath, fontSize * screenScale.X);
            imguiRenderer.RecreateFontDeviceTexture();
            fonts.Add(key, font);

            return font;

        }

        internal IntPtr GetImGuiTextureBinding(Texture texture)
        {
            return imguiRenderer.GetOrCreateImGuiBinding(GraphicsManager.rf, texture);
        }

        public override void Update(float gameTime, float deltaTime)
        {
            imguiRenderer.Update(deltaTime, Input.FrameSnapshot);

            var orderedComps = uiComponents.OrderBy(c => c.transform.worldPosition.Z);

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(Game.screenSize);
            ImGui.Begin("Canvas", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground);

            foreach (UIComponent component in orderedComps)
            {
                component.Render();
            }

            ImGui.End();
        }

        internal Vector2 CalculateEndPos(UIAnchor anchor, Vector3 uıItemPos)
        {
            Vector2 dif = uıItemPos.ToImGuiRefVector2() - anchor.anchorPos;

            //float newX = anchor.anchorPos.X * Game.screenSize.X / Game.canvas.referenceSize.X;
            //float newY = anchor.anchorPos.Y * Game.screenSize.Y / Game.canvas.referenceSize.Y;

            return anchor.anchorPos * screenScale + dif * screenScale;
        }

        public override void UIRender()
        {
            imguiRenderer.Render(GraphicsManager.gd, GraphicsManager.cl);
        }
    }

    class UILayer
    {
        public List<Entity> uiTexts = new List<Entity>();
        public List<Entity> uiImageButtons = new List<Entity>();
        public List<Entity> uiSliderImages = new List<Entity>();
    }
}
