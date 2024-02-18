using System;
using WGIL;
using System.IO;
using ImGuiNET;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABEditor
{
    public class ClipEditor
    {
        public static bool isActive;
      
        static EditorSprite clipSprite;
        static IntPtr texPtr = IntPtr.Zero;
        static SpriteClip clip;

        static RenderPipeline _drawPipeline;

        // Temp clip values
        static float _sampleFreq = 10f;
        static float _lastFrameTime = 0f;
        static int _curFrame = 0;

        public static void Init()
        {

        }

        public static void SetClip(string path)
        {
            //string assetPath = path.Replace(Editor.AssetPath, "");
            string assetPath = path;
            clip = new SpriteClip(assetPath);
            _sampleFreq = clip.SampleFreq;
            //Texture2D tex = AssetCache.CreateTexture2D(clip.imgPath);

            Texture tex = AssetCache.GetTextureDebug(Game.AssetPath + clip.imgPath);

            TextureMeta texMeta = AssetHandler.GetMeta(clip.imgPath) as TextureMeta;
            Texture2D tempTex2d = new Texture2D(0, tex, texMeta.sampler, texMeta.spriteSize);

            clipSprite = new EditorSprite(tempTex2d);
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

            if ((gameTime - clip.SampleFreq) > _lastFrameTime)
            {
                _curFrame++;
                if (_curFrame >= clip.FrameCount)
                    _curFrame = 0;
                _lastFrameTime = gameTime;

                clipSprite.SetUVPosScale(clip.uvPoses[_curFrame], clip.uvScales[_curFrame]);
            }

            ImGui.Begin("Clip Editor");
            if (texPtr != IntPtr.Zero)
                ImGui.Image(texPtr, new Vector2(100, 100), clipSprite.uvPos, clipSprite.uvPos + clipSprite.uvScale);
            if (clip != null)
            {
                float sampleRate = clip.SampleRate;
                if (ImGui.InputFloat("Frame Rate", ref sampleRate))
                    clip.SampleRate = sampleRate;
            }

            if (ImGui.Button("Close"))
            {
                isActive = false;
            }
            ImGui.End();
        }
    }
}
