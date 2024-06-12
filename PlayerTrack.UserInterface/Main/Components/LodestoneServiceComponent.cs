using System.Numerics;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.UserInterface.Components;
using PlayerTrack.UserInterface.ViewModels;
using PlayerTrack.UserInterface.ViewModels.Mappers;

namespace PlayerTrack.UserInterface.Main.Components;

public class LodestoneServiceComponent : ViewComponent
{
    private LodestoneServiceView serviceView = new();
    private bool hideHistory = true;
    
    public LodestoneServiceComponent()
    {
        BuildLodestoneLookups();
    }

    private void BuildLodestoneLookups()
    {
        serviceView = LodestoneViewMapper.MapLookups(hideHistory);
    }
    
    public override void Draw()
    {
        serviceView.RefreshStatus();
        
        ImGui.BeginChild("Lodestone", new Vector2(0, 0), true);

        LocGui.TextColored("LodestoneService", ImGuiColors.DalamudViolet);
        ImGui.Separator();

        LocGui.Text("ServiceStatus");
        ImGui.SameLine();
        LocGui.TextColored(serviceView.ServiceStatus.ToString(), serviceView.ServiceStatusColor);

        LocGui.Text("InQueue");
        ImGui.SameLine();
        ImGui.Text($"{serviceView.InQueue}");
        
        LocGui.Text("LastRefreshed");
        ImGui.SameLine();
        ImGui.Text($"{serviceView.LastRefreshed}");
        
        if (LocGui.Button("Refresh"))
        {
            BuildLodestoneLookups();
        }
        
        ImGui.SameLine();

        if (ToadGui.Checkbox("HideCompletedLodestoneRequests", ref this.hideHistory))
        {
            BuildLodestoneLookups();
        }

        ImGui.Separator();
        
        if (serviceView.LodestoneLookups.Count == 0)
        {
            LocGui.Text("NoLodestoneLookups");
            ImGui.EndChild();
            return;
        }

        ImGui.Columns(6, "LodestoneColumns", true);
        ImGui.SetColumnWidth(0, 250 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(1, 100 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(2, 100 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(3, 100 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(4, 100 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(5, 100 * ImGuiHelpers.GlobalScale);

        LocGui.Text("Player");
        ImGui.NextColumn();
        LocGui.Text("Created");
        ImGui.NextColumn();
        LocGui.Text("Updated");
        ImGui.NextColumn();
        LocGui.Text("NextAttempt");
        ImGui.NextColumn();
        LocGui.Text("Status");
        ImGui.NextColumn();
        LocGui.Text("Lodestone");
        ImGui.NextColumn();
        ImGui.Separator();

        foreach (var lodestoneLookup in serviceView.LodestoneLookups)
        {
            using (DalamudContext.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                ImGui.Text(lodestoneLookup.TypeIcon);
            }
            ImGui.SameLine();
            ImGui.Text(lodestoneLookup.RequestPlayer);
            if (lodestoneLookup.hasNameWorldChanged)
            {
                ImGui.SameLine();
                using (DalamudContext.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                    LocGui.TextColored(FontAwesomeIcon.InfoCircle.ToIconString(), ImGuiColors.DalamudYellow);
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(lodestoneLookup.ResponsePlayer);
                }
            }
            ImGui.NextColumn();
            ImGui.Text(lodestoneLookup.Created);
            ImGui.NextColumn();
            ImGui.Text(lodestoneLookup.Updated);
            ImGui.NextColumn();
            ImGui.Text(lodestoneLookup.NextAttemptDisplay);
            ImGui.NextColumn();
            ImGui.TextColored(lodestoneLookup.Color, lodestoneLookup.Status);
            ImGui.NextColumn();

            if (!lodestoneLookup.ShowLodestoneButton)
            {
                ImGui.BeginDisabled();
            }
            
            if (ImGui.Button($"Open##{lodestoneLookup.LodestoneId}_{lodestoneLookup.Id}"))
            {
                PlayerLodestoneService.OpenLodestoneProfile(lodestoneLookup.LodestoneId);
            }
            
            if (!lodestoneLookup.ShowLodestoneButton)
            {
                ImGui.EndDisabled();
            }
            
            ImGui.NextColumn();
        }

        ImGui.Columns(1);
        ImGui.EndChild();
    }
}