using System;
using System.Numerics;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Config.Components;
using PlayerTrack.UserInterface.Views;

namespace PlayerTrack.UserInterface.Config.Views;

using Dalamud.DrunkenToad.Core;
using Dalamud.Interface.Utility;

public class ConfigView : PlayerTrackView, IDisposable
{
    private readonly WindowComponent windowComponent = new();
    private readonly ContextMenuComponent contextMenuComponent = new();
    private readonly IconComponent iconComponent = new();
    private readonly TagComponent tagComponent = new();
    private readonly PlayerDefaultsComponent playerDefaultsComponent = new();
    private readonly CategoryComponent categoryComponent = new();
    private readonly LocationComponent locationComponent = new();
    private readonly SocialListComponent socialListComponent = new();
    private readonly IntegrationComponent integrationComponent = new();
    private readonly BackupComponent backupComponent = new();
    private readonly DataComponent dataComponent = new();
    private readonly ContributeComponent contributeComponent = new();
    private readonly HelpComponent helpComponent = new();
    private float navMaxWidth;
    private bool isLanguageChanged = true;

    public ConfigView(string name, PluginConfig config, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags)
    {
        this.Size = new Vector2(820f, 450f);
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
        this.socialListComponent.OnPlayerConfigChanged += () => this.PlayerConfigChanged?.Invoke();
        this.windowComponent.WindowConfigComponent_WindowConfigChanged += () => this.WindowConfigChanged?.Invoke();
        DalamudContext.PluginInterface.LanguageChanged += _ => this.isLanguageChanged = true;
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
        this.dataComponent.Initialize();
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

    public void CalcSize()
    {
        float maxWidth = 0;
        foreach (var key in this.ConfigMenuOptions)
        {
            var translatedString = ServiceContext.Localization.GetString(key);
            var stringSize = ImGui.CalcTextSize(translatedString);
            if (stringSize.X > maxWidth)
            {
                maxWidth = stringSize.X;
            }
        }

        this.navMaxWidth = (maxWidth + 20) * ImGuiHelpers.GlobalScale;
        this.backupComponent.CalcSize();
    }

    public override void Draw()
    {
        if (isLanguageChanged)
        {
            this.CalcSize();
            this.isLanguageChanged = false;
        }

        ImGui.BeginChild("###Config_Navigation", ImGuiHelpers.ScaledVector2(this.navMaxWidth, 0), true);
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
            case ConfigMenuOption.SocialLists:
                this.socialListComponent.Draw();
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
            case ConfigMenuOption.Contribute:
                this.contributeComponent.Draw();
                break;
            case ConfigMenuOption.Help:
                this.helpComponent.Draw();
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
        this.socialListComponent.Dispose();
        this.integrationComponent.Dispose();
        this.backupComponent.Dispose();
        GC.SuppressFinalize(this);
    }
}
