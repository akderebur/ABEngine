using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;
using ABEngine.ABEditor.ComponentDrawers;
using ABEngine.ABEditor.ImGuiPlugins;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.Components;
using Veldrid;
using ImGuiNET;
using ABEngine.ABEditor.PropertyDrawers;
using Arch.Core;
using Arch.Core.Extensions;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABEditor
{
    enum FileDialogType
    {
        None,
        OpenFolder,
        OpenFile,
        SaveFile,
    }

  
    internal partial class Editor : Game
	{
        static FileDialogType fileDialogType;
        static FilePicker picker;
        static bool tilemapSelected = false;
        static string loadedScenePath = null;

        private void UpdateEditorUI()
        {
            ImGui.PushFont(defaultFont);
            if (!isPlaying)
            {
                MainMenu();

                

                //ImGui.SetNextWindowSize(new Vector2(400, 300), ImGuiCond.Appearing);
                //ImGui.SetNextWindowPos(new Vector2(screenSize.X / 2f - 200, screenSize.Y / 2f - 150), ImGuiCond.Appearing);
                //ImGui.Begin("Bezier Curve Editor", ImGuiWindowFlags.NoSavedSettings);

                //Vector2 canvasSize = ImGui.GetContentRegionAvail() - new Vector2(20, 20);
                //Vector2 canvasPos = ImGui.GetCursorScreenPos();

                //// Draw the background grid
                //for (int i = 0; i <= 10; i++)
                //{
                //    float x = canvasPos.X + i * (canvasSize.X / 10);
                //    float y = canvasPos.Y + i * (canvasSize.Y / 10);
                //    ImGui.GetWindowDrawList().AddLine(new Vector2(x, canvasPos.Y), new Vector2(x, canvasPos.Y + canvasSize.Y), ImGui.GetColorU32(ImGuiCol.Border));
                //    ImGui.GetWindowDrawList().AddLine(new Vector2(canvasPos.X, y), new Vector2(canvasPos.X + canvasSize.X, y), ImGui.GetColorU32(ImGuiCol.Border));
                //}

                //// Draw labels for the x-axis
                //ImGui.GetWindowDrawList().AddText(new Vector2(ToCanvas(startPoint).X - 5, ToCanvas(new Vector2(0, 0)).Y + 5), ImGui.GetColorU32(ImGuiCol.Text), "0");
                //ImGui.GetWindowDrawList().AddText(new Vector2(ToCanvas(endPoint).X - 5, ToCanvas(new Vector2(0, 0)).Y + 5), ImGui.GetColorU32(ImGuiCol.Text), "1");

                //// Draw labels for the y-axis
                //ImGui.GetWindowDrawList().AddText(new Vector2(ToCanvas(startPoint).X - 5, ToCanvas(new Vector2(0, 1)).Y - 5), ImGui.GetColorU32(ImGuiCol.Text), "1");

                //Vector2 ToCanvas(Vector2 point) => new Vector2(canvasPos.X + point.X * canvasSize.X, canvasPos.Y + (1f - point.Y) * canvasSize.Y);
                //Vector2 FromCanvas(Vector2 point) => new Vector2((point.X - canvasPos.X) / canvasSize.X, 1f - (point.Y - canvasPos.Y) / canvasSize.Y);

                //// Draw the Bezier curve
                //ImGui.GetWindowDrawList().AddBezierCubic(
                //    ToCanvas(startPoint),
                //    ToCanvas(controlPoint1),
                //    ToCanvas(controlPoint2),
                //    ToCanvas(endPoint),
                //    ImGui.GetColorU32(ImGuiCol.PlotLines),
                //    2
                //);

                //// Draw and handle interaction for the start, end, and control points
                //Vector2[] points = new[] { startPoint, endPoint, controlPoint1, controlPoint2 };
                //for (int i = 0; i < points.Length; i++)
                //{
                //    Vector2 screenPoint = ToCanvas(points[i]);
                //    ImGui.SetCursorScreenPos(screenPoint - new Vector2(4, 4));
                //    ImGui.InvisibleButton($"point{i}", new Vector2(12, 12));

                //    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                //    {
                //        Vector2 newPosition = FromCanvas(ImGui.GetIO().MousePos);

                //        if (i < 2) // start and end points
                //        {
                //            newPosition.X = points[i].X; // Restrict movement to vertical only
                //        }
                //        newPosition.X = Math.Clamp(newPosition.X, 0, 1);
                //        newPosition.Y = Math.Clamp(newPosition.Y, 0, 1);
                //        points[i] = newPosition;
                //    }

                //    ImGui.GetWindowDrawList().AddCircleFilled(screenPoint, 4, ImGui.GetColorU32(ImGuiCol.PlotLinesHovered), 12);
                //}
                //startPoint = points[0];
                //endPoint = points[1];
                //controlPoint1 = points[2];
                //controlPoint2 = points[3];



                //ImGui.End();

                // End  test

                // Individual Editors
                AssetsFolderView.Draw();

                SpriteEditor.Draw();

                // Hierarchy
                RenderHierarchy();

                // Recalculate Tilemap Grid
                RecalculateGrid();

                // Tilemap Grid
                RenderGrid();



                // Details - Scene entity
                var selectedEntity = Editor.selectedEntity;
                if (selectedEntity != Entity.Null && selectedEntity.IsAlive())
                {
                    ImGui.Begin("Details");
                    DetailsView(selectedEntity);

                    ImGui.Separator();
                    ImGui.Spacing();

                    DetailsAddComponentMenu(selectedEntity);

                    ImGui.Separator();
                    ImGui.Spacing();

                    DetailMakePrefabMenu(selectedEntity);

                    ImGui.Spacing();
                    //ImGui.Separator();
                    //ImGui.Spacing();

                    //DeleteEntityButton(selectedEntity);

                    ImGui.End();
                }
                else
                {
                    // Details - selected asset
                    AssetDetails();
                }
            }
        }


        PipelineMaterial editMat = null;
        string editMatPath = null;

        void AssetDetails()
        {
            string selectedAsset = Editor.selectedAsset;
            if (!string.IsNullOrEmpty(selectedAsset))
            {
                var meta = AssetHandler.GetMeta(selectedAsset);
                if (meta != null)
                {
                    var metaType = meta.GetType();
                    if (metaType.Equals(typeof(TextureMeta)))
                    {
                        TextureMeta texMeta = meta as TextureMeta;
                        var editTex = AssetHandler.GetAssetBinding(texMeta) as Texture2D;
                        ImGui.Begin("Details");

                        Vector2 spriteSize = texMeta.spriteSize;
                        if (ImGui.InputFloat2("Sprite Size", ref spriteSize))
                            texMeta.spriteSize = spriteSize;

                        if (ImGui.BeginCombo("Sampler", texMeta.sampler.Name))
                        {
                            for (int st = 0; st < GraphicsManager.AllSamplers.Count; st++)
                            {
                                Veldrid.Sampler curSampler = GraphicsManager.AllSamplers[st];
                                bool is_selected = texMeta.sampler == curSampler;
                                if (ImGui.Selectable(curSampler.Name, is_selected))
                                    texMeta.sampler = curSampler;
                                if (is_selected)
                                    ImGui.SetItemDefaultFocus();
                            }

                            ImGui.EndCombo();
                        }

                        if (ImGui.Button("Save"))
                        {
                            texMeta.RefreshAsset();
                            AssetHandler.SaveMeta(texMeta);
                        }
                        ImGui.End();
                    }
                    else if (metaType.Equals(typeof(MaterialMeta)))
                    {

                        MaterialMeta matMeta = meta as MaterialMeta;
                        if (editMatPath != selectedAsset)
                        {
                            editMatPath = selectedAsset;
                            editMat = AssetHandler.GetAssetBinding(matMeta) as PipelineMaterial;
                            editMat.name = Path.GetFileNameWithoutExtension(selectedAsset);
                        }

                        // Reset vals
                        bool changed = false;
                        matMeta.changedPropName = null;
                        matMeta.changedPipeline = null;

                        ImGui.Begin("Details");

                        ImGui.Text(matMeta.pipelineAsset.ToString());

                        ImGui.Text("Pipeline Asset");
                        ImGui.SameLine();
                        if (ImGui.BeginCombo("##Pipeline", matMeta.pipelineAsset.ToString()))
                        {
                            foreach (var pipelineKV in GraphicsManager.pipelineAssets)
                            {
                                bool is_selected = matMeta.pipelineAsset == pipelineKV.Value;
                                if (ImGui.Selectable(pipelineKV.Value.ToString(), is_selected))
                                {
                                    matMeta.pipelineAsset = pipelineKV.Value;
                                    matMeta.changedPipeline = pipelineKV.Value;
                                    changed = true;
                                }
                                if (is_selected)
                                    ImGui.SetItemDefaultFocus();
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.Spacing();
                        ImGui.Text("Properties");
                        ImGui.Separator();

                        var propNames = editMat.pipelineAsset.GetPropNames();
                        for (int i = 0; i < propNames.Count; i++)
                        {
                            string propName = propNames[i];
                            ShaderProp prop = editMat.shaderProps[i];

                            ImGui.Text(propName);
                            ImGui.SameLine();

                            bool locChange = false;
                            if(prop.SizeInBytes == 4) // Float
                            {
                                if (ImGui.InputFloat("##" + propName, ref prop.Float1))
                                    locChange = true;
                            }
                            else if (prop.SizeInBytes == 8) // Vector2
                            {
                                if (ImGui.InputFloat2("##" + propName, ref prop.Float2))
                                    locChange = true;
                            }
                            else if (prop.SizeInBytes == 12) // Vector3
                            {
                                if (ImGui.InputFloat3("##" + propName, ref prop.Float3))
                                    locChange = true;
                            }
                            else if (prop.SizeInBytes == 16) // Vector4
                            {
                                if (ImGui.InputFloat4("##" + propName, ref prop.Float4))
                                    locChange = true;
                            }

                            if (locChange && !changed)
                            {
                                changed = true;
                                matMeta.changedData = prop.Float4;
                                matMeta.changedPropName = propName;
                            }

                        }

                        var texNames = editMat.pipelineAsset.GetTextureNames();
                        for (int i = 0; i < texNames.Count; i++)
                        {
                            string texName = texNames[i];
                            if (texName.Equals("DepthTex") || texName.Equals("CamNormalTex") || texName.Equals("ScreenTex"))
                                continue;

                            ImGui.Text(texName);
                            Texture rawTex = editMat.GetRawTexture(texName);
                            IntPtr imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, rawTex);
                            ImGui.Image(imgPtr, new Vector2(100f, 100f));
                            CheckTextureDropMaterial(editMat, texName);
                        }

                        ImGui.Text("Late Render");
                        ImGui.SameLine();
                        ImGui.Checkbox("##LateRender", ref editMat.isLateRender);

                        if (changed)
                            matMeta.RefreshAsset();
                        
                        if (ImGui.Button("Save"))
                        {
                            AssetHandler.SaveMeta(matMeta);
                        }
                        ImGui.End();
                    }
                }
            }
        }

        static unsafe void CheckTextureDropMaterial(PipelineMaterial mat, string texName)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("SpriteFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var spriteFilePath = AssetsFolderView.files[srcIndex];

                    TextureMeta texMeta = AssetHandler.GetMeta(spriteFilePath) as TextureMeta;
                    Texture2D texture = AssetHandler.GetAssetBinding(texMeta) as Texture2D;

                    mat.SetTexture(texName, texture);
                }

                ImGui.EndDragDropTarget();
            }
        }

        Transform lastPlaced;
        Transform lastCell;
        List<Transform> gridCells = new List<Transform>();
        bool tilemapCollision = false;

        void RenderGrid()
        {
            if (!tilemapSelected || gridTrans == null || Game.activeCam == null)
                return;

            if (lastCell != null)
                lastCell.entity.Get<Sprite>().tintColor = new Vector4(1f, 1f, 1f, 0.3f);

            tilemapCollision = Input.GetKey(Key.C);
            Vector2 worldMouse = Input.GetMousePosition().ScreenToWorld().ToVector2();
            var selCell = gridCells.OrderBy(c => Vector2.Distance(c.worldPosition.ToVector2(), worldMouse)).First();
            if (selCell != null)
            {
                selCell.entity.Get<Sprite>().tintColor = RgbaFloat.Green.ToVector4();

                if (!ImGui.GetIO().WantCaptureMouse)
                {
                    if (Input.GetMouseButtonDown(MouseButton.Left) && lastPlaced == null)
                    {
                        // Add collision
                        if(!tilemapCollision)
                        {
                            lastPlaced = selCell;
                            TilemapDrawer.LeftClickDown(selCell, Input.GetKey(Key.Z));
                        }
                    }
                    else if (Input.GetMouseButton(MouseButton.Left))
                    {
                        if (tilemapCollision)
                        {
                            var chunk = TilemapDrawer.AddCollision(selCell);
                            if (chunk != null)
                                TMColliderGizmo.UpdateChunk(chunk);
                        }
                        else
                        {
                            bool dupe = false;
                            if (Input.GetKey(Key.ShiftLeft) && selCell != lastPlaced) // Update cell, region brush
                            {
                                Vector3 dir = selCell.worldPosition - lastPlaced.worldPosition;
                                Vector2 normDir = (dir / Vector3.Abs(dir)).ToVector2();
                                TilemapDrawer.UpdateTile(normDir);
                                lastPlaced = selCell;
                            }
                            else if (Input.GetKey(Key.Z)) // Duplicate Brush
                                dupe = true;

                            TilemapDrawer.PlaceTile(selCell, dupe);
                        }
                    }
                    else if (Input.GetMouseButtonUp(MouseButton.Left))
                    {
                        if (!tilemapCollision)
                            TilemapDrawer.LeftClickUp();

                        lastPlaced = null;
                    }
                    else if (Input.GetMouseButton(MouseButton.Right))
                    {
                        if (tilemapCollision)
                        {
                            var chunk = TilemapDrawer.RemoveCollision(selCell);
                            if (chunk != null)
                                TMColliderGizmo.UpdateChunk(chunk);
                        }
                        else
                            TilemapDrawer.RemoveTile(selCell);
                    }
                }
            }
            lastCell = selCell;

            Vector3 camPos = Game.activeCam.worldPosition;
            float xRound = MathF.Floor(camPos.X / worldOffset.X) * worldOffset.X - 30 * worldOffset.X;
            float yRound = MathF.Floor(camPos.Y / worldOffset.Y) * worldOffset.Y - 20 * worldOffset.Y;

            Vector3 offset = new Vector3(3, 0, 0f);
            Vector3 endPos = new Vector3(xRound, yRound, 0f);
            gridTrans.localPosition = endPos;
        }

        Transform gridTrans;
        Vector2 worldOffset;

        void RecalculateGrid()
        {
            if(TilemapDrawer.updateGrid)
            {
                TilemapDrawer.updateGrid = false;
                Texture2D gridTex = EditorAssetCache.GetGridTexture();

                worldOffset = TilemapDrawer.tileSize.PixelToWorld();

                Vector2 cellScale = worldOffset / gridTex.imageSize.PixelToWorld();
                Vector2 curPos = new Vector2(0f, worldOffset.Y / 2f);
                Vector2 curLinePos = new Vector2(0f, 0f);

                int childId = 0;
                for (int i = 0; i < 40; i++)
                {
                    // Horizontal Line
                    gridTrans.children[childId].localPosition = new Vector3(5f, curLinePos.Y, 0.2f);
                    childId++;

                    for (int j = 0; j < 60; j++)
                    {
                        if (i == 0)
                        {
                            // Vertical Line
                            curLinePos.X = j * worldOffset.X;
                            gridTrans.children[childId].localPosition = new Vector3(curLinePos.X, 0f, 0.3f);
                            childId++;
                        }

                        curPos.X = j * worldOffset.X + worldOffset.X / 2f;
                        gridTrans.children[childId].localScale = new Vector3(cellScale.X, cellScale.Y, 1f);
                        gridTrans.children[childId].localPosition = new Vector3(curPos.X, curPos.Y, 0.1f);

                        childId++;
                    }

                    curPos.Y += worldOffset.Y;
                    curLinePos.Y += worldOffset.Y;
                }
            }
        }

        void RemakeGrid(bool makeTileMap = false)
        {
            tilemapSelected = false;
            gridTrans = null;
            lastCell = null;
            lastPlaced = null;
            gridCells = new List<Transform>();

            // Grid
            if(makeTileMap)
                EntityManager.CreateEntity("Tilemap", "NoChild", new Tilemap());
            Entity gridEnt = EntityManager.CreateEntity("Grid", "EditorNotVisible");
            //gridEnt.transform.parent = tileEnt.transform;
            gridTrans = gridEnt.Get<Transform>();

            GraphicsManager.AddRenderLayer("Editor");
            int lastLayer = GraphicsManager.renderLayers.Count - 1;
            GraphicsManager.AddRenderLayer("Tilemap");

            Texture2D gridTex = EditorAssetCache.GetGridTexture();
            Texture2D lineTex = EditorAssetCache.GetGridLineTexture();

            worldOffset = gridTex.imageSize.PixelToWorld();
            worldOffset /= 2f;

            Vector2 cellScale = worldOffset / gridTex.imageSize.PixelToWorld();
            Vector2 curPos = new Vector2(0f, worldOffset.Y / 2f);
            Vector2 curLinePos = new Vector2(0f, 0f);


            for (int i = 0; i < 40; i++)
            {
                // Horizontal Line
                Sprite lineSpr = new Sprite(lineTex);
                lineSpr.tintColor = new Vector4(1f, 1f, 1f, 0.3f);
                Entity lineEnt = EntityManager.CreateEntity("Line_" + i, "EditorGridLine", lineSpr);
                lineSpr.renderLayerIndex = 1;
                lineEnt.Get<Transform>().localScale = new Vector3(1, 1f, 1f);
                lineEnt.Get<Transform>().localPosition = new Vector3(5f, curLinePos.Y, 0.2f);
                lineEnt.Get<Transform>().parent = gridEnt.Get<Transform>();

                for (int j = 0; j < 60; j++)
                {
                    if (i == 0)
                    {
                        // Vertical Line
                        curLinePos.X = j * worldOffset.X;

                        lineSpr = new Sprite(lineTex);
                        lineSpr.tintColor = new Vector4(1f, 1f, 1f, 0.3f);
                        lineEnt = EntityManager.CreateEntity("Line_" + i, "EditorGridLine", lineSpr);
                        lineSpr.renderLayerIndex = 1;
                        lineEnt.Get<Transform>().localScale = new Vector3(1f, 1f, 1f);
                        lineEnt.Get<Transform>().localEulerAngles = new Vector3(0f, 0f, MathF.PI / 2f); 
                        lineEnt.Get<Transform>().localPosition = new Vector3(curLinePos.X, 0f, 0.3f);
                        lineEnt.Get<Transform>().parent = gridEnt.Get<Transform>();
                    }

                    curPos.X = j * worldOffset.X + worldOffset.X / 2f;
                    Sprite cellSpr = new Sprite(gridTex);
                    cellSpr.tintColor = new Vector4(1f, 1f, 1f, 0.3f);
                    Entity cellEnt = EntityManager.CreateEntity("Cell_" + i + "_" + j, "EditorGridCell", cellSpr);
                    cellSpr.renderLayerIndex = 1;
                    cellEnt.Get<Transform>().localScale = new Vector3(cellScale.X, cellScale.Y, 1f);
                    cellEnt.Get<Transform>().localPosition = new Vector3(curPos.X, curPos.Y, 0.1f);
                    cellEnt.Get<Transform>().parent = gridEnt.Get<Transform>();
                    gridCells.Add(cellEnt.Get<Transform>());
                }

                curPos.Y += worldOffset.Y;
                curLinePos.Y += worldOffset.Y;
            }

            gridEnt.Get<Transform>().enabled = false;
        }

        private void MainMenu()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open Project"))
                    {
                        picker = FilePicker.GetFolderPicker(this, Path.GetPathRoot(Directory.GetCurrentDirectory()));
                        fileDialogType = FileDialogType.OpenFolder;
                    }
                    else if (ImGui.MenuItem("New Scene", isGameOpen))
                    {
                        Editor.selectedAsset = null;
                        Editor.selectedEntity = Entity.Null;

                        ResetWorld();
                        AssetCache.ClearSceneCache();
                        AssetHandler.ResetScene();

                        canvas = new Canvas(screenSize.X, screenSize.Y);
                        canvas.isDynamicSize = false;

                        GameWorld.Create("Canvas", Guid.NewGuid(), new Transform(), canvas);

                        var camEnt = GameWorld.Create("Camera", Guid.NewGuid(), new Transform(), new Camera());
                        activeCam = camEnt.Get<Transform>();

                        TMColliderGizmo.ResetGizmo();
                        spriteBatchSystem.Start();
                        RemakeGrid();

                        DepthSearch();
                    }
                    else if (ImGui.MenuItem("Load Scene", isGameOpen))
                    {
                        picker = FilePicker.GetFilePicker(this, Game.AssetPath, ".abscene");
                        fileDialogType = FileDialogType.OpenFile;
                    }
                    else if (ImGui.MenuItem("Save Scene", isGameOpen))
                    {
                        picker = FilePicker.GetFileSaver(this, Game.AssetPath);
                        fileDialogType = FileDialogType.SaveFile;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            if (fileDialogType == FileDialogType.OpenFolder)
            {
                ImGui.Begin("Load Project");

                int result = picker.Draw();
                if (result == 1)
                {
                    gameDir = picker.SelectedFile;
                    fileDialogType = FileDialogType.None;
                    OpenGameDummy(gameDir);
                }
                else if (result == 0)
                    fileDialogType = FileDialogType.None;

                ImGui.End();
            }
            else if (fileDialogType == FileDialogType.OpenFile)
            {
                ImGui.Begin("Load Scene");

                int result = picker.Draw();
                if (result == 1)
                {
                    string sceneFile = picker.SelectedFile;
                    loadedScenePath = sceneFile;
                    fileDialogType = FileDialogType.None;

                    AssetCache.DisposeResources();

                    ResetWorld();
                    AssetCache.ClearSceneCache();
                    AssetHandler.ResetScene();

                    TMColliderGizmo.ResetGizmo();
                    LoadScene(File.ReadAllText(sceneFile));

                    spriteBatchSystem.Start();

                    RemakeGrid();
                    DepthSearch();

                }
                else if (result == 0)
                    fileDialogType = FileDialogType.None;

                ImGui.End();
            }
            else if(fileDialogType == FileDialogType.SaveFile)
            {
                ImGui.Begin("Save Scene");

                int result = picker.Draw();
                if (result == 1)
                {
                    string saveFile = picker.SelectedFile;
                    fileDialogType = FileDialogType.None;
                    Console.WriteLine(saveFile);
                    File.WriteAllText(saveFile, SaveScene());
                }
                else if (result == 0)
                    fileDialogType = FileDialogType.None;

                ImGui.End();
            }
        }

        Transform lastDetailsView;
        bool firstGradientOpen = false;
        void DetailsView(in Entity selectedEntity)
        {
            if(!selectedEntity.IsAlive())
                return;
           
            string entName = selectedEntity.Get<string>();
            ImGui.InputText("Name", ref entName, 200);
            if (entName != selectedEntity.Get<string>())
                selectedEntity.Set<string>(entName);

            Transform transform = selectedEntity.Get<Transform>();
            bool newSelection = transform != lastDetailsView;
            lastDetailsView = transform;

            if (newSelection)
                firstGradientOpen = false;

            ImGui.GetStateStorage().SetInt(ImGui.GetID("Transform"), 1);

            if (ImGui.CollapsingHeader("Transform"))
            {
                bool enabled = transform.enabled;
                Vector3 pos = transform.localPosition;
                Vector3 eulRot = transform.localEulerAngles * (360f / (MathF.PI * 2f));
                Vector3 sca = transform.localScale;

                if (ImGui.Checkbox("Enabled", ref enabled))
                    transform.enabled = enabled;

                if (ImGui.InputFloat3("Position", ref pos))
                    EditorActions.UpdateProperty(transform.localPosition, pos, transform, nameof(transform.localPosition));
                if (ImGui.InputFloat3("Rotation", ref eulRot))
                {
                    Vector3 radians = eulRot * ((MathF.PI * 2f) / 360f);
                    EditorActions.UpdateProperty(transform.localEulerAngles, radians, transform, nameof(transform.localEulerAngles));
                }

                if(ImGui.InputFloat3("Scale", ref sca))
                    EditorActions.UpdateProperty(transform.localScale, sca, transform, nameof(transform.localScale));
            }

            bool hasTilemap = false;
            var comps = selectedEntity.GetAllComponents();
            var types = selectedEntity.GetComponentTypes();

            for (int i = 0; i < comps.Length; i++)
            {
                Type type = types[i].Type;
                var comp = comps[i];

                if (type.IsSubclassOf(typeof(ABComponent)))
                {
                    ImGui.GetStateStorage().SetInt(ImGui.GetID(type.Name), 1);

                    if (ImGui.CollapsingHeader(type.Name))
                    {
                        RenderPropertyEditors(type, comp);
                    }
                }
                else if (type == typeof(Sprite))
                {
                    Sprite sprite = (Sprite)comp;

                    ImGui.GetStateStorage().SetInt(ImGui.GetID("Sprite"), 1);

                    if (ImGui.CollapsingHeader("Sprite"))
                    {
                        SpriteDrawer.Draw(sprite);
                    }
                }
                else if(type == typeof(SpriteAnimation))
                {
                    SpriteAnimation sprAnim = (SpriteAnimation)comp;
                    ImGui.GetStateStorage().SetInt(ImGui.GetID("SpriteAnim"), 1);

                    if (ImGui.CollapsingHeader("SpriteAnim"))
                    {
                        SpriteAnimationDrawer.Draw(sprAnim);
                    }
                }
                else if (type == typeof(Rigidbody))
                {
                    Rigidbody rb = (Rigidbody)comp;

                    ImGui.GetStateStorage().SetInt(ImGui.GetID("Rigidbody"), 1);

                    if (ImGui.CollapsingHeader("Rigidbody"))
                    {
                        if (ImGui.BeginCombo("Type", rb.bodyType.ToString()))
                        {
                            for (int bt = 0; bt < (int)Box2D.NetStandard.Dynamics.Bodies.BodyType.MaxTypes; bt++)
                            {
                                bool is_selected = ((int)rb.bodyType == bt); // You can store your selection however you want, outside or inside your objects
                                if (ImGui.Selectable(((Box2D.NetStandard.Dynamics.Bodies.BodyType)bt).ToString(), is_selected))
                                    rb.bodyType = (Box2D.NetStandard.Dynamics.Bodies.BodyType)bt;
                                if (is_selected)
                                    ImGui.SetItemDefaultFocus();
                            }

                            ImGui.EndCombo();
                        }

                        float mass = rb.mass;
                        float linearDump = rb.linearDamping;
                        float friction = rb.friction;
                        float density = rb.density;

                        ImGui.InputFloat("Mass", ref mass);
                        ImGui.InputFloat("Linear Damping", ref linearDump);
                        ImGui.SliderFloat("Friction", ref friction, 0f, 1f);
                        ImGui.SliderFloat("Density", ref density, 0f, 1f);

                        rb.mass = mass;
                        rb.linearDamping = linearDump;
                        rb.friction = friction;
                        rb.density = density;
                    }
                }
                else if (type == typeof(Animator))
                {
                    Animator anim = (Animator)comp;

                    ImGui.GetStateStorage().SetInt(ImGui.GetID("Animator"), 1);

                    if (ImGui.CollapsingHeader("Animator"))
                    {
                        ImGui.Text("Anim Graph");
                        string viewTxt = string.IsNullOrEmpty(anim.animGraph) ? "No graph" : anim.animGraph;
                        ImGui.InputText("##animGraphTxt", ref viewTxt, 100, ImGuiInputTextFlags.ReadOnly);
                        CheckAnimGraphDrop(anim, selectedEntity);
                    }
                }
                else if (type == typeof(Canvas))
                {
                    Canvas canvas = (Canvas)comp;
                    ImGui.GetStateStorage().SetInt(ImGui.GetID("Canvas"), 1);

                    if (ImGui.CollapsingHeader("Canvas"))
                    {
                        Vector2 size = canvas.canvasSize;
                        bool isDynamic = canvas.isDynamicSize;

                        if (ImGui.InputFloat2("Size", ref size))
                            canvas.UpdateCanvasSize(size);
                        if (ImGui.Checkbox("Dynamic Size", ref isDynamic))
                            canvas.isDynamicSize = isDynamic;
                    }

                }
                else if(type == typeof(Tilemap))
                {
                    hasTilemap = true;

                    Tilemap tilemap = (Tilemap)comp;
                    ImGui.GetStateStorage().SetInt(ImGui.GetID("Tilemap"), 1);

                    if (ImGui.CollapsingHeader("Tilemap"))
                    {
                        TilemapDrawer.RenderTilemap(tilemap, transform);
                    }
                }
                else if(type == typeof(ParticleModule))
                {
                    ParticleModule pm = (ParticleModule)comp;
                    ImGui.GetStateStorage().SetInt(ImGui.GetID("Particle Module"), 1);

                    if (ImGui.CollapsingHeader("Particle Module"))
                    {
                        ParticleModuleDrawer.Draw(pm, newSelection);
                    }
                }
                else if (type == typeof(MeshRenderer))
                {
                    MeshRenderer mr = (MeshRenderer)comp;

                    ImGui.GetStateStorage().SetInt(ImGui.GetID("Mesh Renderer"), 1);

                    if (ImGui.CollapsingHeader("Mesh Renderer"))
                    {
                        MeshRendererDrawer.Draw(mr);
                    }
                }
            }

            // Entity with Tilemap component selected
            // Shpw the grid
            if(hasTilemap)
            {
                tilemapSelected = true;
                gridTrans.enabled = true;
                TMColliderGizmo.render = true;
            }
            else
            {
                tilemapSelected = false;
                gridTrans.enabled = false;
                TMColliderGizmo.render = false;
            }
        }

        private void DetailsAddComponentMenu(in Entity selectedEntity)
        {
            if (ImGui.Button("Add Component"))
            {
                ImGui.OpenPopup("AddComponent");
            }

            if (ImGui.BeginPopup("AddComponent"))
            {
                ImGui.Text("Built-in");
                ImGui.Separator();
                if (ImGui.MenuItem("Sprite"))
                {
                    ComponentManager.AddSprite(selectedEntity);
                }
                else if (ImGui.MenuItem("AABB"))
                {
                    ComponentManager.AddAABB(selectedEntity);
                }
                else if (ImGui.MenuItem("Circle Collider"))
                {
                    ComponentManager.AddCircleCollider(selectedEntity);
                }
                else if (ImGui.MenuItem("Rigidbody"))
                {
                    ComponentManager.AddRigidbody(selectedEntity);
                }
                else if (ImGui.MenuItem("Sprite Animation"))
                {
                    ComponentManager.AddSpriteAnimation(selectedEntity);
                }
                //else if (ImGui.MenuItem("Animator"))
                //{
                //    ComponentManager.AddAnimator(selectedEntity);
                //}
                else if (ImGui.MenuItem("Particle Module"))
                {
                    ComponentManager.AddParticleModule(selectedEntity);
                }
                else if(ImGui.MenuItem("Tilemap"))
                {
                    ComponentManager.AddTilemap(selectedEntity);
                }
                else if (ImGui.MenuItem("MeshRenderer"))
                {
                    ComponentManager.AddMeshRenderer(selectedEntity);
                }

                ImGui.Spacing();
                ImGui.Spacing();

                ImGui.Text("User");
                ImGui.Separator();
                foreach (var type in userTypes)
                {
                    if (ImGui.MenuItem(type.Name))
                        selectedEntity.Set(type, Activator.CreateInstance(type));
                }

                ImGui.EndPopup();
            }
        }

        private void DetailMakePrefabMenu(in Entity selectedEntity)
        {
            if(!selectedEntity.Has<Prefab>())
            {
                Transform transform = selectedEntity.Get<Transform>();
                if (transform.parent != null)
                    return;

                if (ImGui.Button("Make Prefab"))
                {
                    int elementIndex = 0;
                    RecursePrefab(transform, ref elementIndex);

                    string entityName = selectedEntity.Get<String>();
                    if (string.IsNullOrEmpty(entityName))
                        entityName = "EntityPrefab";
                    string savePath = Editor.AssetPath + entityName + ".abprefab";

                    int dupeInd = 0;
                    while (File.Exists(savePath))
                        savePath = savePath.Replace(".abprefab", ++dupeInd + ".abprefab");

                    PrefabMeta.CreatePrefabAsset(savePath, selectedEntity);
                }
            }
        }

        private void RecursePrefab(Transform transform, ref int index, Prefab root = null)
        {
            Prefab prefab = root;
            if (prefab == null)
            {
                prefab = new Prefab();
                transform.entity.Add<Prefab>(prefab);
            }
            else
            {
                //prefab.prefabToSceneRemap.Add(transform.entity.Get<Guid>(), Guid.Empty);
                transform.entity.Add<PrefabElement>(new PrefabElement(index++));
            }

            foreach (var child in transform.children)
            {
                RecursePrefab(child, ref index, prefab);
            }
        }


        private void DeleteRecursive(Transform transform)
        {
            foreach (var child in transform.children)
            {
                DeleteRecursive(child);
            }

            hierList.Remove(transform);
            transform.entity.DestroyEntity();
        }

        private void DeleteEntityButton(in Entity entity)
        {
            if (ImGui.Button("Delete Entity"))
            {
                DeleteRecursive(entity.Get<Transform>());
            }
        }

        private static List<Transform> hierList;

        private static void DepthSearch()
        {
            hierList = new List<Transform>();
            var query = new QueryDescription().WithAll<Transform>();
            Game.GameWorld.Query(in query, (ref Transform hierTrans) =>
            {
                if (hierTrans.parent == null)
                {
                    DepthRec(hierTrans);
                }
            });
        }

        private static void DepthRec(Transform transform)
        {
            if (transform.tag.Equals("EditorGrid"))
                return;

            hierList.Add(transform);
            if (transform.children.Count > 0)
            {
                foreach (var child in transform.children)
                {
                    DepthRec(child);
                }
            }
        }

        internal static void AddToHierList(Transform transform)
        {
            hierList.Add(transform);
        }

        private unsafe static void RenderHierarchy()
        {
            ImGui.Begin("Hierarchy");

            float width = ImGui.GetWindowWidth();
            float height = ImGui.GetWindowHeight();
            Vector2 windowPos = ImGui.GetCursorPos();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));

            if (hierList != null && hierList.Count > 0)
            {
                var roots = hierList.Where(t => t.parent == null && !t.tag.Equals("EditorNotVisible"));
                foreach (var transform in roots)
                {
                    RenderTreeRec(transform);
                }
            }

            // Empty Frame
            ImGui.SetCursorPos(windowPos);
            ImGui.Dummy(new Vector2(width, height));


            if (ImGui.BeginPopupContextWindow())
            {
                ImGui.Text("Create");
                ImGui.Separator();
                ImGui.Spacing();
                if (ImGui.MenuItem("Entity"))
                {
                    //var newEnt = GameWorld.Create("NewEntity", Guid.NewGuid());
                    //Transform newTrans = new Transform();
                    //newEnt.Add(newTrans);
                    var newEnt = EntityManager.CreateEntity("New Entity");
                    hierList.Add(newEnt.Get<Transform>());
                }

                ImGui.EndPopup();
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("ItemIndex");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var srcItem = hierList[srcIndex];
                    srcItem.parent = null;
                }

                payload = ImGui.AcceptDragDropPayload("PrefabFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var prefabFilePath = AssetsFolderView.files[srcIndex];
                    PrefabMeta prefabMeta = AssetHandler.GetMeta(prefabFilePath) as PrefabMeta;

                    var prefabTransform = PrefabManager.GetPrefabTransform(prefabMeta.fPathHash);
                    if (prefabTransform == null)
                    {
                        PrefabAsset prefabAsset = AssetHandler.GetAssetBinding(prefabMeta) as PrefabAsset;
                        prefabTransform = EntityManager.LoadSerializedPrefab(prefabAsset);
                        //prefabTransform.entity.Get<Prefab>().prefabAsset = prefabAsset;
                        PrefabManager.AddPrefabEntity(prefabTransform.entity, prefabAsset.fPathHash);
                    }

                    var entity = EntityManager.Instantiate(prefabTransform.entity);
                    hierList.Add(entity.Get<Transform>());
                }


                ImGui.EndDragDropTarget();
            }

            ImGui.PopStyleVar();


            ImGui.End();
        }

        static void RenderTreeRec(Transform transform)
        {
            if (transform.children.Count > 0 && !transform.tag.Equals("NoChild"))
            {
                bool open = ImGui.TreeNodeEx(transform.name, ImGuiTreeNodeFlags.OpenOnArrow);
                //if (ImGui.IsItemClicked())
                //{
                //    GameWorld.SetData(transform.entity);
                //}
                TreeNodeDrag(transform);

                if (open)
                {
                    foreach (var child in transform.children)
                    {
                        RenderTreeRec(child);
                    }
                    ImGui.TreePop();
                }

            }
            else
            {
                ImGui.TreeNodeEx(transform.name, ImGuiTreeNodeFlags.Leaf);
                TreeNodeDrag(transform);

                ImGui.TreePop();
            }
        }

        static unsafe void TreeNodeDrag(Transform transform)
        {
            int index = hierList.IndexOf(transform);

            if (ImGui.IsItemHovered(0))
            {
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    selectedEntity = transform.entity;
            }

            if (ImGui.BeginDragDropSource())
            {
                ImGui.Text(transform.name);

                ImGui.SetDragDropPayload("ItemIndex", (IntPtr)(&index), sizeof(int));
                ImGui.EndDragDropSource();
            }

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("ItemIndex");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var srcItem = hierList[srcIndex];

                    bool valid = true;
                    Transform parent = transform.parent;
                    while (parent != null)
                    {
                        if (parent == srcItem)
                        {
                            valid = false;
                            break;
                        }

                        parent = parent.parent;
                    }

                    if (valid)
                        srcItem.parent = transform;
                }


                ImGui.EndDragDropTarget();
            }
        }


        private static void DepthSearch(Transform transform)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 5));
            if (transform.children.Count > 0)
            {
                bool open = ImGui.TreeNodeEx(transform.name, ImGuiTreeNodeFlags.OpenOnArrow);
                if (ImGui.IsItemClicked())
                {
                    selectedEntity = transform.entity;
                }
                if (open)
                {
                    foreach (var child in transform.children)
                    {
                        DepthSearch(child);
                    }
                    ImGui.TreePop();
                }

            }
            else
            {
                ImGui.TreeNodeEx(transform.name, ImGuiTreeNodeFlags.Leaf);
                if (ImGui.IsItemClicked())
                {
                    selectedEntity = transform.entity;
                }

                ImGui.TreePop();
            }
            ImGui.PopStyleVar();
        }

       

        // File Drag-Drops
        

        unsafe void CheckAnimGraphDrop(Animator sourceAnim, in Entity entity)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("AnimGraphFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var animGraphFilePath = AssetsFolderView.files[srcIndex];
                    bool firstSet = string.IsNullOrEmpty(sourceAnim.animGraph);
                    sourceAnim.animGraph = animGraphFilePath;

                    if (firstSet)
                    {
                        if (!entity.Has<Sprite>())
                            ComponentManager.AddSprite(entity);

                        Sprite sprite = entity.Get<Sprite>();
                        AnimationState entry = sourceAnim.GetEntryState();
                        if (entry != null)
                        {
                            SpriteClip clip = entry.clip;
                            sprite.SetTexture(AssetCache.CreateTexture2D(clip.imgPath));
                            sprite.Resize(new Vector2(clip.frameWidth, clip.frameHeight));
                            sprite.SetUVPosScale(clip.uvPoses[0], clip.uvScales[0]);
                        }

                        if (!entity.Has<AABB>())
                        {
                            ComponentManager.AddAABB(entity);
                        }

                    }

                }

                ImGui.EndDragDropTarget();
            }
        }

        private void RenderPropertyEditors(Type type, object comp)
        {
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.PropertyType == typeof(float))
                {
                    float tmp = (float)prop.GetValue(comp);
                    if(ImGui.InputFloat(prop.Name, ref tmp))
                        prop.SetValue(comp, tmp);
                }
                else if (prop.PropertyType == typeof(int))
                {
                    int tmp = (int)prop.GetValue(comp);
                    if(ImGui.InputInt(prop.Name, ref tmp))
                        prop.SetValue(comp, tmp);
                }
                else if (prop.PropertyType == typeof(bool))
                {
                    bool tmp = (bool)prop.GetValue(comp);
                    if(ImGui.Checkbox(prop.Name, ref tmp))
                        prop.SetValue(comp, tmp);
                }
                else if (prop.PropertyType == typeof(Vector2))
                {
                    Vector2 tmp = (Vector2)prop.GetValue(comp);
                    if(ImGui.InputFloat2(prop.Name, ref tmp))
                        prop.SetValue(comp, tmp);
                }
                else if (prop.PropertyType == typeof(Vector3))
                {
                    Vector3 tmp = (Vector3)prop.GetValue(comp);
                    if(ImGui.InputFloat3(prop.Name, ref tmp))
                        prop.SetValue(comp, tmp);

                }
                else if (prop.PropertyType == typeof(string))
                {
                    string tmp = (string)prop.GetValue(comp);
                    if(ImGui.InputText(prop.Name, ref tmp, 500))
                        prop.SetValue(comp, tmp);
                }
                else if (prop.PropertyType == typeof(Transform))
                {
                    RenderTransformPropEditor(prop, comp);
                }
                else if (prop.PropertyType.IsEnum)
                {
                    int curVal = (int)prop.GetValue(comp);
                    var names = Enum.GetNames(prop.PropertyType);

                    ImGui.Text(prop.Name);
                    ImGui.SameLine();
                    if (ImGui.BeginCombo("##" + prop.Name, names[curVal]))
                    {
                        for (int sel = 0; sel < names.Length; sel++)
                        {
                            bool is_selected = curVal == sel;
                            if (ImGui.Selectable(names[sel], is_selected))
                            {
                                object newEnumValue = Enum.ToObject(prop.PropertyType, sel);
                                prop.SetValue(comp, newEnumValue);
                            }
                            if (is_selected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }
                }
            }
        }

        private unsafe void RenderTransformPropEditor(PropertyInfo prop, object comp)
        {
            Transform tmpTrans = (Transform)prop.GetValue(comp);

            ImGui.Text(prop.Name);
            ImGui.SameLine();
            string viewTxt = tmpTrans == null ? "None" : tmpTrans.name;
            ImGui.InputText("##Txt" + prop.Name, ref viewTxt, 100, ImGuiInputTextFlags.ReadOnly);

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("ItemIndex");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    Transform srcItem = hierList[srcIndex];
                    prop.SetValue(comp, srcItem);
                }

                ImGui.EndDragDropTarget();
            }
        }

        #region ImGuiStyle
        public static void SetupImGuiStyle()
        {
            // Soft Cherry stylePatitotective from ImThemes
            var style = ImGuiNET.ImGui.GetStyle();

            defaultFont = ImGui.GetIO().Fonts.AddFontFromFileTTF(Directory.GetCurrentDirectory() + "/Assets/Fonts/OpenSans-SemiBold.ttf", 16);
            Editor.GetImGuiRenderer().RecreateFontDeviceTexture();

            style.Alpha = 1.0f;
            style.DisabledAlpha = 0.4000000059604645f;
            style.WindowPadding = new Vector2(10.0f, 10.0f);
            style.WindowRounding = 4.0f;
            style.WindowBorderSize = 0.0f;
            style.WindowMinSize = new Vector2(50.0f, 50.0f);
            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
            style.WindowMenuButtonPosition = ImGuiDir.Left;
            style.ChildRounding = 0.0f;
            style.ChildBorderSize = 1.0f;
            style.PopupRounding = 1.0f;
            style.PopupBorderSize = 1.0f;
            style.FramePadding = new Vector2(5.0f, 3.0f);
            style.FrameRounding = 3.0f;
            style.FrameBorderSize = 0.0f;
            style.ItemSpacing = new Vector2(6.0f, 6.0f);
            style.ItemInnerSpacing = new Vector2(3.0f, 2.0f);
            style.CellPadding = new Vector2(3.0f, 3.0f);
            style.IndentSpacing = 6.0f;
            style.ColumnsMinSpacing = 6.0f;
            style.ScrollbarSize = 13.0f;
            style.ScrollbarRounding = 16.0f;
            style.GrabMinSize = 20.0f;
            style.GrabRounding = 4.0f;
            style.TabRounding = 4.0f;
            style.TabBorderSize = 1.0f;
            style.TabMinWidthForCloseButton = 0.0f;
            style.ColorButtonPosition = ImGuiDir.Right;
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
            style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

            style.Colors[(int)ImGuiCol.Text] = new Vector4(0.8588235378265381f, 0.929411768913269f, 0.886274516582489f, 1.0f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.5215686559677124f, 0.5490196347236633f, 0.5333333611488342f, 1.0f);
            style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1294117718935013f, 0.1372549086809158f, 0.168627455830574f, 1.0f);
            style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.1490196138620377f, 0.1568627506494522f, 0.1882352977991104f, 1.0f);
            style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0.1372549086809158f, 0.1137254908680916f, 0.1333333402872086f, 1.0f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.168627455830574f, 0.1843137294054031f, 0.2313725501298904f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.2313725501298904f, 0.2000000029802322f, 0.2705882489681244f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.501960813999176f, 0.07450980693101883f, 0.2549019753932953f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.239215686917305f, 0.239215686917305f, 0.2196078449487686f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.3882353007793427f, 0.3882353007793427f, 0.3725490272045135f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.6941176652908325f, 0.6941176652908325f, 0.686274528503418f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.6941176652908325f, 0.6941176652908325f, 0.686274528503418f, 1.0f);
            style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.658823549747467f, 0.1372549086809158f, 0.1764705926179886f, 1.0f);
            style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.7098039388656616f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.Button] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.Header] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.501960813999176f, 0.07450980693101883f, 0.2549019753932953f, 1.0f);
            style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.4274509847164154f, 0.4274509847164154f, 0.4980392158031464f, 1.0f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.09803921729326248f, 0.4000000059604645f, 0.7490196228027344f, 1.0f);
            style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.09803921729326248f, 0.4000000059604645f, 0.7490196228027344f, 1.0f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.1764705926179886f, 0.3490196168422699f, 0.5764706134796143f, 1.0f);
            style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
            style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.196078434586525f, 0.407843142747879f, 0.6784313917160034f, 1.0f);
            style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.06666667014360428f, 0.1019607856869698f, 0.1450980454683304f, 1.0f);
            style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.1333333402872086f, 0.2588235437870026f, 0.4235294163227081f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.8588235378265381f, 0.929411768913269f, 0.886274516582489f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.3098039329051971f, 0.7764706015586853f, 0.196078434586525f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.2000000029802322f, 1.0f);
            style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.3098039329051971f, 0.3098039329051971f, 0.3490196168422699f, 1.0f);
            style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.2274509817361832f, 0.2274509817361832f, 0.2470588237047195f, 1.0f);
            style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.3843137323856354f, 0.6274510025978088f, 0.9176470637321472f, 1.0f);
            style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
            style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 1.0f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.300000011920929f);
        }
        #endregion
    }
}

