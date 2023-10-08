namespace PlayerTrack.UserInterface.Config.Components;

using System;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Gui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Loc.ImGui;
using Domain;
using ImGuiNET;
using Infrastructure;
using Models;

public class DataComponent : ConfigViewComponent
{
    private int selectedActionIndex;
    private int itemsToDeleteCount;
    private int totalItemsCount;
    private Task? deleteTask;
    private string statusMessage = string.Empty;
    private Vector4 statusColor = Vector4.Zero;
    private bool isDirty = true;
    private string sqlQuery = string.Empty;
    private string sqlResult = string.Empty;
    private string sqlResultDisplay = string.Empty;

    public override void Draw() => this.DrawControls();

    private void DrawControls()
    {
        if (ImGui.BeginTabBar("###Data_TabBar", ImGuiTabBarFlags.None))
        {
            this.DrawPurge();
            this.DrawSQLExecutor();
        }
    }

    private void DrawPurge()
    {
        if (LocGui.BeginTabItem("Purge"))
        {
            this.DrawSelectAction();
            this.DrawActionOptions();
            this.DrawGlobalOptions();
            this.DrawRun();
            this.DrawMessage();
            ImGui.EndTabItem();
        }
    }

    private void DrawSQLExecutor()
    {
        if (LocGui.BeginTabItem("SQLExecutor"))
        {
            ImGuiHelpers.ScaledDummy(1f);
            var query = this.sqlQuery;
            if (ImGui.InputTextMultiline("##SQLInput", ref query, 1000, ImGuiHelpers.ScaledVector2(-1, 90), ImGuiInputTextFlags.None))
            {
                this.sqlQuery = query;
            }

            ImGuiHelpers.ScaledDummy(1f);

            if (ImGui.BeginChild("##SQLResultChild", ImGuiHelpers.ScaledVector2(-1, 90), true, ImGuiWindowFlags.None))
            {
                ImGui.TextUnformatted(this.sqlResultDisplay);
                ImGui.EndChild();
            }

            ImGuiHelpers.ScaledDummy(1f);
            if (LocGui.Button("Execute"))
            {
                this.ExecuteSQL();
            }

            ImGui.SameLine();

            if (LocGui.Button("CopyToClipboard"))
            {
                ImGui.SetClipboardText(this.sqlResult);
            }

            ImGui.EndTabItem();
        }
    }

    private void ExecuteSQL()
    {
        this.sqlResult = string.Empty;
        this.sqlResultDisplay = string.Empty;
        if (string.IsNullOrEmpty(this.sqlQuery))
        {
            return;
        }

        this.sqlResult = RepositoryContext.ExecuteSQLQuery(this.sqlQuery);
        this.sqlResultDisplay = this.sqlResult.Length > 1000 ? this.sqlResult[..1000] : this.sqlResult;
    }

    private void DrawSelectAction()
    {
        var actions = Enum.GetNames(typeof(DataActionType));
        if (ToadGui.Combo("Actions", ref this.selectedActionIndex, actions, 180))
        {
            this.isDirty = true;
        }

        ImGui.Spacing();
    }

    private void DrawMessage()
    {
        ImGui.SameLine();
        if (!string.IsNullOrEmpty(this.statusMessage))
        {
            ImGui.TextColored(this.statusColor, this.statusMessage);
        }
        else if (this.itemsToDeleteCount != 0 && this.totalItemsCount != 0 && this.deleteTask == null)
        {
            var percentageDelete = (float)this.itemsToDeleteCount / this.totalItemsCount * 100;
            percentageDelete = (float)Math.Round(percentageDelete, 2);
            var deleteMessage = string.Format(
                ServiceContext.Localization.GetString("NumberOfRecordsToBeDeleted"),
                this.itemsToDeleteCount.ToString("N0", CultureInfo.CurrentCulture),
                this.totalItemsCount.ToString("N0", CultureInfo.CurrentCulture),
                percentageDelete);
            ImGui.TextColored(ImGuiColors.DalamudRed, deleteMessage);
            ImGui.Spacing();
        }
        else
        {
            LocGui.TextColored("NoRecordsToDelete", ImGuiColors.DalamudYellow);
        }
    }

    private void DrawRun()
    {
        if (LocGui.BeginPopup("Confirmation"))
        {
            LocGui.Text("ConfirmationQuestion");
            ImGui.Separator();
            if (LocGui.Button("Yes"))
            {
                ImGui.CloseCurrentPopup();
                this.statusMessage = ServiceContext.Localization.GetString("DeleteRecordsInProgress");
                this.statusColor = ImGuiColors.DalamudYellow;
                this.deleteTask = (DataActionType)this.selectedActionIndex switch
                {
                    DataActionType.DeletePlayers => Task.Run(() =>
                        {
                            this.RunBackup();
                            ServiceContext.PlayerDataService.DeletePlayers();
                            this.HandleTaskSuccess();
                        })
                        .ContinueWith(_ =>
                        {
                            this.HandlePostTaskSuccess();
                            this.itemsToDeleteCount = ServiceContext.PlayerDataService.GetPlayersForDeletionCount();
                            this.totalItemsCount = ServiceContext.PlayerDataService.GetAllPlayersCount();
                        }),
                    DataActionType.DeletePlayerSettings => Task.Run(() =>
                        {
                            this.RunBackup();
                            ServiceContext.PlayerDataService.DeletePlayerConfigs();
                            this.HandleTaskSuccess();
                        })
                        .ContinueWith(_ =>
                        {
                            this.HandlePostTaskSuccess();
                            this.itemsToDeleteCount = ServiceContext.PlayerDataService.GetPlayerConfigsForDeletionCount();
                            this.totalItemsCount = ServiceContext.PlayerDataService.GetPlayerConfigCount();
                        }),
                    DataActionType.DeleteEncounters => Task.Run(() =>
                        {
                            this.RunBackup();
                            ServiceContext.EncounterService.DeleteEncounters();
                            this.HandleTaskSuccess();
                        })
                        .ContinueWith(_ =>
                        {
                            this.HandlePostTaskSuccess();
                            this.itemsToDeleteCount = ServiceContext.EncounterService.GetEncountersForDeletionCount();
                            this.totalItemsCount = EncounterService.GetEncountersCount();
                        }),
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            ImGui.SameLine();
            if (LocGui.Button("No"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void RunBackup()
    {
        if (this.config.RunBackupBeforeDataActions)
        {
            ServiceContext.BackupService.RunBackup(BackupType.Automatic);
        }
    }

    private void HandleTaskSuccess()
    {
        this.statusMessage = ServiceContext.Localization.GetString("DeleteRecordsSuccess");
        this.statusColor = ImGuiColors.HealerGreen;
    }

    private void HandlePostTaskSuccess()
    {
        this.deleteTask = null;
        this.itemsToDeleteCount = 0;
        this.totalItemsCount = 0;
        this.isDirty = true;
    }

    private void DrawGlobalOptions()
    {
        var runBackupBeforeDataActions = this.config.RunBackupBeforeDataActions;
        if (ToadGui.Checkbox("RunBackupBeforeDataActions", ref runBackupBeforeDataActions))
        {
            this.config.RunBackupBeforeDataActions = runBackupBeforeDataActions;
            ServiceContext.ConfigService.SaveConfig(this.config);
            this.isDirty = true;
        }

        ImGui.Spacing();

        var isDisabled = this.deleteTask != null || this.itemsToDeleteCount == 0 || !string.IsNullOrEmpty(this.statusMessage);
        ImGui.BeginDisabled(isDisabled);

        if (LocGui.Button("RunAction") && !isDisabled)
        {
            ImGui.OpenPopup("Confirmation");
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
    }

    private void DrawActionOptions()
    {
        switch ((DataActionType)this.selectedActionIndex)
        {
            case DataActionType.DeletePlayers:
                if (this.isDirty)
                {
                    this.isDirty = false;
                    this.itemsToDeleteCount = ServiceContext.PlayerDataService.GetPlayersForDeletionCount();
                    this.totalItemsCount = ServiceContext.PlayerDataService.GetAllPlayersCount();
                }

                LocGui.TextColored("KeepPlayers", ImGuiColors.DalamudViolet);
                ImGui.Indent(10);
                foreach (var property in this.config.PlayerDataActionOptions.GetType().GetProperties())
                {
                    var currentValue = (bool)(property.GetValue(this.config.PlayerDataActionOptions) ?? true);
                    if (ToadGui.Checkbox(property.Name, ref currentValue))
                    {
                        property.SetValue(this.config.PlayerDataActionOptions, currentValue);
                        ServiceContext.ConfigService.SaveConfig(this.config);
                        this.isDirty = true;
                        this.statusMessage = string.Empty;
                    }
                }

                break;

            case DataActionType.DeletePlayerSettings:
                if (this.isDirty)
                {
                    this.isDirty = false;
                    this.itemsToDeleteCount = ServiceContext.PlayerDataService.GetPlayerConfigsForDeletionCount();
                    this.totalItemsCount = ServiceContext.PlayerDataService.GetPlayerConfigCount();
                }

                LocGui.TextColored("KeepPlayerSettings", ImGuiColors.DalamudViolet);
                ImGui.Indent(10);
                foreach (var property in this.config.PlayerSettingsDataActionOptions.GetType().GetProperties())
                {
                    var currentValue = (bool)(property.GetValue(this.config.PlayerSettingsDataActionOptions) ?? true);
                    if (ToadGui.Checkbox(property.Name, ref currentValue))
                    {
                        property.SetValue(this.config.PlayerSettingsDataActionOptions, currentValue);
                        ServiceContext.ConfigService.SaveConfig(this.config);
                        this.isDirty = true;
                        this.statusMessage = string.Empty;
                    }
                }

                break;

            case DataActionType.DeleteEncounters:
                if (this.isDirty)
                {
                    this.isDirty = false;
                    this.itemsToDeleteCount = ServiceContext.EncounterService.GetEncountersForDeletionCount();
                    this.totalItemsCount = EncounterService.GetEncountersCount();
                }

                LocGui.TextColored("KeepEncounters", ImGuiColors.DalamudViolet);
                ImGui.Indent(10);
                foreach (var property in this.config.EncounterDataActionOptions.GetType().GetProperties())
                {
                    var currentValue = (bool)(property.GetValue(this.config.EncounterDataActionOptions) ?? true);
                    if (ToadGui.Checkbox(property.Name, ref currentValue))
                    {
                        property.SetValue(this.config.EncounterDataActionOptions, currentValue);
                        ServiceContext.ConfigService.SaveConfig(this.config);
                        this.isDirty = true;
                        this.statusMessage = string.Empty;
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ImGui.Unindent(10);
        ImGui.Spacing();
        ImGui.Separator();
    }
}
