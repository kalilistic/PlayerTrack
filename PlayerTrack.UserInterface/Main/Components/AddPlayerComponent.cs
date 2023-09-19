using System.Linq;
using System.Numerics;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Loc.ImGui;
using Dalamud.Utility;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.UserInterface.Components;

namespace PlayerTrack.UserInterface.Main.Components;

public class AddPlayerComponent : ViewComponent
{
    private readonly uint[] worldIds;
    private readonly string[] worldNames;
    private int selectedWorld;
    private string addPlayerInput = string.Empty;
    private bool showInvalidNameError;
    private bool showInvalidWorldError;
    private bool showDuplicatePlayerError;
    private bool showSuccessMessage;

    public AddPlayerComponent()
    {
        var worlds = DalamudContext.DataManager.Worlds;
        var sortedWorldIds = worlds.Select(pair => pair.Value.Id).ToList();
        sortedWorldIds.Insert(0, 0);
        this.worldIds = sortedWorldIds.ToArray();
        var sortedWorldNames = worlds.Select(pair => pair.Value.Name).ToList();
        sortedWorldNames.Insert(0, string.Empty);
        this.worldNames = sortedWorldNames.ToArray();
    }

    public override void Draw()
    {
        ImGui.BeginChild("###AddPlayerManually", new Vector2(-1, 0), false);
        LocGui.TextColored("AddPlayerInstructions", ImGuiColors.DalamudViolet);
        ImGuiHelpers.ScaledDummy(3f);
        ToadGui.SetNextItemWidth(150f);
        if (ToadGui.InputText(
                "PlayerName",
                ref this.addPlayerInput,
                30))
        {
            this.showSuccessMessage = false;
        }

        if (ToadGui.Combo(
                "PlayerWorld",
                ref this.selectedWorld,
                this.worldNames,
                150))
        {
            this.showSuccessMessage = false;
        }

        ImGuiHelpers.ScaledDummy(10f);
        if (LocGui.Button("AddPlayer"))
        {
            this.showInvalidNameError = false;
            this.showInvalidWorldError = false;
            this.showDuplicatePlayerError = false;
            if (this.selectedWorld == 0)
            {
                this.showInvalidWorldError = true;
            }
            else if (this.addPlayerInput.IsValidCharacterName())
            {
                var worldId = this.worldIds[this.selectedWorld];
                var existingPlayer = ServiceContext.PlayerDataService.GetPlayer(this.addPlayerInput, worldId);
                if (existingPlayer != null)
                {
                    this.showDuplicatePlayerError = true;
                }
                else
                {
                    PlayerProcessService.CreateNewPlayer(this.addPlayerInput, worldId);
                    this.addPlayerInput = string.Empty;
                    this.showSuccessMessage = true;
                }
            }
            else
            {
                this.showInvalidNameError = true;
            }
        }

        ImGui.Spacing();
        if (this.showInvalidWorldError)
        {
            LocGui.TextColored("NoWorldError", ImGuiColors.DPSRed);
        }
        else if (this.showInvalidNameError)
        {
            LocGui.TextColored("InvalidNameError", ImGuiColors.DPSRed);
        }
        else if (this.showDuplicatePlayerError)
        {
            LocGui.TextColored("DuplicatePlayerError", ImGuiColors.DPSRed);
        }
        else if (this.showSuccessMessage)
        {
            LocGui.TextColored("AddedPlayerSuccessfully", ImGuiColors.HealerGreen);
        }

        ImGui.EndChild();
    }
}
