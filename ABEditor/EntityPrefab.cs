using System;
using System.Linq;
using ABEngine.ABERuntime;
using Halak;
using ABEngine.ABERuntime.ECS;

namespace ABEngine.ABEditor
{
    public class EntityPrefab : JSerializable
    {
        public Entity entity { get; set; }

        public EntityPrefab()
        {
        }

        public JValue Serialize()
        {
            JsonObjectBuilder entObj = new JsonObjectBuilder(10000);
            entObj.Put("GUID", entity.Get<Guid>().ToString());
            entObj.Put("Name", entity.Get<string>());

            JsonArrayBuilder compArr = new JsonArrayBuilder(10000);
            var comps = entity.GetAllComponents();
            var types = entity.GetAllComponentTypes();
            for (int i = 0; i < comps.Length; i++)
            {
                if (typeof(JSerializable).IsAssignableFrom(types[i]))
                {
                    compArr.Push(((JSerializable)comps[i]).Serialize());
                }
                else if (types[i].IsSubclassOf(typeof(AutoSerializable)))
                {
                    compArr.Push(AutoSerializable.Serialize((AutoSerializable)comps[i]));

                }
            }

            entObj.Put("Components", compArr.Build());

            return entObj.Build();
        }


        public void Deserialize(string json)
        {
            var userTypes = Editor.GetUserTypes();
            JValue entity = JValue.Parse(json);

            string entName = entity["Name"];
            string guid = entity["GUID"];
            Entity newEnt = Game.GameWorld.CreateEntity(entName, Guid.Parse(guid));

            foreach (var component in entity["Components"].Array())
            {
                Type type = Type.GetType(component["type"]);

                if (type == null)
                    type = userTypes.FirstOrDefault(t => t.ToString().Equals(component["type"]));

                if (type == null)
                    continue;

                if (typeof(JSerializable).IsAssignableFrom(type))
                {
                    var serializedComponent = (JSerializable)Activator.CreateInstance(type);
                    serializedComponent.Deserialize(component.ToString());
                    newEnt.Set(type, serializedComponent);
                }

                else if (type.IsSubclassOf(typeof(AutoSerializable)))
                {
                    var comp = AutoSerializable.Deserialize(component.ToString(), type);
                    newEnt.Set(type, comp);
                }
            }
        }

        public void SetReferences()
        {
        }

        public JSerializable GetCopy(ref Entity newEntity)
        {
            throw new NotImplementedException();
        }
    }
}
