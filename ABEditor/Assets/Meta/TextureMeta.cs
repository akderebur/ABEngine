using System;
using System.Numerics;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.ECS;
using Halak;
using Veldrid;
using ImGuiNET;
using System.Collections.Generic;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class TextureMeta : AssetMeta
	{
        public Sampler sampler { get; set; }
        public Vector2 imageSize { get; set; }
        public Vector2 spriteSize { get; set; }

        public List<Vector2> spriteOffsets { get; set; }

        public TextureMeta() : base()
		{
			spriteSize = Vector2.Zero;
            imageSize = Vector2.Zero;
			sampler = GraphicsManager.linearSampleClamp;
		}

        public override void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);

            base.uniqueID = Guid.Parse(data["GUID"]);
            imageSize = new Vector2(data["ImageSizeX"], data["ImageSizeY"]);
            spriteSize = new Vector2(data["SpriteSizeX"], data["SpriteSizeY"]);

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

        public override JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("GUID", uniqueID.ToString());
            jObj.Put("Sampler", sampler.Name);
            jObj.Put("ImageSizeX", imageSize.X);
            jObj.Put("ImageSizeY", imageSize.Y);
            jObj.Put("SpriteSizeX", spriteSize.X);
            jObj.Put("SpriteSizeY", spriteSize.Y);

            return jObj.Build();
        }

        public override JSerializable GetCopy(ref Entity newEntity)
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
    }
}

