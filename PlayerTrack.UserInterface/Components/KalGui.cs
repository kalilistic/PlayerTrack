namespace PlayerTrack.UserInterface.Components;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.DrunkenToad.Core;
using Dalamud.Loc.ImGui;
using ImGuiNET;

// move to ToadGui after v9
public static class KalGui
{
    public static bool SimpleUIColorPicker(string key, uint colorId, ref Vector4 color, bool includeLabel = true)
    {
        var id = $"###{key}";
        var originalColor = color;
        var itemsPerRow = (int)Math.Sqrt(DalamudContext.DataManager.UIColors.Count);
        var currentItemCount = 0;
        var addedColors = new HashSet<Vector4>(); // Use Vector4 directly to ease comparison

        if (ImGui.ColorButton($"{id}_UIColorButton", color, ImGuiColorEditFlags.NoTooltip))
        {
            ImGui.OpenPopup($"{id}_UIColorButton_Popup");
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"UIColor #{colorId}");
        }

        if (ImGui.BeginPopup($"{id}_UIColorButton_Popup"))
        {
            var sortedColors = DalamudContext.DataManager.UIColors
                .OrderBy(pair => ColorToHue(DalamudContext.DataManager.GetUIColorAsVector4(pair.Value.Id)))
                .ToList();

            foreach (var uiColorPair in sortedColors)
            {
                var buttonColor = DalamudContext.DataManager.GetUIColorAsVector4(uiColorPair.Value.Id);

                if (addedColors.Any(existing => AreColorsSimilar(existing, buttonColor)))
                {
                    continue;
                }

                if (ImGui.ColorButton($"{id}_UIColorButton_{uiColorPair.Value.Id}", buttonColor))
                {
                    color = buttonColor;
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"UIColor #{uiColorPair.Value.Id}");
                }

                currentItemCount++;
                if (currentItemCount % itemsPerRow != 0)
                {
                    ImGui.SameLine();
                }

                addedColors.Add(buttonColor);
            }

            ImGui.EndPopup();
        }

        if (includeLabel)
        {
            ImGui.SameLine();
            LocGui.Text(key);
        }

        return color != originalColor;
    }

    private static bool AreColorsSimilar(Vector4 a, Vector4 b, float tolerance = 0.05f) => Math.Abs(a.X - b.X) <
        tolerance && Math.Abs(a.Y - b.Y) < tolerance && Math.Abs(a.Z - b.Z) < tolerance;

    private static float ColorToHue(Vector4 color)
    {
        var r = color.X;
        var g = color.Y;
        var b = color.Z;

        var min = Math.Min(r, Math.Min(g, b));
        var max = Math.Max(r, Math.Max(g, b));

        var delta = max - min;
        var hue = 0f;

        if (!(Math.Abs(delta) > float.Epsilon))
        {
            return hue;
        }

        if (Math.Abs(max - r) < float.Epsilon)
        {
            hue = (g - b) / delta;
        }
        else if (Math.Abs(max - g) < float.Epsilon)
        {
            hue = 2 + ((b - r) / delta);
        }
        else if (Math.Abs(max - b) < float.Epsilon)
        {
            hue = 4 + ((r - g) / delta);
        }

        hue = ((hue * 60) + 360) % 360;

        return hue;
    }
}
