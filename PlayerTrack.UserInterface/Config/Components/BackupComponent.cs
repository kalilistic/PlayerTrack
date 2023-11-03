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
    private readonly List<float> columnWidths = new();
    private readonly List<string> columnHeaderKeys = new()
    {
        "Type", "Name", "Created", "Size", "Delete",
    };
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
    
    public void CalcSize()
    {
        var headers = ServiceContext.Localization.GetStrings(this.columnHeaderKeys.ToArray());
        this.columnWidths.Clear();
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
            this.columnWidths.Add(computedWidth);
        }
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
        var headers = ServiceContext.Localization.GetStrings(this.columnHeaderKeys.ToArray());

        if (this.columnWidths.Count == 0)
        {
            this.CalcSize();
        }

        if (ImGui.BeginTable("BackupTable", headers.Length, ImGuiTableFlags.None))
        {
            for (var i = 0; i < headers.Length; i++)
            {
                var columnID = $"Backup_Table_Col_{i + 1}";
                ToadGui.TableSetupColumn(columnID, ImGuiTableColumnFlags.WidthFixed, this.columnWidths[i]);
            }

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
