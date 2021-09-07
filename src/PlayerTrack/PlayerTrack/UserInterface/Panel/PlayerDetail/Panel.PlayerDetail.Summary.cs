using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Player Detail Summary View.
    /// </summary>
    public partial class Panel
    {
        private string newTag = string.Empty;

        private void PlayerSummary()
        {
            if (this.SelectedPlayer == null) return;

            var sameLineOffset1 = 100f;
            var sameLineOffset2 = 260f;
            var sameLineOffset3 = 360f;

            // FR override for more spacing
            if (PlayerTrackPlugin.PluginInterface.UiLanguage == "fr")
            {
                sameLineOffset1 = 120f;
                sameLineOffset2 = 280f;
                sameLineOffset3 = 450f;
            }

            ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("PlayerInfo", "Player Info"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset2);
            ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("PlayerStats", "Player Stats"));

            ImGui.Text(Loc.Localize("PlayerName", "Name"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset1);
            if (this.SelectedPlayer.Names.Count > 1)
            {
                ImGui.BeginGroup();
                ImGui.Text(this.SelectedPlayer.Names.First());
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors2.ToadYellow, FontAwesomeIcon.InfoCircle.ToIconString());
                ImGui.PopFont();
                ImGui.EndGroup();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(string.Format(
                                         Loc.Localize("PlayerPreviousNames", "Previously known as {0}"),
                                         string.Join(", ", this.SelectedPlayer.Names.Skip(1))));
                }
            }
            else
            {
                ImGui.Text(this.SelectedPlayer.Names!.First());
            }

            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset2);
            ImGui.Text(Loc.Localize("PlayerFirstSeen", "First Seen"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset3);
            ImGui.Text(this.SelectedPlayer.SeenCount != 0 ? this.SelectedPlayer.Created.ToTimeSpan() : "N/A");

            ImGui.Text(Loc.Localize("PlayerHomeworld", "Homeworld"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset1);
            if (this.SelectedPlayer.HomeWorlds.Count > 1)
            {
                ImGui.BeginGroup();
                ImGui.Text(this.SelectedPlayer.HomeWorlds!.First().Value);
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors2.ToadYellow, FontAwesomeIcon.InfoCircle.ToIconString());
                ImGui.PopFont();
                ImGui.EndGroup();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(string.Format(
                                         Loc.Localize("PlayerPreviousWorlds", "Previously on {0}"),
                                         string.Join(", ", this.SelectedPlayer.HomeWorlds.Skip(1).Select(kvp => kvp.Value))));
                }
            }
            else
            {
                ImGui.Text(this.SelectedPlayer.HomeWorlds!.First().Value);
            }

            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset2);
            ImGui.Text(Loc.Localize("PlayerLastSeen", "Last Seen"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset3);
            ImGui.Text(this.SelectedPlayer.SeenCount != 0 ? this.SelectedPlayer.Updated.ToTimeSpan() : "N/A");

            ImGui.Text(Loc.Localize("FreeCompany", "Free Company"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset1);
            ImGui.Text(this.SelectedPlayer.FreeCompany);

            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset2);
            ImGui.Text(Loc.Localize("PlayerLastLocation", "Last Location"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset3);
            ImGui.Text(this.SelectedPlayer.LastLocationName);

            ImGui.Text(Loc.Localize("PlayerLodestone", "Lodestone"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset1);
            if (this.SelectedPlayer.LodestoneStatus != LodestoneStatus.Failed)
            {
                ImGui.Text(this.SelectedPlayer.LodestoneStatus.ToString());
            }
            else
            {
                ImGui.BeginGroup();
                ImGui.Text(this.SelectedPlayer.LodestoneStatus.ToString());
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.DPSRed, FontAwesomeIcon.Redo.ToIconString());
                ImGui.PopFont();
                ImGui.EndGroup();
                if (ImGui.IsItemClicked())
                {
                    this.SelectedPlayer.LodestoneStatus = LodestoneStatus.Unverified;
                    this.SelectedPlayer.LodestoneFailureCount = 0;
                    this.plugin.PlayerService.UpdatePlayerLodestoneState(this.SelectedPlayer);
                    this.plugin.PlayerService.SubmitLodestoneRequest(this.SelectedPlayer);
                }
            }

            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset2);
            ImGui.Text(Loc.Localize("PlayerSeenCount", "Seen Count"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset3);
            ImGui.Text(this.SelectedPlayer.SeenCount != 0 ? this.SelectedPlayer.SeenCount + "x" : "N/A");

            // add tag
            if (this.plugin.Configuration.ShowPlayerTags)
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("PlayerTags", "Tags"));
                ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                ImGui.InputText(
                    "###PlayerTrack_PlayerNotes_InputText",
                    ref this.newTag,
                    20);

                ImGui.SameLine();
                if (ImGui.Button("Add"))
                {
                    if (!string.IsNullOrEmpty(this.newTag))
                    {
                        this.SelectedPlayer.Tags.Add(this.newTag);
                        this.plugin.PlayerService.UpdatePlayerTags(this.SelectedPlayer);
                        this.newTag = string.Empty;
                    }
                }

                // tags
                ImGui.Spacing();
                for (var i = 0; i < this.SelectedPlayer.Tags.Count; i++)
                {
                    if (ImGui.SmallButton(this.SelectedPlayer.Tags[i] + " ×"))
                    {
                        this.SelectedPlayer.Tags.RemoveAt(i);
                        this.plugin.PlayerService.UpdatePlayerTags(this.SelectedPlayer);
                    }

                    ImGui.SameLine();
                }

                if (this.SelectedPlayer.Tags.Count > 0)
                {
                    ImGuiHelpers.ScaledDummy(5f);
                }
            }

            ImGui.Spacing();
            ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("PlayerNotes", "Notes"));
            var notes = this.SelectedPlayer.Notes;
            if (ImGui.InputTextMultiline(
                "###PlayerTrack_PlayerNotes_MultiText",
                ref notes,
                30,
                new Vector2(
                    x: ImGui.GetWindowSize().X - (5f * ImGuiHelpers.GlobalScale),
                    y: -1 - (5f * ImGuiHelpers.GlobalScale))))
            {
                this.SelectedPlayer.Notes = notes;
                this.plugin.PlayerService.UpdatePlayerNotes(this.SelectedPlayer);
            }
        }
    }
}
