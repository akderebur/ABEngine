using System;
using System.Net;
using System.Numerics;
using ABEngine.ABERuntime.Core.Math;
using ImGuiNET;

namespace ABEngine.ABEditor.PropertyDrawers
{
	public class CurveEditor
	{
		public CurveEditor()
		{
		
		}

        public static void Draw(BezierCurve curve)
        {
            float scale = curve.scale;
            float offset = curve.offset;

            Vector2 startPoint = curve.StartPoint;
            Vector2 endPoint = curve.EndPoint;
            Vector2 controlPoint1 = curve.ControlPoint1;
            Vector2 controlPoint2 = curve.ControlPoint2;

            float width = ImGui.GetWindowWidth();
            float height = width * 0.75f;

            Vector2 canvasSize = new Vector2(width, height) - new Vector2(width / 10f, 20);
            Vector2 canvasPos = ImGui.GetCursorScreenPos();

            // Draw the background grid
            for (int i = 0; i <= 10; i++)
            {
                float x = canvasPos.X + i * (canvasSize.X / 10);
                float y = canvasPos.Y + i * (canvasSize.Y / 10);
                ImGui.GetWindowDrawList().AddLine(new Vector2(x, canvasPos.Y), new Vector2(x, canvasPos.Y + canvasSize.Y), ImGui.GetColorU32(ImGuiCol.Border));
                ImGui.GetWindowDrawList().AddLine(new Vector2(canvasPos.X, y), new Vector2(canvasPos.X + canvasSize.X, y), ImGui.GetColorU32(ImGuiCol.Border));
            }

            // Draw labels for the x-axis
            ImGui.GetWindowDrawList().AddText(new Vector2(ToCanvas(startPoint).X - 5, ToCanvas(new Vector2(0, 0)).Y + 5), ImGui.GetColorU32(ImGuiCol.Text), "" + 0);
            ImGui.GetWindowDrawList().AddText(new Vector2(ToCanvas(endPoint).X - 5, ToCanvas(new Vector2(0, 0)).Y + 5), ImGui.GetColorU32(ImGuiCol.Text), "" + 1);

            // Draw labels for the y-axis
            ImGui.GetWindowDrawList().AddText(new Vector2(ToCanvas(startPoint).X - 5, ToCanvas(new Vector2(0, 0)).Y - 5), ImGui.GetColorU32(ImGuiCol.Text), "" + offset);
            ImGui.GetWindowDrawList().AddText(new Vector2(ToCanvas(startPoint).X - 5, ToCanvas(new Vector2(0, 1)).Y - 5), ImGui.GetColorU32(ImGuiCol.Text), "" + (offset + scale));

            Vector2 ToCanvas(Vector2 point) => new Vector2(canvasPos.X + point.X * canvasSize.X, canvasPos.Y + (1f - point.Y) * canvasSize.Y);
            Vector2 FromCanvas(Vector2 point) => new Vector2((point.X - canvasPos.X) / canvasSize.X, 1f - (point.Y - canvasPos.Y) / canvasSize.Y);

            // Draw the Bezier curve
            ImGui.GetWindowDrawList().AddBezierCubic(
                ToCanvas(startPoint),
                ToCanvas(controlPoint1),
                ToCanvas(controlPoint2),
                ToCanvas(endPoint),
                ImGui.GetColorU32(ImGuiCol.PlotLines),
                2
            );

            // Draw and handle interaction for the start, end, and control points
            Vector2[] points = new[] { startPoint, endPoint, controlPoint1, controlPoint2 };
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 screenPoint = ToCanvas(points[i]);
                ImGui.SetCursorScreenPos(screenPoint - new Vector2(4, 4));
                ImGui.InvisibleButton($"point{i}", new Vector2(12, 12));

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    Vector2 newPosition = FromCanvas(ImGui.GetIO().MousePos);

                    if (i < 2) // start and end points
                    {
                        newPosition.X = points[i].X; // Restrict movement to vertical only
                    }
                    newPosition.X = Math.Clamp(newPosition.X, 0, 1);
                    newPosition.Y = Math.Clamp(newPosition.Y, 0, 1);
                    points[i] = newPosition;
                }

                ImGui.GetWindowDrawList().AddCircleFilled(screenPoint, 4, ImGui.GetColorU32(ImGuiCol.PlotLinesHovered), 12);
            }

            ImGui.SetCursorScreenPos(canvasPos + Vector2.UnitY * (canvasSize.Y + 20f));

            ImGui.Text("Offset");
            ImGui.SameLine();
            if (ImGui.InputFloat("##Offset", ref offset))
                curve.offset = offset;

            ImGui.Text("Scale");
            ImGui.SameLine();
            if (ImGui.InputFloat("##Scale", ref scale))
                curve.scale = scale;

            curve.StartPoint = points[0];
            curve.EndPoint = points[1];
            curve.ControlPoint1 = points[2];
            curve.ControlPoint2 = points[3];
        }
    }
}

