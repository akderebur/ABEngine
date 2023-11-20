using System;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ImGuiNET;
using System.Numerics;
using static ABEngine.ABEditor.SpriteEditor;
using System.Collections.Generic;
using System.Linq;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABEditor.ComponentDrawers
{
	public static class SpriteAnimationDrawer
	{
        static Texture2D lastTex = null;
        static SpriteAnimation lastSprAnim;
        static Vector2 lastSpriteSize;

        static IntPtr imgPtr;

        static float cutPrWidth = 720;
        static float cutPrHeight = 720;
        static float lastCutPrWidth = 720;
        static float lastCutPrHeight = 720;

        static float prWidth = 720;
        static float prHeight = 720;

        static float widthMult;
        static float heightMult;

        static float borderPad = 1.3f;


        static uint greenCol, blueCol, whiteCol;

        static List<CutQuad> selFrames;
        static List<CutQuad> quads;

        static SpriteAnimationDrawer()
        {
            greenCol = ImGui.GetColorU32(new Vector4(0f, 0.7f, 0f, 1));
            blueCol = ImGui.GetColorU32(new Vector4(0f, 0f, 1f, 1));
            whiteCol = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1));

            selFrames = new List<CutQuad>();
            quads = new List<CutQuad>();

            lastSpriteSize = new Vector2(-1, -1);
        }

        private static void RefreshCutQuads(Texture2D texture, Vector2 cutSize, bool clear)
        {
            float curWidth = 0f, curHeight = 0f;
            float srcWidth = 0f, srcHeight = 0f;
            int colC = 0, rowC = 0;

            if(clear)
                quads.Clear();

            int quadId = 0;
            while (srcHeight < texture.imageSize.Y)
            {
                curWidth = 0f;
                srcWidth = 0f;
                rowC++;

                colC = 0;
                while (srcWidth < texture.imageSize.X)
                {
                    if (clear)
                        quads.Add(new CutQuad() { startX = curWidth, StartY = curHeight, srcStartX = srcWidth, srcStartY = srcHeight, quadId = quadId++ });
                    else
                    {
                        CutQuad quad = quads[quadId++];
                        quad.startX = curWidth;
                        quad.StartY = curHeight;
                    }

                    curWidth += cutPrWidth;
                    srcWidth += cutSize.X;
                    colC++;
                }

                curHeight += cutPrHeight;
                srcHeight += cutSize.Y;
            }
        }

        public static void Draw(SpriteAnimation sprAnim)
        {
            Texture2D tex = sprAnim.sprite.texture;
            bool reset = lastSprAnim != sprAnim;
            bool texReset = lastTex != tex && !reset;

            Vector2 checkSize = tex.spriteSize != Vector2.Zero ? tex.spriteSize : tex.imageSize;
            if (!texReset) // Sprite cut changed somehow
                reset |= lastSpriteSize != checkSize;

            // Recalculate preview
            prWidth = ImGui.GetWindowWidth();
            prHeight = tex.imageSize.Y * prWidth / tex.imageSize.X;

            widthMult = prWidth / tex.imageSize.X;
            heightMult = prHeight / tex.imageSize.Y;

            cutPrWidth = checkSize.X * widthMult;
            cutPrHeight = checkSize.Y * heightMult;


            if (reset || texReset)
            {
                selFrames.Clear();
                RefreshCutQuads(tex, checkSize, true);
                imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(tex.GetView());

                if (texReset)
                {
                    sprAnim.Refresh();
                    CutQuad quad = quads[0];
                    quad.selected = true;
                    quad.selInd = 1;
                    selFrames.Add(quad);
                }
                else
                {

                    List<int> toRemove = new List<int>();
                    // Load data from animation
                    int selInd = 0;
                    foreach (var spriteId in sprAnim.spriteIds)
                    {
                        if (spriteId >= quads.Count)
                        {
                            toRemove.Add(spriteId);
                            continue;
                        }

                        CutQuad quad = quads[spriteId];
                        quad.selected = true;
                        quad.selInd = ++selInd;
                        selFrames.Add(quad);
                    }

                    foreach (var spriteId in toRemove)
                        sprAnim.RemoveSpriteID(spriteId);
                }


                lastSpriteSize = checkSize;
                lastSprAnim = sprAnim;
                lastTex = sprAnim.sprite.texture;
            }

            if (lastCutPrWidth != cutPrWidth || lastCutPrHeight != cutPrHeight)
            {
                lastCutPrWidth = cutPrWidth;
                lastCutPrHeight = cutPrHeight;

                // Update cut quads without clear
                RefreshCutQuads(tex, checkSize, false);
            }


            if (imgPtr != null)
            {
                Vector2 pos = ImGui.GetCursorScreenPos();
                var draw = ImGui.GetWindowDrawList();

                ImGui.Image(imgPtr, new Vector2(prWidth, prHeight));

                Vector2 cutStep = new Vector2(cutPrWidth, cutPrHeight);
                float offset = 0f;
                while (offset < prWidth)
                {
                    draw.AddLine(new Vector2(pos.X + offset, pos.Y), new Vector2(pos.X + offset, pos.Y + prHeight), greenCol, 0.1f);
                    offset += cutStep.X;
                }

                offset = 0f;
                while (offset < prHeight)
                {
                    draw.AddLine(new Vector2(pos.X, pos.Y + offset), new Vector2(pos.X + prWidth, pos.Y + offset), greenCol, 0.1f);
                    offset += cutStep.Y;
                }

                foreach (var curQuad in selFrames)
                {
                    Vector2 startPos = pos + new Vector2(curQuad.startX, curQuad.StartY);
                    draw.AddQuad(startPos, new Vector2(startPos.X + cutPrWidth - borderPad, startPos.Y), new Vector2(startPos.X + cutPrWidth - borderPad, startPos.Y + cutPrHeight - borderPad), new Vector2(startPos.X, startPos.Y + cutPrHeight - borderPad), blueCol, 0.1f);
                    draw.AddText(new Vector2(startPos.X + cutPrWidth / 2f, startPos.Y + cutPrHeight / 2f), whiteCol, "" + curQuad.selInd);
                }

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    Vector2 mousePos = ImGui.GetMousePos();

                    if (mousePos.X > pos.X && mousePos.Y > pos.Y && mousePos.X < pos.X + prWidth && mousePos.Y < pos.Y + prHeight)
                    {
                        Vector2 normPos = mousePos - pos;
                        var selQuad = quads.LastOrDefault(q => normPos.X > q.startX && normPos.Y > q.StartY);
                        if (selQuad != null)
                        {
                            selQuad.selected = !selQuad.selected;

                            if (selQuad.selected)
                            {
                                int retId = sprAnim.AddSpriteID(selQuad.quadId);
                                if (retId != -1)
                                {
                                    selFrames.Add(selQuad);
                                    selQuad.selInd = selFrames.Count;
                                }
                                else
                                    selQuad.selected = false;
                            }
                            else
                            {
                                int retId = sprAnim.RemoveSpriteID(selQuad.quadId);
                                if (retId == -2) // Reset to 0
                                {
                                    foreach (var item in selFrames)
                                        item.selected = false;
                                    selFrames.Clear();
                                    selFrames.Add(quads.FirstOrDefault(q => q.quadId == 0));
                                }
                                else if (retId == -1) // Couldn't unselect
                                    selQuad.selected = true;
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
                    }
                }
            }

            bool playing = sprAnim.isPlaying;
            if (ImGui.Checkbox("Play in Editor", ref playing))
                sprAnim.isPlaying = playing;

          
        }
    }
}

