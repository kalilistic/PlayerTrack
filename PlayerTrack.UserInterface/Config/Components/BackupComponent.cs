using System;
using System.Collections.Generic;
using Dalamud.DrunkenToad.Gui;
using Dalamud.DrunkenToad.Gui.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Helpers;

namespace PlayerTrack.UserInterface.Config.Components;

using Dalamud.Interface.Utility;

public class BackupComponent : ConfigViewComponent
{
    private List<Backup> backups = null!;
    private Tuple<ActionRequest, Backup>? backupToDelete;
    private bool showError;

    public override void Draw()
    {
        this.FetchBackups();
        ImGui.BeginChild("Backup");
        this.DrawErrorOrBackupList();
        DrawBackupControls();
        ImGui.EndChild();
    }

    private static void DrawBackupErrorMessage() => LocGui.TextColored("BackupErrorMessage", ImGuiColors.DalamudRed);

    private static void DrawNoBackupMessage() => LocGui.TextColored("NoBackupsMessage", ImGuiColors.DalamudYellow);

    private static void DrawBackupControls()
    {
        ImGuiHelpers.ScaledDummy(5f);
        if (LocGui.Button("RunBackup"))
        {
            ServiceContext.BackupService.RunBackup(BackupType.Manual);
        }

        ImGui.SameLine();
        if (LocGui.Button("RunBackupCleanup"))
        {
            ServiceContext.BackupService.AutoDeleteBackups();
        }
    }

    private void FetchBackups() => this.backups = BackupService.GetBackups();

    private void DrawErrorOrBackupList()
    {
        if (this.showError)
        {
            DrawBackupErrorMessage();
        }
        else if (this.backups.Count == 0)
        {
            DrawNoBackupMessage();
        }
        else
        {
            this.DrawBackupList();
        }
    }

    private void DrawBackupList()
    {
        var headers = ServiceContext.Localization.GetStrings(new[]
        {
            "Type", "Name", "Created", "Size", "Delete",
        });

        if (ImGui.BeginTable("BackupTable", headers.Length, ImGuiTableFlags.None))
        {
            ToadGui.TableSetupColumn("Backup_Table_Col_1", ImGuiTableColumnFlags.WidthFixed, 80f);
            ToadGui.TableSetupColumn("Backup_Table_Col_2", ImGuiTableColumnFlags.WidthFixed, 240f);
            ToadGui.TableSetupColumn("Backup_Table_Col_3", ImGuiTableColumnFlags.WidthFixed, 100f);
            ToadGui.TableSetupColumn("Backup_Table_Col_4", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("Backup_Table_Col_5");

            foreach (var header in headers)
            {
                ImGui.TableNextColumn();
                LocGui.TextColored(header, ImGuiColors.DalamudViolet);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            foreach (var backup in this.backups)
            {
                this.DrawBackupRow(backup);
            }

            ImGui.EndTable();
        }
    }

    private void DrawBackupRow(Backup backup)
    {
        LocGui.Text(backup.BackupType.ToString());
        ImGui.TableNextColumn();
        ImGui.Text(backup.DisplayName);
        ImGui.TableNextColumn();
        ImGui.Text(backup.Created.ToTimeSpan());
        ImGui.TableNextColumn();
        ImGui.Text(backup.Size.FormatFileSize());
        ImGui.TableNextColumn();

        this.HandleBackupDeletion(backup);
        ImGui.TableNextColumn();
    }

    private void HandleBackupDeletion(Backup backup)
    {
        ToadGui.Confirm(backup, FontAwesomeIcon.Trash, "ConfirmDelete", ref this.backupToDelete);
        if (this.backupToDelete?.Item1 == ActionRequest.Confirmed)
        {
            this.DeleteBackup();
        }
        else if (this.backupToDelete?.Item1 == ActionRequest.None)
        {
            this.backupToDelete = null;
        }
    }

    private void DeleteBackup()
    {
        var backup = this.backupToDelete?.Item2;
        var result = backup != null && ServiceContext.BackupService.DeleteBackup(backup);
        if (!result)
        {
            this.showError = true;
        }

        this.backupToDelete = null;
    }
}
