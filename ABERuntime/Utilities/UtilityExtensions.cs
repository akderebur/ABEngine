using System;
using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Halak;

namespace ABEngine.ABERuntime
{
    public static class UtilityExtensions
    {
        public static JsonSerializer jsonSerializer;
        public static JsonSerializerSettings jsonSettings;

        static IgnoreReferencesResolver resolver;

        static UtilityExtensions()
        {
            jsonSerializer = JsonSerializer.CreateDefault();
            resolver = new IgnoreReferencesResolver();

            jsonSettings = new JsonSerializerSettings()
            {
                ContractResolver = resolver
            };
            jsonSerializer.ContractResolver = resolver;
        }

        public static Func<Task> Coroutine(this Task task)
        {
            return new Func<Task>(() => task);
        }

        // Random
        public static float NextFloat(this Random rnd, float min, float max)
        {
            return (float)rnd.NextDouble() * (max - min) + min;
        }

        #region Conversions
        public static int ToMilliseconds(this float seconds)
        {
            return (int)TimeSpan.FromSeconds(seconds).TotalMilliseconds;
        }
        #endregion

        #region Vectors
        public static Vector2 ToVector2(this Vector3 vec)
        {
            return new Vector2(vec.X, vec.Y);
        }

        public static Vector3 ToVector3(this Vector2 vec)
        {
            return new Vector3(vec.X, vec.Y, 0f);
        }

        public static Vector3 ToVector3(this Vector4 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static Vector2 PixelToWorld(this Vector2 vec)
        {
            return new Vector2(vec.X, vec.Y) / 100f;
        }

        public static Vector2 PixelToWorld(this Vector3 vec)
        {
            return new Vector2(vec.X, vec.Y) / 100f;
        }

        public static Vector2 RoundTo2Dec(this Vector2 vec)
        {
            vec.X = (float)MathF.Round(vec.X * 100f) / 100f;
            vec.Y = (float)MathF.Round(vec.Y * 100f) / 100f;

            return vec;
        }

        public static Vector3 RoundTo2Dec(this Vector3 vec)
        {
            vec.X = (float)MathF.Round(vec.X * 100f) / 100f;
            vec.Y = (float)MathF.Round(vec.Y * 100f) / 100f;
            //vec.Z = (float)MathF.Round(vec.Z * 100f) / 100f;

            return vec;
        }

        public static List<Vector2> GetAdjacent(this Vector2 tilePos, Vector2 tileSize)
        {
            List<Vector2> neighbors = new List<Vector2>();

            neighbors.Add((tilePos + new Vector2(tileSize.X, 0f)).RoundTo2Dec()); // Right
            neighbors.Add((tilePos - new Vector2(tileSize.X, 0f)).RoundTo2Dec()); // Left
            neighbors.Add((tilePos + new Vector2(0f, tileSize.Y)).RoundTo2Dec()); // Up
            neighbors.Add((tilePos - new Vector2(0f, tileSize.Y)).RoundTo2Dec()); // Down

            return neighbors;
        }

        #endregion

        #region ImGui
        public static Vector2 ToImGuiVector2(this Vector3 vec)
        {
            return new Vector2(vec.X, Game.screenSize.Y - vec.Y);
        }

        public static Vector2 ToImGuiVector2(this Vector2 vec)
        {
            return new Vector2(vec.X, Game.screenSize.Y - vec.Y);
        }

        public static Vector2 ToImGuiRefVector2(this Vector3 vec)
        {
            return new Vector2(vec.X, Game.canvas.referenceSize.Y - vec.Y);
        }

        public static Vector2 ToImGuiRefVector2(this Vector2 vec)
        {
            return new Vector2(vec.X, Game.canvas.referenceSize.Y - vec.Y);
        }
        #endregion
    }

    class IgnoreReferencesResolver : DefaultContractResolver
    {
        public IgnoreReferencesResolver()
        {
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            if (prop.PropertyType.IsValueType == false || (member.MemberType == MemberTypes.Field && !member.DeclaringType.IsValueType))
            {
                prop.Ignored = true;
            }
            return prop;
        }
    }
}
