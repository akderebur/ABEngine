using System;
using ABEngine.ABERuntime;
using System.Numerics;
using ABEngine.ABERuntime.Components;
using ImGuiNET;
using Veldrid;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;
using static ABEngine.ABEditor.SpriteEditor;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Linq;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABEditor.ComponentDrawers
{
	public static class TilemapDrawer
	{
		static TilemapDrawer()
		{
            greenCol = ImGui.GetColorU32(new Vector4(0f, 0.7f, 0f, 1));
            blueCol = ImGui.GetColorU32(new Vector4(0f, 0f, 1f, 1));
            whiteCol = ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1));
        }

        internal static CollisionChunk selectedChunk;
        private static Vector3 clickCellPos;

        public static bool updateGrid;
        public static Vector2 tileSize = new Vector2(128, 128);

        static Entity cursorSprite;

        static Transform lastTilemapTrans;
        static Tilemap lastTilemap;
        static Texture2D texture2d;
        static Texture texture;
        static IntPtr imgPtr;
        static string spriteFilePath;
        static bool init = false;

        static float uvIndent = 0f;
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

        static int colC;
        static int rowC;

        static uint greenCol, blueCol, whiteCol;

        static float borderPad = 1.3f;

        static CutQuad curSelection;
        static CutQuad savedQuad;

        static Dictionary<Vector3, CutQuad> brushRecord;
        static bool recording = false;
        static bool recordLeave = false;
        static bool placeWithCol;

        static int layerIndex = 0;

        internal static event Action<CollisionChunk> onCollisionUpdate;

        internal static CollisionChunk AddCollision(Transform placeCell)
        {
            Vector3 placePos = placeCell.worldPosition;
            placePos.Z = 0.001f * layerIndex;
            return lastTilemap.AddCollision(placePos);
        }

        internal static CollisionChunk RemoveCollision(Transform placeCell)
        {
            Vector3 placePos = placeCell.worldPosition;
            placePos.Z = 0.001f * layerIndex;
            return lastTilemap.RemoveCollision(placePos);
        }

        public static void PlaceTile(Transform placeGrid, bool dupe)
        {
            if (lastTilemap == null)
                return;

            float layerZ = 0.001f * layerIndex;
            Vector3 placePos = placeGrid.worldPosition;
            placePos.Z = layerZ;
            Vector3 placePosRound = placePos.RoundTo2Dec();
            placePosRound.Z = layerZ;

            if (curSelection == null) // Chunk move
            {
                if(selectedChunk != null)
                {
                    Vector2 newPivot = Vector2.Zero;
                    bool updated = selectedChunk.UpdatePosition(clickCellPos, placePos, ref newPivot);
                    if (updated)
                    {
                        clickCellPos.X = newPivot.X;
                        clickCellPos.Y = newPivot.Y;

                        onCollisionUpdate?.Invoke(selectedChunk);
                    }
                }

                return;
            }

            // Painting - Place tile on cell
            int placeSprID = lastTilemap.GetPlacementSpriteID(placePosRound);
            if (placeSprID == curSelection.quadId)
            {
                return;
            }

            // Duplicate with multiple tile placements at once
            if (dupe)
            {
                if (brushRecord.Count < 1)
                    return;

                Vector3 oldPivot = brushRecord.ElementAt(0).Key;
                Vector3 newPivot = placePos;
                foreach (var frame in brushRecord)
                {
                    Vector3 localDif = frame.Key - oldPivot;
                    Vector3 newPos = newPivot + localDif;
                    CutQuad recQuad = frame.Value;

                    Sprite tileSprite = new Sprite(texture2d, texture2d.spriteSize);
                    tileSprite.SetSpriteID(recQuad.quadId);
                    tileSprite.SetUVIndent(uvIndent);

                    var tileSpriteEnt = EntityManager.CreateEntity("Tile", "TilemapTile", false);
                    tileSpriteEnt.transform.localPosition = newPos;
                    tileSpriteEnt.transform.localEulerAngles = cursorSprite.transform.localEulerAngles;
                    tileSpriteEnt.transform.parent = lastTilemapTrans;

                    tileSpriteEnt.Set<Sprite>(tileSprite);
                    tileSprite.renderLayerIndex = GraphicsManager.renderLayers.Count - 1;

                    lastTilemap.AddTile(tileSpriteEnt.transform, recQuad.quadId);

                    if (placeWithCol)
                    {
                        var chunk = lastTilemap.AddCollision(newPos);
                        onCollisionUpdate?.Invoke(chunk);
                    }
                }
                
            }
            else // Single tile placement
            {
                if (brushRecord.Count < 1 || recording || Input.GetKey(Key.R))
                {
                    if(!brushRecord.ContainsKey(placePosRound))
                        brushRecord.Add(placePosRound, curSelection);
                }

                Sprite tileSprite = new Sprite(texture2d, texture2d.spriteSize);
                tileSprite.SetSpriteID(curSelection.quadId);
                tileSprite.SetUVIndent(uvIndent);

                var tileSpriteEnt = EntityManager.CreateEntity("Tile", "TilemapTile", tileSprite, false);
                tileSpriteEnt.transform.localPosition = placePosRound;
                tileSpriteEnt.transform.localEulerAngles = cursorSprite.transform.localEulerAngles;
                tileSpriteEnt.transform.parent = lastTilemapTrans;

                //tileSpriteEnt.Set<Sprite>(tileSprite);
                tileSprite.renderLayerIndex = GraphicsManager.renderLayers.Count - 1;

                lastTilemap.AddTile(tileSpriteEnt.transform, curSelection.quadId);
                if (placeWithCol)
                {
                    var chunk = lastTilemap.AddCollision(placePos);
                    onCollisionUpdate?.Invoke(chunk);
                }
            }
        }

        public static void UpdateTile(Vector2 dir)
        {
            if (lastTilemap == null || curSelection == null)
                return;

            int dirX = (int)MathF.Round(dir.X);
            int dirY = (int)MathF.Round(dir.Y);

            curSelection.selected = false;

            int quadId = curSelection.quadId;
            if (dir.X > 0)
                quadId++;
            else if (dir.X < 0)
                quadId--;

            if (dir.Y > 0)
                quadId -= colC;
            else if (dir.Y < 0)
                quadId += colC;

            recording = false;
            var targQuad = quads.FirstOrDefault(q => q.quadId == quadId);
            if (targQuad != null && targQuad != curSelection)
            {
                recording = true;

                if (!Input.GetKey(Key.Space))
                {
                    curSelection.selected = false;
                    curSelection = targQuad;
                    curSelection.selected = true;
                    cursorSprite.SetEnabled(true);
                    cursorSprite.Get<Sprite>().SetSpriteID(curSelection.quadId);
                }
            }
        }

        public static void RemoveTile(Transform placeGrid)
        {
            if (lastTilemap == null)
                return;

            float layerZ = 0.001f * layerIndex;
            Vector3 placePosRound = placeGrid.worldPosition.RoundTo2Dec();
            placePosRound.Z = layerZ;

            lastTilemap.RemoveTile(placePosRound);
        }

        // Left click down
        public static void LeftClickDown(Transform placeCell, bool dupe)
        {
            if (curSelection != null) // Paint mode
            {
                savedQuad = curSelection;
                recording = false;
                if (!dupe && !recordLeave)
                    brushRecord.Clear();
            }
            else // Chunk select / duplicate
            {
                Vector3 placePos = placeCell.worldPosition;
                placePos.Z = 0.001f * layerIndex;
                clickCellPos = placePos;

                if(dupe && selectedChunk != null) // Dupe chunk
                {
                    List<Vector2> spawnPoses = selectedChunk.ValidateDuplicateSpawn(placePos);
                    if(spawnPoses != null) // Can spawn duplicate
                    {
                        for (int i = 0; i < spawnPoses.Count; i++)
                        {
                            Vector2 spawnPos = spawnPoses[i];
                            ChunkTile tile = selectedChunk.tiles.ElementAt(i).Value;

                            var entCopy = EntityManager.Instantiate(tile.spriteTrans.entity, null);
                            entCopy.transform.localPosition = spawnPos.ToVector3().RoundTo2Dec();

                            lastTilemap.AddTile(entCopy.transform, entCopy.Get<Sprite>().GetSpriteID());
                            var chunk = lastTilemap.AddCollision(spawnPos.ToVector3().RoundTo2Dec());
                            onCollisionUpdate?.Invoke(chunk);
                        }
                    }
                }

                var oldChunk = selectedChunk;
                selectedChunk = lastTilemap.GetSelectionChunk(placePos);
                if (selectedChunk != oldChunk) // New selection
                {
                    onCollisionUpdate?.Invoke(oldChunk);
                    onCollisionUpdate?.Invoke(selectedChunk);
                }
            }
        }

        // Left click up
        public static void LeftClickUp()
        {

            if (savedQuad != null && curSelection != savedQuad)
            {
                if(curSelection != null)
                    curSelection.selected = false;
                curSelection = savedQuad;
                curSelection.selected = true;
                cursorSprite.SetEnabled(true);
                cursorSprite.Get<Sprite>().SetSpriteID(curSelection.quadId);

                savedQuad = null;
            }

            if (Input.GetKey(Key.R))
                recordLeave = true;
            else
                recordLeave = false;
        }

        static void ResetState(bool cutSet = false)
        {
            brushRecord = new Dictionary<Vector3, CutQuad>();
            curSelection = null;

            uvIndent = 0f;
            if (!cutSet)
            {
                cutWidth = texture.Width;
                cutHeight = texture.Height;
            }
            lastCutWidth = 0;
            lastCutHeight = 0;

            quads.Clear();
            quads.Add(new CutQuad() { startX = 0, StartY = 0, quadId = 0 });

            if (cursorSprite.IsValid())
                cursorSprite.SetEnabled(false);
            updateGrid = true;
        }

        static List<CutQuad> quads = new List<CutQuad>();

        public static void RenderTilemap(Tilemap tilemap, Transform tilemapTrans)
        {
            // Reinit
            if (lastTilemap != tilemap)
            {
                Editor.GetTilemapGizmo().ResetGizmo();
                foreach (var chunk in tilemap.GetAllChunks())
                {
                    onCollisionUpdate?.Invoke(chunk);
                }

                var tmpTex2d = tilemap.tileImage;
                texture = tilemap.tileImage.texture;
                imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, texture);
               
                lastTilemap = tilemap;
                lastTilemapTrans = tilemapTrans;

                if(tmpTex2d != AssetCache.GetDefaultTexture())
                {
                    init = true;
                    texture2d = tmpTex2d;

                    if(texture2d.isSpriteSheet)
                    {
                        cutWidth = texture2d.spriteSize.X;
                        cutHeight = texture2d.spriteSize.Y;
                    }
                    else
                    {
                        cutWidth = texture2d.imageSize.X;
                        cutHeight = texture2d.imageSize.Y;
                    }

                    foreach (var tileSpr in tilemap.GetAllSprites())
                    {
                        tileSpr.entity.Get<Sprite>().renderLayerIndex = GraphicsManager.renderLayers.Count - 1;
                    }

                    if (!cursorSprite.IsValid())
                    {
                        cursorSprite = EntityManager.CreateEntity("TilemapCursor", "EditorNotVisible", new Sprite(texture2d));
                    }
                    else
                        cursorSprite.Get<Sprite>().SetTexture(texture2d);

                    cursorSprite.Get<Sprite>().Resize(new Vector2(cutWidth, cutHeight));

                    ResetState(true);
                }
                else
                    ResetState();
            }

            Sprite spr = null;
            if (cursorSprite.IsValid())
            {
                cursorSprite.transform.localPosition = Game.activeCam.worldPosition + new Vector3(ImGui.GetMousePos().MouseToZoomed().ToImGuiVector2().PixelToWorld(), 0.5f);
                spr = cursorSprite.Get<Sprite>();

                if (Input.GetKeyDown(Key.Q))
                    cursorSprite.transform.localEulerAngles -= Vector3.UnitZ * MathF.PI / 2f;
                else if (Input.GetKeyDown(Key.E))
                    cursorSprite.transform.localEulerAngles += Vector3.UnitZ * MathF.PI / 2f;

            }

            if (Input.GetKeyDown(Key.X)) // Selection. Remove brush
            {
                if (curSelection != null)
                    curSelection.selected = false;
                curSelection = null;
                if (cursorSprite.IsValid())
                {
                    cursorSprite.SetEnabled(false);
                }
            }

            // Recalculate preview
            prWidth = ImGui.GetWindowWidth();
            prHeight = texture.Height * prWidth / texture.Width;


            widthMult = prWidth / texture.Width;
            heightMult = prHeight / texture.Height;

            cutPrWidth = cutWidth * widthMult;
            cutPrHeight = cutHeight * heightMult;


            // Tilemap details
            ImGui.Text("Tilemap Image");
            ImGui.Spacing();

            ImGui.Text("Cell Size: ");
            ImGui.SameLine();
            ImGui.PushItemWidth(100f);
            ImGui.InputFloat("##CellX", ref cutWidth);
            ImGui.SameLine();
            ImGui.InputFloat("##CellY", ref cutHeight);
            ImGui.PopItemWidth();

            ImGui.Text("UV Indent: ");
            ImGui.PushItemWidth(100f);
            ImGui.InputFloat("##UVIndent", ref uvIndent);
            ImGui.PopItemWidth();

            ImGui.Text("Layer");
            ImGui.PushItemWidth(100f);
            if (ImGui.InputInt("##Layer", ref layerIndex))
                selectedChunk = null;
            ImGui.PopItemWidth();

            ImGui.Text("Chunk Brush");
            ImGui.SameLine();
            ImGui.Checkbox("##BrushCol", ref placeWithCol);

            if (lastCutHeight != cutHeight || lastCutWidth != cutWidth)
            {
                if (cutWidth < 1f)
                    cutWidth = 1f;
                if (cutHeight < 1f)
                    cutHeight = 1f;

                if (spr != null)
                {
                    texture2d.RetileTexture(new Vector2(cutWidth, cutHeight));
                    spr.Resize(new Vector2(cutWidth, cutHeight));
                }

                cutPrWidth = cutWidth * widthMult;
                cutPrHeight = cutHeight * heightMult;

                lastCutHeight = cutHeight;
                lastCutWidth = cutWidth;

                float curWidth = 0f, curHeight = 0f;
                float srcWidth = 0f, srcHeight = 0f;
                colC = 0; rowC = 0;

                quads.Clear();
                int quadId = 0;
                while (srcHeight < texture.Height)
                {
                    curWidth = 0f;
                    srcWidth = 0f;
                    rowC++;

                    colC = 0;
                    while (srcWidth < texture.Width)
                    {
                        quads.Add(new CutQuad() { startX = curWidth, StartY = curHeight, srcStartX = srcWidth, srcStartY = srcHeight, quadId = quadId++ });
                        //draw.AddQuad(new Vector2(pos.X + curWidth, pos.Y + curHeight), new Vector2(pos.X + curWidth + cutPrWidth, pos.Y + curHeight), new Vector2(pos.X + cutPrWidth + curWidth, pos.Y + cutPrHeight + curHeight), new Vector2(pos.X + curWidth, pos.Y + cutPrHeight + curHeight), col, 0.1f);
                        curWidth += cutPrWidth;
                        srcWidth += cutWidth;
                        colC++;
                    }

                    curHeight += cutPrHeight;
                    srcHeight += cutHeight;
                }
                updateGrid = true;
                tileSize = new Vector2(cutWidth, cutHeight);
            }

            if (imgPtr != null)
            {
                Vector2 pos = ImGui.GetCursorScreenPos();
                var draw = ImGui.GetWindowDrawList();

                //IntPtr imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, tilemap.tileImage.texture);
                ImGui.Image(imgPtr, new Vector2(prWidth, prHeight));

                foreach (var quad in quads) // Draw cut quads
                {

                    Vector2 startPos = pos + new Vector2(quad.startX, quad.StartY);
                    draw.AddQuad(startPos, new Vector2(startPos.X + cutPrWidth - borderPad, startPos.Y), new Vector2(startPos.X + cutPrWidth - borderPad, startPos.Y + cutPrHeight - borderPad), new Vector2(startPos.X, startPos.Y + cutPrHeight - borderPad), quad.selected ? blueCol : greenCol, 0.0001f);
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
                            if (curSelection != null)
                                curSelection.selected = false;

                            // First time using this texture
                            if (!init)
                            {
                                if (string.IsNullOrEmpty(spriteFilePath))
                                    return;

                                init = true;
                
                                TextureMeta texMeta = AssetHandler.GetMeta(spriteFilePath) as TextureMeta;
                                texture2d = AssetHandler.GetAssetBinding(texMeta, spriteFilePath) as Texture2D;
                                tilemap.tileImage = texture2d;
                                texture = texture2d.texture;

                                texture2d.RetileTexture(new Vector2(cutWidth, cutHeight));

                                if (!cursorSprite.IsValid())
                                {
                                    cursorSprite = EntityManager.CreateEntity("TilemapCursor", "EditorNotVisible", new Sprite(texture2d));
                                }
                                else
                                    cursorSprite.Get<Sprite>().SetTexture(texture2d);

                                cursorSprite.Get<Sprite>().Resize(new Vector2(cutWidth, cutHeight));
                            }

                            curSelection = selQuad;
                            curSelection.selected = true;
                            cursorSprite.SetEnabled(true);
                            cursorSprite.Get<Sprite>().SetSpriteID(curSelection.quadId);
                            cursorSprite.transform.localEulerAngles = Vector3.Zero;
                        }
                    }
                }
            }

            CheckTilemapTextureDrop(tilemap);

            // Selected chunk info
            if(selectedChunk != null)
            {
                ImGui.Spacing();
                if (ImGui.CollapsingHeader("Chunk"))
                {
                    ImGui.Text("Tile Count: ");
                    ImGui.SameLine();
                    ImGui.Text("" + selectedChunk.tiles.Count);
                }

                if(Input.GetKey(Key.ControlLeft) && Input.GetKeyDown(Key.X))
                {
                    lastTilemap.DeleteChunk(selectedChunk);
                    onCollisionUpdate?.Invoke(selectedChunk);
                    selectedChunk = null;
                }
            }
        }

        // Drag drop image
        unsafe static void CheckTilemapTextureDrop(Tilemap tilemap)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("SpriteFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    spriteFilePath = AssetsFolderView.files[srcIndex];

                    texture = AssetCache.GetTextureDebug(Game.AssetPath + spriteFilePath);
                    imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, texture);
                    init = false;
                    ResetState();

                    //TextureMeta texMeta = AssetHandler.GetMeta(spriteFilePath) as TextureMeta;
                    //Texture2D texture = AssetHandler.GetTextureBinding(texMeta, spriteFilePath);
                    //tilemap.tileImage = texture;
                }

                ImGui.EndDragDropTarget();
            }
        }
    }
}

