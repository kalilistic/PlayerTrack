using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Resource;
using PlayerTrack.Windows.Components;

namespace PlayerTrack.Windows.Main.Components;

public class AddPlayerComponent : ViewComponent
{
    private readonly uint[] WorldIds;
    private readonly string[] WorldNames;
    private int SelectedWorld;
    private string AddPlayerInput = string.Empty;
    private bool ShowInvalidNameError;
    private bool ShowInvalidWorldError;
    private bool ShowDuplicatePlayerError;
    private bool ShowSuccessMessage;

    public AddPlayerComponent()
    {
        WorldIds = Sheets.Worlds.Select(pair => pair.Value.Id).Prepend(0u).ToArray();
        WorldNames = Sheets.Worlds.Select(pair => pair.Value.Name).Prepend(string.Empty).ToArray();
    }

    public override void Draw()
    {
        using var child = ImRaii.Child("###AddPlayerManually", new Vector2(-1, 0), false);
        if (!child.Success)
            return;

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.AddPlayerInstructions);
        ImGuiHelpers.ScaledDummy(3f);
        ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText(Language.PlayerName, ref AddPlayerInput, 30))
            ShowSuccessMessage = false;

        if (Helper.Combo(Language.PlayerWorld, ref SelectedWorld, WorldNames, 150))
            ShowSuccessMessage = false;

        ImGuiHelpers.ScaledDummy(10f);
        if (ImGui.Button(Language.AddPlayer))
        {
            ShowInvalidNameError = false;
            ShowInvalidWorldError = false;
            ShowDuplicatePlayerError = false;

            if (SelectedWorld == 0)
            {
                ShowInvalidWorldError = true;
            }
            else if (AddPlayerInput.IsValidCharacterName())
            {
                var worldId = WorldIds[SelectedWorld];
                var existingPlayer = ServiceContext.PlayerDataService.GetPlayer(AddPlayerInput, worldId);
                if (existingPlayer != null)
                {
                    ShowDuplicatePlayerError = true;
                }
                else
                {
                    PlayerProcessService.CreateNewPlayer(AddPlayerInput, worldId, 0, false);
                    AddPlayerInput = string.Empty;
                    ShowSuccessMessage = true;
                }
            }
            else
            {
                ShowInvalidNameError = true;
            }
        }

        ImGui.Spacing();
        if (ShowInvalidWorldError)
            Helper.TextColored(ImGuiColors.DPSRed, Language.NoWorldError);
        else if (ShowInvalidNameError)
            Helper.TextColored(ImGuiColors.DPSRed, Language.InvalidNameError);
        else if (ShowDuplicatePlayerError)
            Helper.TextColored(ImGuiColors.DPSRed, Language.DuplicatePlayerError);
        else if (ShowSuccessMessage)
            Helper.TextColored(ImGuiColors.HealerGreen, Language.PlayerAddedSuccessfully);
    }
}
