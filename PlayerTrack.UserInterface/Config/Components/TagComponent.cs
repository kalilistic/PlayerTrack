using System;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Gui;
using Dalamud.DrunkenToad.Gui.Enums;
using Dalamud.Interface;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;

namespace PlayerTrack.UserInterface.Config.Components;

using Dalamud.Logging;
using UserInterface.Components;

public class TagComponent : ConfigViewComponent
{
    private string tagInput = string.Empty;
    private Tuple<ActionRequest, Tag>? tagToDelete;

    public override void Draw()
    {
        this.DrawTags();
        this.DrawAddTagInput();
    }

    private void DrawTags()
    {
        var tags = ServiceContext.TagService.GetAllTags();

        foreach (var tag in tags)
        {
            this.DrawTag(tag);
        }
    }

    private void DrawTag(Tag tag)
    {
        var tagText = tag.Name;
        ToadGui.SetNextItemWidth(240f);
        if (ToadGui.InputText("###EditTagInput" + tag.Id, ref tagText, 20))
        {
            this.UpdateTagText(tag, tagText);
        }

        ImGui.SameLine();

        var color = DalamudContext.DataManager.GetUIColorAsVector4(tag.Color);
        if (ToadGui.SimpleUIColorPicker("###TagColorPicker" + tag.Id, tag.Color, ref color, false))
        {
            this.UpdateTagColor(tag, color);
        }

        ImGui.SameLine();

        this.DrawTagDeleteConfirmation(tag);
    }

    private void UpdateTagText(Tag tag, string text)
    {
        tag.Name = text;
        ServiceContext.TagService.UpdateTag(tag);
        this.NotifyConfigChanged();
    }

    private void UpdateTagColor(Tag tag, System.Numerics.Vector4 color)
    {
        tag.Color = DalamudContext.DataManager.FindClosestUIColor(color).Id;
        ServiceContext.TagService.UpdateTag(tag);
        this.NotifyConfigChanged();
    }

    private void DrawTagDeleteConfirmation(Tag tag)
    {
        ToadGui.Confirm(tag, FontAwesomeIcon.Trash, "ConfirmDelete", ref this.tagToDelete);

        if (this.tagToDelete?.Item1 == ActionRequest.Confirmed)
        {
            ServiceContext.TagService.DeleteTag(this.tagToDelete.Item2);
            this.tagToDelete = null;
            this.NotifyConfigChanged();
        }
        else if (this.tagToDelete?.Item1 == ActionRequest.None)
        {
            this.tagToDelete = null;
        }
    }

    private void DrawAddTagInput()
    {
        ToadGui.SetNextItemWidth(240f);
        LocGui.InputTextWithHint("###AddTagInput", "NewTagHint", ref this.tagInput, 20);
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        LocGui.Text(FontAwesomeIcon.Plus.ToIconString());

        if (ImGui.IsItemClicked() && !string.IsNullOrEmpty(this.tagInput))
        {
            this.AddNewTag();
        }

        ImGui.PopFont();
    }

    private void AddNewTag()
    {
        ServiceContext.TagService.CreateTag(this.tagInput);
        this.tagInput = string.Empty;
        this.NotifyConfigChanged();
    }
}
