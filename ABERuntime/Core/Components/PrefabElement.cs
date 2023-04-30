using System;
using Halak;

namespace ABEngine.ABERuntime.Components
{
	public class PrefabElement : JSerializable
	{
        public int elementIndex { get; set; }

        public PrefabElement()
        {

        }

        public PrefabElement(int index)
		{
            elementIndex = index;
		}

        public JValue Serialize()
        {
            JsonObjectBuilder jObj = new JsonObjectBuilder(500);
            jObj.Put("type", GetType().ToString());
            jObj.Put("ElementIndex", elementIndex);

            return jObj.Build();
        }

        public void Deserialize(string json)
        {
            JValue data = JValue.Parse(json);
            elementIndex = data["ElementIndex"];
        }

        public void SetReferences()
        {
        }

        public JSerializable GetCopy()
        {
            return new PrefabElement(elementIndex);
        }
    }
}

