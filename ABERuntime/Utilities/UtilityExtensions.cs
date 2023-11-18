using System;
using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Halak;
using Force.Crc32;
using System.Text;
using Arch.Core;
using Arch.Core.Extensions;
using ABEngine.ABERuntime.Components;

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

        public static Vector4 ToVector4(this Vector3 vec)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, 0f);
        }

        public static Vector3 Normalize(this Vector3 v)
        {
            if (v.Length() < 1e-10)
                return v;

            return Vector3.Normalize(v);
        }

        public static Vector2 PixelToWorld(this Vector2 vec)
        {
            return new Vector2(vec.X, vec.Y) / 100f;
        }

        public static Vector2 PixelToWorld(this Vector3 vec)
        {
            return new Vector2(vec.X, vec.Y) / 100f;
        }

        public static Vector2 ScreenToCanvas(this Vector2 screenPos)
        {
            Vector2 ratio = screenPos / Game.pixelSize;
            return Game.canvas.canvasSize * ratio;
        }

        public static Vector3 ScreenToWorld(this Vector2 screenPoint)
        {
            return UnprojectOrtho(screenPoint, Game.pipelineData.Projection, Game.pipelineData.View, Game.pixelSize.X, Game.pixelSize.Y);
        }

        public static Vector3 UnprojectOrtho(Vector2 screenPoint, Matrix4x4 projection, Matrix4x4 view, float viewportWidth, float viewportHeight)
        {
            // Convert screen point to normalized device coordinates
            Vector3 point = new Vector3(
                (screenPoint.X / viewportWidth) * 2.0f - 1.0f,
                (screenPoint.Y / viewportHeight) * 2.0f - 1.0f,
                0);  // Z can be set to 0 in orthographic projection

            // Invert view and projection matrices
            Matrix4x4.Invert(view, out Matrix4x4 invertedView);
            Matrix4x4.Invert(projection, out Matrix4x4 invertedProjection);

            // Transform to eye space
            Vector3 eyeSpace = Vector3.Transform(point, invertedProjection);

            // Transform to world space
            Vector3 worldSpace = Vector3.Transform(eyeSpace, invertedView);

            return worldSpace;
        }

        //public static Vector3 CanvasToWorld(this Vector2 vec)
        //{
        //    Vector3 world = new Vector3(vec.X / 100f, vec.Y / 100f, 0f);
        //    return world + Game.activeCam.worldPosition;
        //}

        public static Vector2 MouseToZoomed(this Vector2 mousePos)
        {
            return new Vector2((mousePos.X - Game.pixelSize.X / 2f) * Game.zoomFactor + Game.pixelSize.X / 2f,
                               (mousePos.Y - Game.pixelSize.Y / 2f) * Game.zoomFactor + Game.pixelSize.Y / 2f);
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

        public static List<Vector2> GetAdjacentDiagonal(this Vector2 tilePos, Vector2 tileSize)
        {
            List<Vector2> neighbors = new List<Vector2>();

            neighbors.Add((tilePos + new Vector2(tileSize.X, 0f)).RoundTo2Dec()); // Right
            neighbors.Add((tilePos - new Vector2(tileSize.X, 0f)).RoundTo2Dec()); // Left
            neighbors.Add((tilePos + new Vector2(0f, tileSize.Y)).RoundTo2Dec()); // Up
            neighbors.Add((tilePos - new Vector2(0f, tileSize.Y)).RoundTo2Dec()); // Down

            neighbors.Add((tilePos + new Vector2(tileSize.X, tileSize.Y)).RoundTo2Dec()); // Right - Up
            neighbors.Add((tilePos + new Vector2(tileSize.X, -tileSize.Y)).RoundTo2Dec()); // Right - Down
            neighbors.Add((tilePos + new Vector2(-tileSize.X, tileSize.Y)).RoundTo2Dec()); // Left - Up
            neighbors.Add((tilePos + new Vector2(-tileSize.X, -tileSize.Y)).RoundTo2Dec()); // Left - Down

            return neighbors;
        }

        public static List<Vector2> GetAdjacentDiagonalExtended(this Vector2 tilePos, Vector2 tileSize)
        {
            List<Vector2> neighbors = new List<Vector2>();

            Vector2 start = tilePos + new Vector2(-tileSize.X * 2, -tileSize.Y * 2).RoundTo2Dec();
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    if (y == 2 && x == 2)
                        continue;

                    neighbors.Add((start + new Vector2(tileSize.X * x, tileSize.Y * y)).RoundTo2Dec());
                }
            }

            return neighbors;
        }

        // IO
        public static string ToCommonPath(this string path)
        {
            return path.Replace("\\", "/");
        }

        // Hashing
        public static uint ToHash32(this string str)
        {
            return Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(str));
        }

        // ECS
        public static bool IsEnabled(this in Entity entity)
        {
            if (entity.TryGet<Transform>(out Transform transform))
                return transform.enabled;

            return false;
        }

        #endregion

        #region ImGui
        public static Vector2 ToImGuiVector2(this Vector3 vec)
        {
            return new Vector2(vec.X, Game.pixelSize.Y - vec.Y);
        }

        public static Vector2 ToImGuiVector2(this Vector2 vec)
        {
            return new Vector2(vec.X, Game.pixelSize.Y - vec.Y);
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
