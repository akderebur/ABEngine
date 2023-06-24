using System;
using Newtonsoft.Json;
using Halak;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Linq;
using Arch.Core.Extensions;
using Arch.Core;
using Arch.Core.Utils;
using System.Collections.Generic;

namespace ABEngine.ABERuntime
{
    public abstract class ABComponent
    {
        protected string savedJson;

        public static Halak.JValue Serialize(ABComponent toSerialize)
        {
            JObject jo = JObject.FromObject(toSerialize, UtilityExtensions.jsonSerializer);
            jo.Add("type", toSerialize.GetType().ToString());

            //Serialize entity references
            foreach (PropertyInfo prop in toSerialize.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(Transform))
                {
                    Transform entTrans = (Transform)prop.GetValue(toSerialize);
                    string propGuid = null;
                    if (entTrans != null)
                        propGuid = entTrans.entity.Get<Guid>().ToString();
                    jo.Add(prop.Name, propGuid);
                }
            }

            return Halak.JValue.Parse(jo.ToString());
        }

        public static object Deserialize(string json, Type type)
        {
            var obj = JsonConvert.DeserializeObject(json, type, UtilityExtensions.jsonSettings);
            ((ABComponent)obj).savedJson = json;
            return obj;
        }

        public static object GetCopy(ABComponent toCopy)
        {
            Type type = toCopy.GetType();
            string serialized = Serialize(toCopy).Serialize();
            return Deserialize(serialized, type);
        }

        public static void SetReferences(ABComponent obj)
        {
            Halak.JValue data = null;

            //Deserialize entity references
            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(Transform))
                {
                    if (data == null)
                        data = Halak.JValue.Parse(obj.savedJson);

                    string transGuid = data[prop.Name];
                    if (string.IsNullOrEmpty(transGuid))
                        continue;

                    var query = new QueryDescription().WithAll<Transform>();
                    var entities = new List<Entity>();
                    Game.GameWorld.GetEntities(query, entities);

                    var transEnt = entities.FirstOrDefault(e => e.Get<Guid>().Equals(Guid.Parse(transGuid)));
                    if (transEnt != Entity.Null)
                        prop.SetValue(obj, transEnt.Get<Transform>());
                }
            }

            obj.savedJson = null;
        }

        protected virtual void PostDeserialize()
        {

        }

    }

}
