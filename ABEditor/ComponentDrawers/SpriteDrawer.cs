using System;
using ABEngine.ABEditor.ImGuiPlugins;
using ABEngine.ABEditor.PropertyDrawers;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.Components;
using ImGuiNET;
using System.Numerics;
using Veldrid;
using ABEngine.ABEditor.Assets;
using ABEngine.ABEditor.Assets.Meta;
using ABEngine.ABERuntime.Animation;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABEditor.ComponentDrawers
{
	public static class SpriteDrawer
	{
        public static void Draw(Sprite sprite)
        {
            ImGui.Text("Image");
            ImGui.Spacing();

            IntPtr imgPtr = Editor.GetImGuiRenderer().GetOrCreateImGuiBinding(GraphicsManager.rf, sprite.texture.texture);
            ImGui.Image(imgPtr, new Vector2(100f, 100f));

            CheckSpriteDrop(sprite);

            Vector4 tint = sprite.tintColor;
            bool flipX = sprite.flipX;
            bool flipY = sprite.flipY;
            Vector2 spriteSize = sprite.size * 100f;
            int spriteID = sprite.GetSpriteID();

            if(ImGui.ColorEdit4("Tint", ref tint))
                Editor.EditorActions.UpdateProperty(sprite.tintColor, tint, sprite, nameof(sprite.tintColor));

            if (ImGui.Checkbox("FlipX", ref flipX))
                Editor.EditorActions.UpdateProperty(sprite.flipX, flipX, sprite, nameof(sprite.flipX));
            if (ImGui.Checkbox("FlipY", ref flipY))
                Editor.EditorActions.UpdateProperty(sprite.flipY, flipY, sprite, nameof(sprite.flipY));
            if (ImGui.InputInt("Sprite ID", ref spriteID))
                sprite.SetSpriteID(spriteID);

            ImGui.Text("Material");
            ImGui.InputText("##matName", ref sprite.sharedMaterial.name, 100, ImGuiInputTextFlags.ReadOnly);
            CheckMaterialDropSprite(sprite);
        }

        static unsafe void CheckSpriteDrop(Sprite sourceSprite)
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
                    Texture2D texture = AssetHandler.GetAssetBinding(texMeta, spriteFilePath) as Texture2D;

                    Editor.EditorActions.UpdateProperty(sourceSprite.texture, texture, sourceSprite, nameof(sourceSprite.texture), value => sourceSprite.SetTexture(value));

                    //sourceSprite.SetTexture(texture);
                }

                ImGui.EndDragDropTarget();
            }
        }

        static unsafe void CheckMaterialDropSprite(Sprite sprite)
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
                    PipelineMaterial mat = AssetHandler.GetAssetBinding(matMeta, materialFilePath) as PipelineMaterial;

                    Editor.EditorActions.UpdateProperty(sprite.material, mat, sprite, nameof(sprite.material), value => sprite.SetMaterial(value));

                    //sprite.SetMaterial(mat);
                }

                ImGui.EndDragDropTarget();
            }
        }
    }
}

