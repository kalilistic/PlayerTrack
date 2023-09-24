using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Config.Components;
using PlayerTrack.UserInterface.Views;

namespace PlayerTrack.UserInterface.Config.Views;

public class ConfigView : PlayerTrackView, IDisposable
{
    private readonly WindowComponent windowComponent = new();
    private readonly ContextMenuComponent contextMenuComponent = new();
    private readonly IconComponent iconComponent = new();
    private readonly TagComponent tagComponent = new();
    private readonly PlayerDefaultsComponent playerDefaultsComponent = new();
    private readonly CategoryComponent categoryComponent = new();
    private readonly LocationComponent locationComponent = new();
    private readonly IntegrationComponent integrationComponent = new();
    private readonly BackupComponent backupComponent = new();
    private readonly DataComponent dataComponent = new();

    public ConfigView(string name, PluginConfig config, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags)
    {
        this.Size = new Vector2(730f, 420f);
        this.SizeCondition = ImGuiCond.Appearing;
        this.backupComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.categoryComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.contextMenuComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.contextMenuComponent.UpdateContextMenu += () => this.ContextMenuUpdated?.Invoke();
        this.iconComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.integrationComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.playerDefaultsComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.tagComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.locationComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.windowComponent.WindowConfigComponent_WindowConfigChanged += () => this.WindowConfigChanged?.Invoke();
    }

    public delegate void WindowConfigChangedDelegate();

    public delegate void PlayerConfigChangedDelegate();

    public delegate void ContextMenuUpdatedDelegate();

    public event WindowConfigChangedDelegate? WindowConfigChanged;

    public event PlayerConfigChangedDelegate? PlayerConfigChanged;

    public event ContextMenuUpdatedDelegate? ContextMenuUpdated;

    public ConfigMenuOption SelectedMenuOption { get; set; }

    public string[] ConfigMenuOptions { get; set; } = null!;

    public override void Initialize()
    {
        this.ConfigMenuOptions = Enum.GetNames(typeof(ConfigMenuOption));
        this.SelectedMenuOption = this.config.SelectedConfigOption;
    }

    public override void OnOpen()
    {
        this.config.IsConfigOpen = true;
        ServiceContext.ConfigService.SaveConfig(this.config);
    }

    public override void OnClose()
    {
        this.config.IsConfigOpen = false;
        ServiceContext.ConfigService.SaveConfig(this.config);
    }

    public override void Draw()
    {
        ImGui.BeginChild("###Config_Navigation", ImGuiHelpers.ScaledVector2(120, 0), true);
        for (var i = 0; i < this.ConfigMenuOptions.Length; i++)
        {
            if (ImGui.Selectable(ServiceContext.Localization.GetString(this.ConfigMenuOptions[i]), (int)this.SelectedMenuOption == i))
            {
                this.SelectedMenuOption = (ConfigMenuOption)i;
                this.config.SelectedConfigOption = this.SelectedMenuOption;
            }
        }

        ImGui.EndChild();
        ImGui.SameLine();
        ImGui.BeginGroup();
        switch (this.SelectedMenuOption)
        {
            case ConfigMenuOption.Window:
                this.windowComponent.Draw();
                break;
            case ConfigMenuOption.ContextMenu:
                this.contextMenuComponent.Draw();
                break;
            case ConfigMenuOption.Icons:
                this.iconComponent.Draw();
                break;
            case ConfigMenuOption.Tags:
                this.tagComponent.Draw();
                break;
            case ConfigMenuOption.PlayerDefaults:
                this.playerDefaultsComponent.Draw();
                break;
            case ConfigMenuOption.Categories:
                this.categoryComponent.Draw();
                break;
            case ConfigMenuOption.Locations:
                this.locationComponent.Draw();
                break;
            case ConfigMenuOption.Integrations:
                this.integrationComponent.Draw();
                break;
            case ConfigMenuOption.Backups:
                this.backupComponent.Draw();
                break;
            case ConfigMenuOption.Data:
                this.dataComponent.Draw();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ImGui.EndGroup();
    }

    public void Open(ConfigMenuOption configMenuOption)
    {
        this.SelectedMenuOption = configMenuOption;
        ServiceContext.ConfigService.SaveConfig(this.config);
        this.IsOpen = true;
    }

    public void Dispose()
    {
        this.categoryComponent.Dispose();
        this.playerDefaultsComponent.Dispose();
        this.tagComponent.Dispose();
        this.locationComponent.Dispose();
        this.integrationComponent.Dispose();
        this.backupComponent.Dispose();
        GC.SuppressFinalize(this);
    }
}
