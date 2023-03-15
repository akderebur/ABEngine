using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Veldrid;
using System.Numerics;
using System.Linq;
using Halak;
using ABEngine.ABERuntime;

namespace ABEngine.ABEditor
{
    public class SpriteEditor
    {
        public static bool isActive = false;
        static Texture texture;
        static IntPtr texPtr = IntPtr.Zero;
        static IntPtr greenBoxPtr = IntPtr.Zero;

        static float cutWidth = 720;
        static float cutHeight = 720;
        static float lastCutWidth = 720;
        static float lastCutHeight = 720;
        static float cutPrWidth = 720;
        static float cutPrHeight = 720;

        static float prWidth = 720;
        static float prHeight = 720;

        static float widthMult;
        static float heightMult;

        static string texName = "NoName";

        static uint greenCol, blueCol, whiteCol;

        static float borderPad = 1.3f;

        static string curImgPath;
        static List<CutQuad> selFrames;
        static ImFontPtr textFont;

        static string clipName = "ClipName";

        static List<CutQuad> curSelection;


        static List<CutQuad> quads = new List<CutQuad>();
        internal class CutQuad
        {
            public float startX;
            public float StartY;
            public float srcStartX;
            public float srcStartY;
            public bool selected;
            public int selInd;
            public int quadId;
        }


        public static void Init()
        {
            selFrames = new List<CutQuad>();
            curSelection = new List<CutQuad>();
            greenBoxPtr = Editor.GetImGuiTexture(Editor.greenBoxTex);
            greenCol = ImGui.GetColorU32(new Vector4(0f, 0.7f, 0f, 1));
            blueCol = ImGui.GetColorU32(new Vector4(0f, 0f, 1f, 1));
            whiteCol = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1));

            ImFontConfig fontConfig = new ImFontConfig()
            {
                SizePixels = 20
            };

            textFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(Directory.GetCurrentDirectory() + "/Assets/Fonts/OpenSans-Regular.ttf", 40);
            Editor.GetImGuiRenderer().RecreateFontDeviceTexture();

        }

        public static void SetImage(string imgPath)
        {
            //var imgDict = Editor.GetImgDict();
            //string assetPath = imgPath.Replace(Editor.AssetPath, "");
            if(File.Exists(imgPath))
            {
                curImgPath = imgPath;
                texName = Path.GetFileName(imgPath);
                texture = AssetCache.GetTextureDebug(imgPath);
                if(texture.Width > texture.Height)
                {
                    prWidth = 720f;
                    prHeight = texture.Height * prWidth / texture.Width;
                }
                else
                {
                    prHeight = 720f;
                    prWidth = texture.Width * prHeight / texture.Height;
                }

                widthMult = prWidth / texture.Width;
                heightMult = prHeight / texture.Height;

                cutWidth = texture.Width;
                cutHeight = texture.Height;
                lastCutWidth = cutWidth;
                lastCutHeight = cutHeight;

                cutPrWidth = cutWidth * widthMult;
                cutPrHeight = cutHeight * heightMult;

                quads.Clear();
                quads.Add(new CutQuad() { startX = 0, StartY = 0 });

                texPtr = Editor.GetImGuiTexture(texture);
                isActive = true;

                // Settings
                string ext = Path.GetExtension(curImgPath);
                string setPath = curImgPath.Replace(ext, "_settings.abjs");
                if(File.Exists(setPath))
                {
                    var data = JValue.Parse(File.ReadAllText(setPath));
                    cutWidth = data["CutWidth"];
                    cutHeight = data["CutHeight"];
                }
            }
        }

        public static void Draw()
        {
            if (!isActive)
                return;


            ImGui.SetNextWindowSize(new Vector2(1280, 720));
            ImGui.SetNextWindowPos(new Vector2(0f, 0f));
            ImGui.Begin("Sprite Editor", ImGuiWindowFlags.NoMove);

            ImGui.BeginTabBar("SpriteEditorTabs");

            ImGui.Text(texName);
            ImGui.InputFloat("Cut Width", ref cutWidth);
            ImGui.InputFloat("Cut height", ref cutHeight);

            if (ImGui.Button("Save Settings"))
            {
                if (!string.IsNullOrEmpty(curImgPath))
                {
                    string ext = Path.GetExtension(curImgPath);
                    string setPath = curImgPath.Replace(ext, "_settings.abjs");

                    JsonObjectBuilder jObj = new JsonObjectBuilder(500);
                    jObj.Put("CutWidth", cutWidth);
                    jObj.Put("CutHeight", cutHeight);
                    File.WriteAllText(setPath, jObj.Build().Serialize());
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Clear"))
            {
                cutWidth = texture.Width;
                cutHeight = texture.Height;
            }

            ImGui.InputText("Clip Name", ref clipName, 100);
            ImGui.SameLine();
            if (ImGui.Button("Create Clip"))
            {
                JsonObjectBuilder jClip = new JsonObjectBuilder(1024);

                JsonObjectBuilder jMeta = new JsonObjectBuilder(200);
                jMeta.Put("app", "ABEditor");
                jMeta.Put("image", Path.GetFileName(curImgPath));
                JsonObjectBuilder jSize = new JsonObjectBuilder(100);
                jSize.Put("w", (int)texture.Width);
                jSize.Put("h", (int)texture.Height);
                jMeta.Put("size", jSize.Build());
                JsonObjectBuilder jCutSize = new JsonObjectBuilder(100);
                jCutSize.Put("w", cutWidth);
                jCutSize.Put("h", cutHeight);
                jMeta.Put("cutSize", jCutSize.Build());

                jClip.Put("meta", jMeta.Build());

                JsonArrayBuilder jFrames = new JsonArrayBuilder(512);
                for (int i = 0; i < selFrames.Count; i++)
                {
                    CutQuad frameQuad = selFrames[i];
                    JsonObjectBuilder jFrameDesc = new JsonObjectBuilder(200);

                    JsonObjectBuilder jFrame = new JsonObjectBuilder(100);
                    jFrame.Put("x", frameQuad.srcStartX);
                    jFrame.Put("y", frameQuad.srcStartY);
                    jFrame.Put("w", cutWidth);
                    jFrame.Put("h", cutHeight);

                    jFrameDesc.Put("frame", jFrame.Build());
                    jFrames.Push(jFrameDesc.Build());
                }

                jClip.Put("frames", jFrames.Build());

                if (!string.IsNullOrEmpty(clipName))
                {
                    try
                    {
                        string folder = Path.GetDirectoryName(curImgPath);
                        File.WriteAllText(folder + "/" + clipName + ".abanim2d", jClip.Build().Serialize());
                    }
                    catch
                    {

                    }
                }
            }

            if (lastCutHeight != cutHeight || lastCutWidth != cutWidth)
            {
                if (cutWidth < 1f)
                    cutWidth = 1f;
                if (cutHeight < 1f)
                    cutHeight = 1f;

                cutPrWidth = cutWidth * widthMult;
                cutPrHeight = cutHeight * heightMult;

                lastCutHeight = cutHeight;
                lastCutWidth = cutWidth;

                selFrames = new List<CutQuad>();

                float curWidth = 0f, curHeight = 0f;
                float srcWidth = 0f, srcHeight = 0f;

                quads.Clear();
                while (curHeight < prHeight)
                {
                    curWidth = 0f;
                    srcWidth = 0f;
                    while (curWidth < prWidth)
                    {
                        quads.Add(new CutQuad() { startX = curWidth, StartY = curHeight, srcStartX = srcWidth, srcStartY = srcHeight });
                        //draw.AddQuad(new Vector2(pos.X + curWidth, pos.Y + curHeight), new Vector2(pos.X + curWidth + cutPrWidth, pos.Y + curHeight), new Vector2(pos.X + cutPrWidth + curWidth, pos.Y + cutPrHeight + curHeight), new Vector2(pos.X + curWidth, pos.Y + cutPrHeight + curHeight), col, 0.1f);
                        curWidth += cutPrWidth;
                        srcWidth += cutWidth;
                    }

                    curHeight += cutPrHeight;
                    srcHeight += cutHeight;
                }
            }


            if (texPtr != IntPtr.Zero)
            {
                Vector2 pos = ImGui.GetCursorScreenPos();

                var draw = ImGui.GetWindowDrawList();

                ImGui.Image(texPtr, new Vector2(prWidth, prHeight));

                foreach (var quad in quads) // Draw cut quads
                {

                    Vector2 startPos = pos + new Vector2(quad.startX, quad.StartY);
                    draw.AddQuad(startPos, new Vector2(startPos.X + cutPrWidth - borderPad, startPos.Y), new Vector2(startPos.X + cutPrWidth - borderPad, startPos.Y + cutPrHeight - borderPad), new Vector2(startPos.X, startPos.Y + cutPrHeight - borderPad), quad.selected ? blueCol : greenCol, 0.1f);


                    if (quad.selected)
                    {
                        ImGui.PushFont(textFont);
                        draw.AddText(new Vector2(startPos.X + cutPrWidth / 2f, startPos.Y + cutPrHeight / 2f), whiteCol, "" + quad.selInd);
                        ImGui.PopFont();
                    }
                }


                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (!ImGui.IsKeyDown(ImGuiKey.Z)) // Keep selections
                        curSelection = new List<CutQuad>();
                }
                else if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    Vector2 mousePos = ImGui.GetMousePos();

                    if (mousePos.X > pos.X && mousePos.Y > pos.Y && mousePos.X < pos.X + prWidth && mousePos.Y < pos.Y + prHeight)
                    {
                        Vector2 normPos = mousePos - pos;
                        var selQuad = quads.LastOrDefault(q => normPos.X > q.startX && normPos.Y > q.StartY);
                        if (selQuad != null)
                        {
                            if (!curSelection.Contains(selQuad))
                                curSelection.Add(selQuad);
                            /*
                            int ind = quads.IndexOf(selQuad);
                            //quads[ind].selected = qu;
                            selQuad.selected = !selQuad.selected;
                            if (selQuad.selected)
                            {
                                selFrames.Add(selQuad);
                                selQuad.selInd = selFrames.Count;
                            }
                            else
                            {
                                selFrames.Remove(selQuad);

                                for (int i = 0; i < selFrames.Count; i++)
                                {
                                    selFrames[i].selInd = i + 1;
                                }
                            }
                            Console.WriteLine(ind);
                            */
                        }
                    }
                }
                else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left)) // Display selected on release
                {
                    foreach (var selQuad in curSelection)
                    {
                        selQuad.selected = !selQuad.selected;

                        if (selQuad.selected)
                        {
                            selFrames.Add(selQuad);
                            selQuad.selInd = selFrames.Count;
                        }
                        else
                        {
                            selFrames.Remove(selQuad);

                            for (int i = 0; i < selFrames.Count; i++)
                            {
                                selFrames[i].selInd = i + 1;
                            }
                        }
                    }
                }

                /*
                float curWidth = 0f, curHeight = 0f;

                while(curHeight < prHeight)
                {
                    curWidth = 0f;
                    while(curWidth < prWidth)
                    {
                        draw.AddQuad(new Vector2(pos.X + curWidth, pos.Y + curHeight), new Vector2(pos.X + curWidth + cutPrWidth, pos.Y + curHeight), new Vector2(pos.X + cutPrWidth + curWidth, pos.Y + cutPrHeight + curHeight), new Vector2(pos.X + curWidth, pos.Y + cutPrHeight + curHeight), col, 0.1f);
                        curWidth += cutPrWidth;
                    }

                    curHeight += cutPrHeight;
                }*/

            }

            if (ImGui.Button("Close"))
                isActive = false;

            ImGui.End();
        }
    }
}
