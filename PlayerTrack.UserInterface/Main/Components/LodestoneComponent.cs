using System.Collections.Generic;
using System.Numerics;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Components;
using PlayerTrack.UserInterface.Helpers;
using PlayerTrack.UserInterface.ViewModels;

namespace PlayerTrack.UserInterface.Main.Components;

public class LodestoneComponent : ViewComponent
{
    private readonly List<LodestoneLookupView> lodestoneLookups = new();
    private int inQueue;
    private string lastRefreshed = string.Empty;
    
    public LodestoneComponent()
    {
        BuildLodestoneLookups();
    }
    
    public void BuildLodestoneLookups()
    {
        lodestoneLookups.Clear();
        inQueue = 0;
        lastRefreshed = UnixTimestampHelper.CurrentTime().ToTimeSpan();
        var lookups = PlayerLodestoneService.GetLodestoneLookups();
        foreach (var lookup in lookups)
        {
            lodestoneLookups.Add(new LodestoneLookupView
            {
                Id = lookup.Id,
                RequestPlayer = $"{lookup.PlayerName}@{DalamudContext.DataManager.GetWorldNameById(lookup.WorldId)}",
                Status = lookup.LodestoneStatus.ToString(),
                Created = lookup.Created.ToTimeSpan(),
                Updated = lookup.Updated.ToTimeSpan(),
                Color = ColorHelper.GetColorByStatus(lookup.LodestoneStatus),
                LodestoneId = lookup.LodestoneId,
                ShowLodestoneButton = lookup.LodestoneStatus == LodestoneStatus.Verified,
                TypeIcon = lookup.LodestoneLookupType == LodestoneLookupType.Batch
                    ? FontAwesomeIcon.PeopleGroup.ToIconString()
                    : FontAwesomeIcon.Redo.ToIconString()
            });

            if (lookup.LodestoneLookupType == LodestoneLookupType.Batch)
            {
                lodestoneLookups[^1].ResponsePlayer = lodestoneLookups[^1].RequestPlayer;
            }
            else if (!string.IsNullOrEmpty(lookup.UpdatedPlayerName) && lookup.UpdatedWorldId > 0)
            {
                lodestoneLookups[^1].ResponsePlayer =
                    $"{lookup.UpdatedPlayerName}@{DalamudContext.DataManager.GetWorldNameById(lookup.UpdatedWorldId)}";
            }
            else
            {
                lodestoneLookups[^1].ResponsePlayer = DalamudContext.LocManager.GetString("NotAvailable");
            }

            if (lookup.LodestoneStatus is LodestoneStatus.Unverified or LodestoneStatus.Failed)
            {
                inQueue++;
            }
        }
    }
    
    public override void Draw()
    {
        ImGui.BeginChild("Lodestone", new Vector2(0, 0), true);

        LocGui.TextColored("LodestoneService", ImGuiColors.DalamudViolet);
        ImGui.Separator();

        LocGui.Text("ServiceStatus");
        ImGui.SameLine();
        if (ServiceContext.LodestoneService.IsUp())
        {
            LocGui.TextColored("ServiceStatusOnline", ImGuiColors.HealerGreen);
        }
        else
        {
            LocGui.TextColored("ServiceStatusOffline", ImGuiColors.DalamudRed);
        }

        LocGui.Text("InQueue");
        ImGui.SameLine();
        ImGui.Text($"{inQueue}");
        
        LocGui.Text("LastRefreshed");
        ImGui.SameLine();
        ImGui.Text($"{lastRefreshed}");
        
        if (LocGui.SmallButton("Refresh"))
        {
            BuildLodestoneLookups();
        }

        ImGui.Separator();
        
        if (lodestoneLookups.Count == 0)
        {
            LocGui.Text("NoLodestoneLookups");
            ImGui.EndChild();
            return;
        }

        ImGui.Columns(6, "LodestoneColumns", true);
        ImGui.SetColumnWidth(0, 220 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(1, 200 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(2, 90 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(3, 90 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(4, 90 * ImGuiHelpers.GlobalScale);
        ImGui.SetColumnWidth(5, 90 * ImGuiHelpers.GlobalScale);

        LocGui.Text("Request");
        ImGui.NextColumn();
        LocGui.Text("Response");
        ImGui.NextColumn();
        LocGui.Text("Status");
        ImGui.NextColumn();
        LocGui.Text("Created");
        ImGui.NextColumn();
        LocGui.Text("Updated");
        ImGui.NextColumn();
        LocGui.Text("Lodestone");
        ImGui.NextColumn();
        ImGui.Separator();

        foreach (var lodestoneLookup in lodestoneLookups)
        {
            using (DalamudContext.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push()) {
                ImGui.Text(lodestoneLookup.TypeIcon);
            }
            ImGui.SameLine();
            ImGui.Text(lodestoneLookup.RequestPlayer);
            ImGui.NextColumn();
            ImGui.Text(lodestoneLookup.ResponsePlayer);
            ImGui.NextColumn();
            ImGui.TextColored(lodestoneLookup.Color, lodestoneLookup.Status);
            ImGui.NextColumn();
            ImGui.Text(lodestoneLookup.Created);
            ImGui.NextColumn();
            ImGui.Text(lodestoneLookup.Updated);
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