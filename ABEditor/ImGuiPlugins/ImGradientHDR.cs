using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Box2D.NetStandard.Common;
using ImGuiNET;
using System.Threading.Channels;
using ABEngine.ABEditor;

namespace ABEngine.ABEditor.ImGuiPlugins
{
    enum DrawMarkerMode
    {
        Selected,
        Unselected,
        None
    }

    enum MarkerDirection
    {
        ToUpper,
        ToLower
    }

    struct UpdateMarkerResult
    {
        public bool isChanged;
        public bool isHovered;
    };


    struct SortedMarker
    {
        public int index;
        public ImGradientHDRState.Marker marker;
    };


    public class ImGradientHDR
    {
        static Vector4 ByteToFloat = new Vector4(1 / 255f, 1 / 255f, 1 / 255f, 1 / 255f);

        static void DrawMarker(Vector2 pmin, Vector2 pmax, uint color, DrawMarkerMode mode)
        {
            var drawList = ImGui.GetWindowDrawList();
            var w = (int)MathF.Round(pmax.X - pmin.X);
            var h = (int)MathF.Round(pmax.Y - pmin.Y);
            var sign = h < 0 ? -1 : 1;

            var margin = 2;
            var marginh = margin * sign;

            if (mode != DrawMarkerMode.None)
            {
                var outlineColor = mode == DrawMarkerMode.Selected ? ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 1.0f, 1.0f)) : ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f));

                drawList.AddTriangleFilled(

                            new Vector2(pmin.X + w / 2, pmin.Y),
                            new Vector2(pmin.X + 0, pmin.Y + h / 2),
                            new Vector2(pmin.X + w, pmin.Y + h / 2),
                            outlineColor);

                drawList.AddRectFilled(new Vector2(pmin.X + 0, pmin.Y + h / 2), new Vector2(pmin.X + w, pmin.Y + h), outlineColor);
            }

            drawList.AddTriangleFilled(

                new Vector2(pmin.X + w / 2, pmin.Y + marginh),
                new Vector2(pmin.X + 0 + margin, pmin.Y + h / 2),
                new Vector2(pmin.X + w - margin, pmin.Y + h / 2),
                color);

            drawList.AddRectFilled(new Vector2(pmin.X + 0 + margin, pmin.Y + h / 2), new Vector2(pmin.X + w - margin, pmin.Y + h - marginh), color);
        }

        static void SortMarkers<T>(List<T> a, int selectedIndex, int draggingIndex) where T : ImGradientHDRState.Marker
        {
            List<SortedMarker> sorted = new List<SortedMarker>();

            for (int i = 0; i < a.Count; i++)
            {
                sorted.Add(new SortedMarker { index = i, marker = a[i] });
            }

            var sortedMarker = sorted.OrderBy(i => i.marker.Position).ToList();

            for (int i = 0; i < a.Count; i++)
            {
                a[i] = (T)sortedMarker[i].marker;
            }

            if (selectedIndex != -1)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    if (sortedMarker[i].index == selectedIndex)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            if (draggingIndex != -1)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    if (sortedMarker[i].index == draggingIndex)
                    {
                        draggingIndex = i;
                        break;
                    }
                }
            }
        }


        static UpdateMarkerResult UpdateMarker<T>(
            List<T> markerArray,
            int markerCount,
            ref ImGradientHDRTemporaryState temporaryState,
            ImGradientHDRMarkerType markerType,
            string keyStr,
            Vector2 originPos,
            float width,
            float markerWidth,
            float markerHeight,
            MarkerDirection markerDir) where T : ImGradientHDRState.Marker
        {
            UpdateMarkerResult ret;
            ret.isChanged = false;
            ret.isHovered = false;

            for (int i = 0; i < markerCount; i++)
            {
                var x = (int)(markerArray[i].Position * width);
                ImGui.SetCursorScreenPos(new Vector2(originPos.X + x - 5, originPos.Y));

                DrawMarkerMode mode;
                if (temporaryState.selectedMarkerType == markerType && temporaryState.selectedIndex == i)
                {
                    mode = DrawMarkerMode.Selected;
                }

                else
                {
                    mode = DrawMarkerMode.Unselected;
                }

                if (markerDir == MarkerDirection.ToLower)
                {
                    ImGradientHDRState.AlphaMarker marker = markerArray[i] as ImGradientHDRState.AlphaMarker;
                    DrawMarker(
                                new Vector2(originPos.X + x - 5, originPos.Y + markerHeight),
                                new Vector2(originPos.X + x + 5, originPos.Y + 0),
                                GetMarkerColor(marker),
                                mode);
                }
                else
                {
                    ImGradientHDRState.ColorMarker marker = markerArray[i] as ImGradientHDRState.ColorMarker;
                    DrawMarker(
                                new Vector2(originPos.X + x - 5, originPos.Y + 0),
                                new Vector2(originPos.X + x + 5, originPos.Y + markerHeight),
                                GetMarkerColor(marker),
                                mode);
                }

                ImGui.InvisibleButton(keyStr + i, new Vector2(markerWidth, markerHeight));

                ret.isHovered |= ImGui.IsItemHovered();

                if (temporaryState.draggingIndex == -1 && ImGui.IsItemHovered() && ImGui.IsMouseDown(0))
                {
                    temporaryState.selectedMarkerType = markerType;
                    temporaryState.selectedIndex = i;
                    temporaryState.draggingMarkerType = markerType;
                    temporaryState.draggingIndex = i;
                }

                if (!ImGui.IsMouseDown(0))
                {
                    temporaryState.draggingIndex = -1;
                    temporaryState.draggingMarkerType = ImGradientHDRMarkerType.Unknown;
                }

                if (temporaryState.draggingMarkerType == markerType && temporaryState.draggingIndex == i && ImGui.IsMouseDragging(0))
                {
                    var diff = ImGui.GetIO().MouseDelta.X / width;
                    markerArray[i].Position += diff;
                    markerArray[i].Position = MathF.Max(MathF.Min(markerArray[i].Position, 1.0f), 0.0f);

                    ret.isChanged |= diff != 0.0f;
                }
            }

            return ret;
        }

        public static bool DrawGradient(int gradientID, ref ImGradientHDRState state, ref ImGradientHDRTemporaryState temporaryState, bool isMarkerShown = true)
        {
            bool changed = false;

            ImGui.PushID(gradientID);

            var originPos = ImGui.GetCursorScreenPos();

            var drawList = ImGui.GetWindowDrawList();

            var margin = 5;

            var width = ImGui.GetContentRegionAvail().X - margin * 2;
            var barHeight = 20;
            var markerWidth = 10;
            var markerHeight = 15;

            if (isMarkerShown)
            {
                var resultAlpha = UpdateMarker(state.Alphas, state.Alphas.Count, ref temporaryState, ImGradientHDRMarkerType.Alpha, "a", originPos, width, markerWidth, markerHeight, MarkerDirection.ToLower);

                changed |= resultAlpha.isChanged;

                if (temporaryState.draggingMarkerType == ImGradientHDRMarkerType.Alpha)
                {
                    SortMarkers(state.Alphas, temporaryState.selectedIndex, temporaryState.draggingIndex);
                }

                ImGui.SetCursorScreenPos(originPos);

                ImGui.InvisibleButton("AlphaArea", new Vector2(width, markerHeight));

                if (ImGui.IsItemHovered())
                {
                    float x = (ImGui.GetIO().MousePos.X - originPos.X);
                    float xn = (ImGui.GetIO().MousePos.X - originPos.X) / width;
                    var c = state.GetAlpha(xn);

                    if (!resultAlpha.isHovered && state.Alphas.Count < state.Alphas.Count)
                    {
                        DrawMarker(
                        new Vector2(originPos.X + x - 5, originPos.Y + markerHeight),
                        new Vector2(originPos.X + x + 5, originPos.Y + 0),
                        ImGui.GetColorU32(new Vector4(c, c, c, 0.5f)),
                        DrawMarkerMode.None);
                    }

                    if (ImGui.IsMouseClicked(0))
                    {
                        changed |= state.AddAlphaMarker(xn, c);
                    }
                }
            }

            var barOriginPos = ImGui.GetCursorScreenPos();

            ImGui.Dummy(new Vector2(width, (barHeight)));

            int gridSize = 10;

            drawList.AddRectFilled(new Vector2(barOriginPos.X - 2, barOriginPos.Y - 2),
                                    new Vector2(barOriginPos.X + width + 2, barOriginPos.Y + barHeight + 2),
                                    ImGui.GetColorU32(new Vector4(100, 100, 100, 255) * ByteToFloat));

            for (int y = 0; y * gridSize < barHeight; y += 1)
            {
                for (int x = 0; x * gridSize < width; x += 1)
                {
                    int wgrid = (int)MathF.Round(MathF.Min(gridSize, (width) - x * gridSize));
                    int hgrid = (int)MathF.Round(MathF.Min(gridSize, barHeight - y * gridSize));
                    uint color = ImGui.GetColorU32(new Vector4(100, 100, 100, 255) * ByteToFloat);

                    if ((x + y) % 2 == 0)
                    {
                        color = ImGui.GetColorU32(new Vector4(50, 50, 50, 255) * ByteToFloat);
                    }

                    drawList.AddRectFilled(new Vector2(barOriginPos.X + x * gridSize, barOriginPos.Y + y * gridSize),
                                            new Vector2(barOriginPos.X + x * gridSize + wgrid, barOriginPos.Y + y * gridSize + hgrid),
                                            color);
                }
            }

            {
                List<float> xkeys = new List<float>();

                for (int i = 0; i < state.Colors.Count; i++)
                {
                    xkeys.Add(state.Colors[i].Position);
                }

                for (int i = 0; i < state.Alphas.Count; i++)
                {
                    xkeys.Add(state.Alphas[i].Position);
                }

                xkeys.Add(0.0f);
                xkeys.Add(1.0f);

                var result = xkeys.Distinct().ToList();
                result.Sort();

                xkeys = result;

                for (int i = 0; i < xkeys.Count - 1; i++)
                {
                    var c1 = state.GetCombinedColor(xkeys[i]);
                    var c2 = state.GetCombinedColor(xkeys[i + 1]);

                    var colorAU32 = ImGui.GetColorU32(c1);
                    var colorBU32 = ImGui.GetColorU32(c2);

                    drawList.AddRectFilledMultiColor(new Vector2(barOriginPos.X + xkeys[i] * width, barOriginPos.Y),
                                                      new Vector2(barOriginPos.X + xkeys[i + 1] * width, barOriginPos.Y + barHeight),
                                                      colorAU32,
                                                      colorBU32,
                                                      colorBU32,
                                                      colorAU32);
                }
            }

            if (isMarkerShown)
            {
                originPos = ImGui.GetCursorScreenPos();

                var resultColor = UpdateMarker(state.Colors, state.Colors.Count, ref temporaryState, ImGradientHDRMarkerType.Color, "c", originPos, width, markerWidth, markerHeight, MarkerDirection.ToUpper);

                changed |= resultColor.isChanged;

                if (temporaryState.draggingMarkerType == ImGradientHDRMarkerType.Color)
                {
                    SortMarkers(state.Colors, temporaryState.selectedIndex, temporaryState.draggingIndex);
                }

                ImGui.SetCursorScreenPos(originPos);

                ImGui.InvisibleButton("ColorArea", new Vector2(width, markerHeight));

                if (ImGui.IsItemHovered())
                {
                    float x = (ImGui.GetIO().MousePos.X - originPos.X);
                    float xn = x / width;
                    var c = state.GetColorAndIntensity(xn);

                    if (!resultColor.isHovered && state.Colors.Count < state.Colors.Count)
                    {
                        DrawMarker(

                                    new Vector2(originPos.X + x - 5, originPos.Y + 0),
                                    new Vector2(originPos.X + x + 5, originPos.Y + markerHeight),
                                    ImGui.GetColorU32(new Vector4(c.X, c.Y, c.Z, 0.5f)),
                                    DrawMarkerMode.None);
                    }

                    if (ImGui.IsMouseClicked(0))
                    {
                        changed |= state.AddColorMarker(xn, new Vector3(c.X, c.Y, c.Z), c.W);
                    }
                }
            }

            var lastOriginPos = ImGui.GetCursorScreenPos();

            ImGui.SetCursorScreenPos(barOriginPos);

            ImGui.Dummy(new Vector2(width, (barHeight)));

            ImGui.SetCursorScreenPos(lastOriginPos);

            ImGui.PopID();

            return changed;
        }

        static uint GetMarkerColor(ImGradientHDRState.ColorMarker marker)
        {
            var c = marker.Color;
            return ImGui.GetColorU32(new Vector4(marker.Color, 1f));
        }

        static uint GetMarkerColor(ImGradientHDRState.AlphaMarker marker)
        {
            var c = marker.Alpha;
            return ImGui.GetColorU32(new Vector4(c, c, c, 1.0f));
        }
    }


    public struct ImGradientHDRState
    {
        const int MarkerMax = 8;

        public class Marker
        {
            public float Position;
        }

        public class ColorMarker : Marker
        {
            public Vector3 Color;
            public float Intensity;
        }

        public class AlphaMarker : Marker
        {
            public float Alpha;
        }

        public List<ColorMarker> Colors;
        public List<AlphaMarker> Alphas;

        public ImGradientHDRState()
        {
            Colors = new List<ColorMarker>();
            Alphas = new List<AlphaMarker>();
        }

        public ColorMarker GetColorMarker(int index)
        {
            if (index < 0 || index >= Colors.Count)
            {
                return null;
            }

            return Colors[index];
        }

        public AlphaMarker GetAlphaMarker(int index)
        {
            if (index < 0 || index >= Alphas.Count)
            {
                return null;
            }

            return Alphas[index];
        }

        void AddMarker<T>(List<T> list, T value) where T : Marker
        {
            var nextItem = list.FirstOrDefault(c => c.Position > value.Position);
            if (nextItem == null)
                list.Add(value);
            else
            {
                int nextInd = list.IndexOf(nextItem);
                list.Insert(nextInd, value);
            }
        }

        public bool AddColorMarker(float x, Vector3 color, float intensity)
        {
            if (Colors.Count >= MarkerMax)
            {
                return false;
            }

            var marker = new ColorMarker()
            {
                Position = x,
                Color = color,
                Intensity = intensity
            };

            AddMarker(Colors, marker);

            return true;
        }

        public bool AddAlphaMarker(float x, float alpha)
        {
            if (Alphas.Count >= MarkerMax)
            {
                return false;
            }

            x = MathF.Max(MathF.Min(x, 1.0f), 0.0f);

            var marker = new AlphaMarker
            {
                Position = x,
                Alpha = alpha
            };

            AddMarker(Alphas, marker);

            return true;
        }

        public bool RemoveColorMarker(int index)
        {
            if (index >= Colors.Count || index < 0)
            {
                return false;
            }

            Colors.RemoveAt(index);
            return true;
        }

        public bool RemoveAlphaMarker(int index)
        {
            if (index >= Alphas.Count || index < 0)
            {
                return false;
            }

            Alphas.RemoveAt(index);
            return true;
        }

        public Vector4 GetCombinedColor(float x)
        {
            var c = GetColorAndIntensity(x);
            return new Vector4(c.X * c.W, c.Y * c.W, c.Z * c.W, GetAlpha(x));
        }

        public Vector4 GetColorAndIntensity(float x)
        {
            if (Colors.Count == 0)
            {
                return Vector4.One;
            }

            if (x < Colors.First().Position)
            {
                ColorMarker c = Colors.First();
                return new Vector4(c.Color, c.Intensity);
            }

            if (Colors.Last().Position <= x)
            {
                ColorMarker c = Colors.Last();
                return new Vector4(c.Color, c.Intensity);
            }

            var it = Colors.FirstOrDefault(c => c.Position >= x);
            int ind = Colors.IndexOf(it);

            {
                if (Colors[ind].Position != x)
                {
                    ind--;
                }

                if (Colors[ind].Position <= x && x <= Colors[ind + 1].Position)
                {
                    var area = Colors[ind + 1].Position - Colors[ind].Position;
                    if (area == 0)
                    {
                        ColorMarker c = Colors[ind];
                        return new Vector4(c.Color, c.Intensity);
                    }

                    var alpha = (x - Colors[ind].Position) / area;
                    Vector3 color = Vector3.Lerp(Colors[ind].Color, Colors[ind + 1].Color, alpha);
                    var intensity = Colors[ind + 1].Intensity * alpha + Colors[ind].Intensity * (1.0f - alpha);
                    return new Vector4(color, intensity);
                }
                else
                {
                    
                }
            }

            return Vector4.One;
        }

        public float GetAlpha(float x)
        {
            if (Alphas.Count == 0)
            {
                return 1.0f;
            }

            if (x < Alphas.First().Position)
            {
                return Alphas.First().Alpha;
            }

            if (Alphas.Last().Position <= x)
            {
                return Alphas.Last().Alpha;
            }

            var it = Alphas.FirstOrDefault(a => a.Position >= x);
            int ind = Alphas.IndexOf(it);

            {
                if (Alphas[ind].Position != x)
                {
                    ind--;
                }

                if (Alphas[ind].Position <= x && x <= Alphas[ind + 1].Position)
                {
                    var area = Alphas[ind + 1].Position - Alphas[ind].Position;
                    if (area == 0)
                    {
                        return Alphas[ind].Alpha;
                    }

                    var alpha = (x - Alphas[ind].Position) / area;
                    return Alphas[ind + 1].Alpha * alpha + Alphas[ind].Alpha * (1.0f - alpha);
                }
                else
                {
                    
                }
            }

            return 1.0f;
        }


    }

    public enum ImGradientHDRMarkerType
    {
        Color,
        Alpha,
        Unknown
    }

    public struct ImGradientHDRTemporaryState
    {
        public ImGradientHDRMarkerType selectedMarkerType;
        public int selectedIndex;

        public ImGradientHDRMarkerType draggingMarkerType;
        public int draggingIndex;
    }
}

