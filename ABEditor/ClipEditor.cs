using System;
using Veldrid;
using System.IO;
using ImGuiNET;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;

namespace ABEngine.ABEditor
{
    public class ClipEditor
    {
        public static bool isActive;

        static CommandList _cl;
        static ResourceFactory _rs;
      
        static EditorSprite clipSprite;
        static IntPtr texPtr = IntPtr.Zero;
        static SpriteClip clip;

        static Pipeline _drawPipeline;

        // Temp clip values
        static float _sampleFreq = 10f;
        static float _lastFrameTime = 0f;
        static int _curFrame = 0;

        public static void Init()
        {
            _cl = GraphicsManager.cl;
            _rs = GraphicsManager.rf;
        }

        public static void SetClip(string path)
        {
            //string assetPath = path.Replace(Editor.AssetPath, "");
            string assetPath = path;
            clip = new SpriteClip(assetPath);
            _sampleFreq = clip.sampleFreq;
            //Texture2D tex = AssetCache.CreateTexture2D(clip.imgPath);

            Texture tex = AssetCache.GetTextureDebug(Game.AssetPath + clip.imgPath);

            TextureMeta texMeta = AssetHandler.GetMeta(clip.imgPath) as TextureMeta;
            Texture2D tempTex2d = new Texture2D(0, tex, texMeta.sampler, texMeta.spriteSize);

            clipSprite = new EditorSprite(tempTex2d, _rs);
            texPtr = Editor.GetImGuiTexture(clipSprite.frameView);
            isActive = true;
        }

        public static void Draw(float gameTime)
        {
            if (!isActive)
            {
                if (clipSprite != null)
                {
                    clipSprite.DestroyResources();
                    clipSprite = null;
                }

                return;
            }

            _cl.SetFramebuffer(clipSprite.spriteFB);
            _cl.SetFullViewports();
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            if ((gameTime - clip.sampleFreq) > _lastFrameTime)
            {
                _curFrame++;
                if (_curFrame >= clip.frameCount)
                    _curFrame = 0;
                _lastFrameTime = gameTime;

                clipSprite.SetUVPosScale(clip.uvPoses[_curFrame], clip.uvScales[_curFrame]);
            }
            clipSprite.DrawEditor();

            ImGui.Begin("Clip Editor");
            if (texPtr != IntPtr.Zero)
                ImGui.Image(texPtr, new Vector2(100, 100));
            if (clip != null)
            {
                float sampleRate = clip.sampleRate;
                if (ImGui.InputFloat("Frame Rate", ref sampleRate))
                    clip.sampleRate = sampleRate;
            }

            if (ImGui.Button("Close"))
            {
                isActive = false;
            }
            ImGui.End();
        }
    }
}
