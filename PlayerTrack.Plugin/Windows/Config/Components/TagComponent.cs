using System;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Enums;
using PlayerTrack.Models;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class TagComponent : ConfigViewComponent
{
    private string TagInput = string.Empty;
    private Tuple<ActionRequest, Tag>? TagToDelete;

    public override void Draw()
    {
        DrawTags();
        DrawAddTagInput();
    }

    private void DrawTags()
    {
        var tags = ServiceContext.TagService.GetAllTags();

        foreach (var tag in tags)
            DrawTag(tag);
    }

    private void DrawTag(Tag tag)
    {
        var tagText = tag.Name;
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText($"###EditTagInput{tag.Id}", ref tagText, 20))
            UpdateTagText(tag, tagText);

        ImGui.SameLine();

        var color = Sheets.GetUiColorAsVector4(tag.Color);
        if (Helper.SimpleUiColorPicker($"###TagColorPicker{tag.Id}", tag.Color, ref color, false))
            UpdateTagColor(tag, color);

        ImGui.SameLine();

        DrawTagDeleteConfirmation(tag);
    }

    private void UpdateTagText(Tag tag, string text)
    {
        tag.Name = text;
        ServiceContext.TagService.UpdateTag(tag);
        NotifyConfigChanged();
    }

    private void UpdateTagColor(Tag tag, System.Numerics.Vector4 color)
    {
        tag.Color = Sheets.FindClosestUiColor(color).Id;
        ServiceContext.TagService.UpdateTag(tag);
        NotifyConfigChanged();
    }

    private void DrawTagDeleteConfirmation(Tag tag)
    {
        Helper.Confirm(tag, FontAwesomeIcon.Trash, Language.ConfirmDelete, ref TagToDelete);

        if (TagToDelete?.Item1 == ActionRequest.Confirmed)
        {
            ServiceContext.TagService.DeleteTag(TagToDelete.Item2);
            TagToDelete = null;
            NotifyConfigChanged();
        }
        else if (TagToDelete?.Item1 == ActionRequest.None)
        {
            TagToDelete = null;
        }
    }

    private void DrawAddTagInput()
    {
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("###AddTagInput", Language.NewTagHint, ref TagInput, 20);
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.Plus.ToIconString());
            if (ImGui.IsItemClicked() && !string.IsNullOrEmpty(TagInput))
                AddNewTag();
        }
    }

    private void AddNewTag()
    {
        ServiceContext.TagService.CreateTag(TagInput);
        TagInput = string.Empty;
        NotifyConfigChanged();
    }
}
