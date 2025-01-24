using System;
using System.Collections.Generic;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Enums;
using PlayerTrack.Models;
using PlayerTrack.Resource;
using PlayerTrack.Windows.Helpers;

namespace PlayerTrack.Windows.Config.Components;

public class BackupComponent : ConfigViewComponent
{
    private readonly List<float> ColumnWidths = [];
    private List<Backup> Backups = null!;
    private Tuple<ActionRequest, Backup>? BackupToDelete;
    private bool ShowError;

    private string[] ColumnsHeaderKeys =>
    [
        Language.Type, Language.Name, Language.Created, Language.Size, Language.Delete
    ];

    public override void Draw()
    {
        FetchBackups();

        using var child = ImRaii.Child("Backup");
        if (!child.Success)
            return;

        DrawErrorOrBackupList();
        DrawBackupControls();
    }

    public void CalcSize()
    {
        var headers = ColumnsHeaderKeys;

        ColumnWidths.Clear();
        var columnPaddings = new[]
        {
            70f * ImGuiHelpers.GlobalScale, // Type
            175f * ImGuiHelpers.GlobalScale, // Name
            50f * ImGuiHelpers.GlobalScale, // Created
            50f * ImGuiHelpers.GlobalScale, // Size
            150f * ImGuiHelpers.GlobalScale // Delete
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var padding = columnPaddings[i];
            var computedWidth = ImGui.CalcTextSize(headers[i]).X + padding;
            ColumnWidths.Add(computedWidth);
        }
    }

    private static void DrawBackupErrorMessage() => Helper.TextColored(ImGuiColors.DalamudRed, Language.BackupErrorMessage);

    private static void DrawNoBackupMessage() => Helper.TextColored(ImGuiColors.DalamudYellow, Language.NoBackupsMessage);

    private static void DrawBackupControls()
    {
        ImGuiHelpers.ScaledDummy(5f);
        if (ImGui.Button(Language.RunBackup))
            ServiceContext.BackupService.RunBackup(BackupType.Manual);

        ImGui.SameLine();

        if (ImGui.Button(Language.RunBackupCleanup))
            ServiceContext.BackupService.AutoDeleteBackups();
    }

    private void FetchBackups() => Backups = BackupService.GetBackups();

    private void DrawErrorOrBackupList()
    {
        if (ShowError)
            DrawBackupErrorMessage();
        else if (Backups.Count == 0)
            DrawNoBackupMessage();
        else
            DrawBackupList();
    }

    private void DrawBackupList()
    {
        var headers = ColumnsHeaderKeys;
        if (ColumnWidths.Count == 0)
            CalcSize();

        using var table = ImRaii.Table("BackupTable", headers.Length, ImGuiTableFlags.None);
        if (table.Success)
        {
            for (var i = 0; i < headers.Length; i++)
                ImGui.TableSetupColumn($"Backup_Table_Col_{i + 1}", ImGuiTableColumnFlags.WidthFixed, ColumnWidths[i] * ImGuiHelpers.GlobalScale);

            foreach (var header in headers)
            {
                ImGui.TableNextColumn();
                Helper.TextColored(ImGuiColors.DalamudViolet, header);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            foreach (var backup in Backups)
                DrawBackupRow(backup);
        }
    }

    private void DrawBackupRow(Backup backup)
    {
        ImGui.TextUnformatted(Utils.GetLoc(backup.BackupType.ToString()));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(backup.DisplayName);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(backup.Created.ToTimeSpan());
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(backup.Size.FormatFileSize());
        ImGui.TableNextColumn();

        HandleBackupDeletion(backup);
        ImGui.TableNextColumn();
    }

    private void HandleBackupDeletion(Backup backup)
    {
        Helper.Confirm(backup, FontAwesomeIcon.Trash, Language.ConfirmDelete, ref BackupToDelete);
        if (BackupToDelete?.Item1 == ActionRequest.Confirmed)
            DeleteBackup();
        else if (BackupToDelete?.Item1 == ActionRequest.None)
            BackupToDelete = null;
    }

    private void DeleteBackup()
    {
        var backup = BackupToDelete?.Item2;
        var result = backup != null && ServiceContext.BackupService.DeleteBackup(backup);
        if (!result)
            ShowError = true;

        BackupToDelete = null;
    }
}
