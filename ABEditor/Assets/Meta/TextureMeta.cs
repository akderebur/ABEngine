using System;
using System.Numerics;
using ABEngine.ABERuntime;
using Halak;
using Veldrid;
using ImGuiNET;
using System.Collections.Generic;
using ABEngine.ABERuntime.Core.Assets;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class TextureMeta : AssetMeta
	{
        internal Sampler sampler { get; set; }
        internal Vector2 imageSize { get; set; }
        internal Vector2 spriteSize { get; set; }

        public TextureMeta() : base()
		{
			spriteSize = Vector2.Zero;
            imageSize = Vector2.Zero;
			sampler = GraphicsManager.linearSampleClamp;
		}

        public override JValue Serialize()
        {
            base.Serialize();

            jObj.Put("Sampler", sampler.Name);
            jObj.Put("ImageSize", imageSize);
            jObj.Put("SpriteSize", spriteSize);

            return jObj.Build();
        }

        public override void Deserialize(string json)
        {
            base.Deserialize(json);

            imageSize = data["ImageSize"];
            spriteSize = data["SpriteSize"];

            string sampler = data["Sampler"];
            switch (sampler)
            {
                case "LinearClamp":
                    this.sampler = GraphicsManager.linearSampleClamp;
                    break;
                case "LinearWrap":
                    this.sampler = GraphicsManager.linearSamplerWrap;
                    break;
                case "PointClamp":
                    this.sampler = GraphicsManager.pointSamplerClamp;
                    break;
                default:
                    this.sampler = GraphicsManager.linearSampleClamp;
                    break;
            }
        }

        public override JSerializable GetCopy()
        {
            throw new NotImplementedException();
        }

        public override void SetReferences()
        {
            throw new NotImplementedException();
        }

        public override void DrawMeta()
        {
            
        }

        public override Texture2D CreateAssetBinding()
        {
            Texture2D tex = AssetCache.GetTextureEditorBinding(base.fPath);
            if(tex == null)
                tex = AssetCache.CreateTexture2D(base.fPath, sampler, spriteSize);
            return tex;
        }
    }
}

