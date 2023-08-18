using System;
using ABEngine.ABEditor.ImGuiPlugins;
using ABEngine.ABEditor.PropertyDrawers;
using ABEngine.ABERuntime;
using ImGuiNET;
using System.Net.NetworkInformation;
using ABEngine.ABERuntime.Components;
using Veldrid;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;
using System.Numerics;
using ABEngine.ABERuntime.Core.Math;

namespace ABEngine.ABEditor.ComponentDrawers
{
	public static class ParticleModuleDrawer
	{
        // Gradient Editor
        static bool gradientEditOpen;
        static bool firstGradientOpen;
        static int stateID = 10;
        static ColorGradient editGradient;

        static ImGradientHDRState state = new ImGradientHDRState();
        static ImGradientHDRTemporaryState tempState = new ImGradientHDRTemporaryState();

        static ParticleModule lastPM = null;
        static IntPtr imgPtr = IntPtr.Zero;

        static float prWidth;
        static float prHeight;

        static string[] renderTypes = new string[] { "Alpha Blend", "Additive", "Custom" };


        static void DrawGradientEditor()
        {
            if (gradientEditOpen)
            {
                ImGui.Begin("Edit Gradient", ref gradientEditOpen);

                bool isMarkerShown = true;
                bool changed = ImGradientHDR.DrawGradient(stateID, ref state, ref tempState, isMarkerShown);

                if (tempState.selectedMarkerType == ImGradientHDRMarkerType.Color)
                {
                    var selectedColorMarker = state.GetColorMarker(tempState.selectedIndex);
                    if (selectedColorMarker != null)
                    {
                        changed |= ImGui.ColorEdit3("Color", ref selectedColorMarker.Color, ImGuiColorEditFlags.Float);
                        //ImGui.DragFloat("Intensity", ref selectedColorMarker.Intensity, 0.1f, 0.0f, 10.0f, "%f");
                    }
                }

                if (tempState.selectedMarkerType == ImGradientHDRMarkerType.Alpha)
                {
                    var selectedAlphaMarker = state.GetAlphaMarker(tempState.selectedIndex);
                    if (selectedAlphaMarker != null)
                    {
                        changed |= ImGui.DragFloat("Alpha", ref selectedAlphaMarker.Alpha, 0.1f, 0.0f, 1.0f, "%f");
                    }
                }

                if (tempState.selectedMarkerType != ImGradientHDRMarkerType.Unknown)
                {
                    if (ImGui.Button("Delete"))
                    {
                        changed = true;
                        if (tempState.selectedMarkerType == ImGradientHDRMarkerType.Color)
                        {
                            state.RemoveColorMarker(tempState.selectedIndex);
                            tempState = new ImGradientHDRTemporaryState();
                        }
                        else if (tempState.selectedMarkerType == ImGradientHDRMarkerType.Alpha)
                        {
                            state.RemoveAlphaMarker(tempState.selectedIndex);
                            tempState = new ImGradientHDRTemporaryState { };
                        }
                    }
                }

                ImGui.End();

                if(changed)
                {
                    editGradient.colorKeys.Clear();
                    editGradient.alphaKeys.Clear();

                    foreach (var colorMarker in state.Colors)
                        editGradient.colorKeys.Add(new ColorKey(colorMarker.Position, colorMarker.Color));
                    foreach (var alphaMarker in state.Alphas)
                        editGradient.alphaKeys.Add(new AlphaKey(alphaMarker.Position, alphaMarker.Alpha));
                }
            }
        }

        public static void Draw(ParticleModule pm, bool newSelection)
		{
            DrawGradientEditor();

            if (newSelection || lastPM != pm)
            {
                gradientEditOpen = false;
                firstGradientOpen = false;
                lastPM = pm;
                imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, pm.particleTexture.texture);
            }

            int maxParticles = pm.maxParticles;
            float spawnRange = pm.spawnRange;
            FloatRange startLifetime = pm.startLifetime;
            FloatRange spawnRate = pm.spawnRate;
            FloatRange speed = pm.speed;

            DrawFloatRangeEditor(startLifetime, "Start Lifetime");
            DrawFloatRangeEditor(speed, "Speed");
            DrawFloatRangeEditor(spawnRate, "Spawn Rate");

            if (ImGui.InputFloat("Spawn Range", ref spawnRange))
                pm.spawnRange = spawnRange;

            if (ImGui.InputInt("Max Particles", ref maxParticles))
                pm.maxParticles = maxParticles;

            ImGui.Text("Simulation Space");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##SimSpace", pm.simulationSpace.ToString()))
            {
                for (int ss = 0; ss < 2; ss++)
                {
                    bool is_selected = (int)pm.simulationSpace == ss;
                    if (ImGui.Selectable(((SimulationSpace)ss).ToString(), is_selected))
                        pm.simulationSpace = (SimulationSpace)ss;
                    if (is_selected)
                        ImGui.SetItemDefaultFocus();
                }

                ImGui.EndCombo();
            }


            if (ImGui.CollapsingHeader("Lifetime Size"))
            {
                CurveEditor.Draw(pm.lifetimeSize);
            }

            if (ImGui.CollapsingHeader("Lifetime Color"))
            {
                // Gradient Edit
                ColorGradient gradient = pm.lifetimeColor;

                if (!firstGradientOpen)
                {
                    firstGradientOpen = true;
                    state = new ImGradientHDRState();
                    tempState = new ImGradientHDRTemporaryState();

                    foreach (var colorKey in gradient.colorKeys)
                        state.AddColorMarker(colorKey.Time, colorKey.Color, 1f);
                    foreach (var alphaKey in gradient.alphaKeys)
                        state.AddAlphaMarker(alphaKey.Time, alphaKey.Alpha);
                }


                ImGradientHDR.DrawGradient(11, ref state, ref tempState, false);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !gradientEditOpen)
                {
                    gradientEditOpen = true;
                    editGradient = gradient;
                }
            }

            if(ImGui.CollapsingHeader("Rendering"))
            {
                // Recalculate preview
                prWidth = ImGui.GetWindowWidth() / 2f;
                prHeight = pm.particleTexture.texture.Height * prWidth / pm.particleTexture.texture.Width;

                ImGui.Text("Texture");
                ImGui.SameLine();
                ImGui.Image(imgPtr, new Vector2(prWidth, prHeight));
                CheckTextureDrop(pm);


                ImGui.Text("Material");
                ImGui.InputText("##matName", ref pm.particleMaterial.name, 100, ImGuiInputTextFlags.ReadOnly);
                CheckMaterialDropParticle(pm);
            }
        }

        static void DrawFloatRangeEditor(FloatRange range, string name)
        {
            if (range.isConstant)
            {
                float val = range.value;
                if (ImGui.InputFloat(name, ref val))
                    range.value = val;
            }
            else
            {
                Vector2 val = range.range;
                if (ImGui.InputFloat2(name, ref val))
                    range.range = val;
            }

            ImGui.PushID(name);
            ImGui.SameLine();
            bool ticked = !range.isConstant;
            if (ImGui.Checkbox("Range", ref ticked))
                    range.isConstant = !ticked;
            ImGui.PopID();

        }

        // Drag drop image
        unsafe static void CheckTextureDrop(ParticleModule pm)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("SpriteFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    string spriteFilePath = AssetsFolderView.files[srcIndex];

                    TextureMeta texMeta = AssetHandler.GetMeta(spriteFilePath) as TextureMeta;
                    pm.particleTexture = AssetHandler.GetAssetBinding(texMeta) as Texture2D;
                    imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, pm.particleTexture.texture);
                }

                ImGui.EndDragDropTarget();
            }
        }

        static unsafe void CheckMaterialDropParticle(ParticleModule pm)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("MaterialFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var materialFilePath = AssetsFolderView.files[srcIndex];
                    MaterialMeta matMeta = AssetHandler.GetMeta(materialFilePath) as MaterialMeta;
                    PipelineMaterial mat = AssetHandler.GetAssetBinding(matMeta) as PipelineMaterial;

                    pm.particleMaterial = mat;
                }

                ImGui.EndDragDropTarget();
            }
        }
    }
}

