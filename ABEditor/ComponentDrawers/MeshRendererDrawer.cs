using System;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ImGuiNET;
using System.Numerics;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABEditor.ComponentDrawers
{
	public static class MeshRendererDrawer
	{
        static MeshRenderer cachedMr;
        static AssetMeta cachedMeshMeta;
        
        public static void Draw(MeshRenderer mr)
        {
            if(mr != cachedMr)
            {
                cachedMr = mr;
                if (mr.mesh != null)
                    cachedMeshMeta = AssetHandler.GetMeta(mr.mesh.fPathHash) as AssetMeta;
            }

            ImGui.Text("Mesh");
            ImGui.SameLine();
            string meshTxt = cachedMeshMeta == null ? "Cube" : cachedMeshMeta.displayName;
            if (meshTxt == null)
                meshTxt = "Null";
            ImGui.InputText("##MeshName", ref meshTxt, 100, ImGuiInputTextFlags.ReadOnly);
            CheckMeshDrop(mr);

            ImGui.Text("Material");
            ImGui.SameLine();
            ImGui.InputText("##matName", ref mr.material.name, 100, ImGuiInputTextFlags.ReadOnly);
            CheckMaterialDropMR(mr);
        }

        static unsafe void CheckMeshDrop(MeshRenderer mr)
        {
            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("MeshFileInd");
                if (payload.NativePtr != null)
                {
                    var dataPtr = (int*)payload.Data;
                    int srcIndex = dataPtr[0];

                    var meshFilePath = AssetsFolderView.files[srcIndex];

                    MeshMeta meshMeta = AssetHandler.GetMeta(meshFilePath) as MeshMeta;
                    Mesh mesh = AssetHandler.GetAssetBinding(meshMeta) as Mesh;

                    Editor.EditorActions.UpdateProperty(mr.mesh, mesh, mr, nameof(mr.mesh));

                    cachedMeshMeta = meshMeta;
                }

                ImGui.EndDragDropTarget();
            }
        }

        static unsafe void CheckMaterialDropMR(MeshRenderer mr)
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

                    Editor.EditorActions.UpdateProperty(mr.material, mat, mr, nameof(mr.material));
                }

                ImGui.EndDragDropTarget();
            }
        }
    }
}

