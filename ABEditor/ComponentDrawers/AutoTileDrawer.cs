using System;
using System.Numerics;
using System.Collections.Generic;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ImGuiNET;
using ABEngine.ABEditor.TilemapExtension;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;
using System.IO;
using Newtonsoft.Json;
using ABEngine.ABERuntime.ECS.Internal;
using Veldrid;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABEditor.ComponentDrawers
{
	public static class AutoTileDrawer
	{
		private static Tilemap tilemap;
		static bool open;

		static List<AutoTile> autoTiles = new List<AutoTile>();
        static Vector4 defaultColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Button];

        public static AutoTile selectedAutoTile = null;

        static IntPtr imgPtr = IntPtr.Zero;
        static int selectedImgId = -2;


        public static void SetTilemap(Tilemap newTilemap)
		{
            open = true;

            if (tilemap != newTilemap)
			{
                selectedAutoTile = null;
				SaveConfiguration();
                tilemap = newTilemap;

                imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, tilemap.tileImage.texture);

                // Load auto tile configuration
                autoTiles.Clear();

				AssetMeta meta = AssetHandler.GetMeta(tilemap.tileImage.fPathHash);
				string autotileFile = Editor.EditorAssetPath + "Tilemap/Rules/" + meta.uniqueID.ToString() + ".json";
				if(File.Exists(autotileFile))
				{
					string json = File.ReadAllText(autotileFile);
                    autoTiles = JsonConvert.DeserializeObject<List<AutoTile>>(json);
                }
            }
        }

        public static void SetSprite(int spriteId)
        {
            if (selectedAutoTile == null)
                return;

            if (selectedImgId == -1)
                selectedAutoTile.defaultSpriteID = spriteId;
            else if(selectedImgId >= 0)
            {
                if(selectedImgId > selectedAutoTile.tileRules.Count)
                {
                    selectedImgId = -2;
                    return;
                }

                selectedAutoTile.tileRules[selectedImgId].spriteID = spriteId;
            }
        }

        static void ImageWithBorder(int spriteID, int itemID)
        {
            if (imgPtr != IntPtr.Zero)
            {
                bool selected = selectedImgId == itemID;
                Vector2 uvPos = tilemap.tileImage[spriteID];
                uvPos /= tilemap.tileImage.imageSize;
                Vector2 uvScale = tilemap.tileImage.spriteSize / tilemap.tileImage.imageSize;

                if (selected)
                {
                    ImGui.BeginChild("imageWithBorder", new Vector2(64, 64), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                    ImGui.Image(imgPtr, new Vector2(64, 64), uvPos, uvPos + uvScale);
                    ImGui.EndChild();
                }
                else
                    ImGui.Image(imgPtr, new Vector2(64, 64), uvPos, uvPos + uvScale);

                if(ImGui.IsItemClicked())
                {
                    if (selected)
                        selectedImgId = -2;
                    else
                        selectedImgId = itemID;
                }
            }
        }

		static void SaveConfiguration()
		{
			if (tilemap == null)
				return;

            AssetMeta meta = AssetHandler.GetMeta(tilemap.tileImage.fPathHash);
            string autotileFile = Editor.EditorAssetPath + "Tilemap/Rules/" + meta.uniqueID.ToString() + ".json";
			string json = JsonConvert.SerializeObject(autoTiles, Newtonsoft.Json.Formatting.Indented);
			File.WriteAllText(autotileFile, json);
        }

        public static void Draw()
        {
            if (!open)
                return;

            ImGui.Begin("Auto Tiles", ref open, ImGuiWindowFlags.None);

            if (ImGui.Button("Add Auto Tile"))
                autoTiles.Add(new AutoTile());

            for (int t = 0; t < autoTiles.Count; t++)
            {
                AutoTile autoTile = autoTiles[t];
                bool isHeaderSelected = selectedAutoTile == autoTile;

                ImGui.GetStateStorage().SetInt(ImGui.GetID("Tile " + t), 1);
                if (ImGui.CollapsingHeader("Tile " + t))
                {
                    if (isHeaderSelected)
                    {
                        ImageWithBorder(autoTile.defaultSpriteID, -1);
                        ImGui.SameLine();
                        ImGui.InputInt($"Sprite ID##auto{t}", ref autoTile.defaultSpriteID);
                        ImGui.Separator();

                        if (ImGui.Button("Add Rule"))
                            autoTile.tileRules.Add(new TileRule());

                        for (int index = 0; index < autoTile.tileRules.Count; index++)
                        {
                            var entry = autoTile.tileRules[index];
                            ImGui.PushID(index); // Push unique ID

                            ImageWithBorder(entry.spriteID, index);
                            ImGui.SameLine();
                            ImGui.InputInt("Sprite ID", ref entry.spriteID);

                            for (int i = 0; i < 3; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    Vector4 color;
                                    if (entry.Grid[i][j] == 1)
                                    {
                                        color = new Vector4(0, 1, 0, 1); // Green
                                    }
                                    else if (entry.Grid[i][j] == 2)
                                    {
                                        color = new Vector4(1, 0, 0, 1); // Red
                                    }
                                    else
                                    {
                                        color = defaultColor; // Default color
                                    }

                                    ImGui.PushStyleColor(ImGuiCol.Button, color);
                                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
                                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);

                                    if (ImGui.Button($"##Square{i}{j}", new Vector2(30, 30)))
                                    {
                                        entry.Grid[i][j] = (entry.Grid[i][j] + 1) % 3;
                                    }

                                    ImGui.PopStyleColor(3); // Pop colors

                                    if (j < 2)
                                    {
                                        ImGui.SameLine();
                                    }
                                }
                            }

                            ImGui.PopID();
                        }
                    }
                }

                if (ImGui.IsItemClicked())
                    selectedAutoTile = autoTile;

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }

            ImGui.End();


            if (!open)
            {
                // Closed now
                SaveConfiguration();
                selectedAutoTile = null;
                selectedImgId = -2;
            }
        }
	}
}

