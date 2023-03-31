using System;
using ABEngine.ABERuntime;
using ABEngine.ABERuntime.ECS;
using Halak;
using Veldrid;

namespace ABEngine.ABEditor.Assets.Meta
{
	public class MaterialMeta : AssetMeta
	{
        
		public MaterialMeta() : base()
		{
		}

        public override void Deserialize(string json)
        {
            throw new NotImplementedException();
        }

        public override void DrawMeta()
        {

        }

        public override JSerializable GetCopy(ref Entity newEntity)
        {
            throw new NotImplementedException();
        }

        public override JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("GUID", uniqueID.ToString());
            //jObj.Put("PropCount", );
            //jObj.Put("ImageSize", imageSize);
            //jObj.Put("SpriteSize", spriteSize);

            return jObj.Build();
        }

        public override void SetReferences()
        {
            throw new NotImplementedException();
        }
    }
}

