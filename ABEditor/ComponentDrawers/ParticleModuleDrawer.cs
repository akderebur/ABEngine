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
            float startLifetime = pm.startLifetime;
            float spawnRate = pm.spawnRate;
            float spawnRange = pm.spawnRange;
            float speed = pm.speed;

            if (ImGui.InputFloat("Start Lifetime", ref startLifetime))
                pm.startLifetime = startLifetime;

            if (ImGui.InputFloat("Speed", ref speed))
                pm.speed = speed;

            if (ImGui.InputFloat("Spawn Rate", ref spawnRate))
                pm.spawnRate = spawnRate;

            if (ImGui.InputFloat("Spawn Range", ref spawnRange))
                pm.spawnRange = spawnRange;

            if (ImGui.InputInt("Max Particles", ref maxParticles))
                pm.maxParticles = maxParticles;


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


                ImGui.Text("Render Mode");
                ImGui.SameLine();
                string renderType = "Custom";
                PipelineMaterial mat = pm.particleMaterial;
                if (mat == GraphicsManager.GetUberMaterial())
                    renderType = "Alpha Blend";
                else if (mat == GraphicsManager.GetUberAdditiveMaterial())
                    renderType = "Additive";

                if (ImGui.BeginCombo("##RenderMode", renderType))
                {
                    for (int rt = 0; rt < 2; rt++)
                    {
                        bool is_selected = GraphicsManager.pipelineMaterials[rt]  == mat;
                        if (ImGui.Selectable(renderTypes[rt], is_selected))
                            pm.particleMaterial = GraphicsManager.pipelineMaterials[rt];
                        if (is_selected)
                            ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }
            }
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
                    pm.particleTexture = AssetHandler.GetTextureBinding(texMeta, spriteFilePath);
                    imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, pm.particleTexture.texture);
                }

                ImGui.EndDragDropTarget();
            }
        }
    }
}

