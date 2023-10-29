using System;
using Dalamud.DrunkenToad.Core;
using Dalamud.Loc.ImGui;

using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Config.Views;
using PlayerTrack.UserInterface.Main.Presenters;
using PlayerTrack.UserInterface.Main.Views;

namespace PlayerTrack.Plugin;

public static class GuiController
{
    private const string Name = "PlayerTrack";
    private static ConfigView? configView;
    private static PluginConfig config = null!;

    private static MainPresenter presenter = null!;
    private static Combined? combinedView;
    private static PlayerList? playerListView;
    private static PanelView? panelView;
    private static bool isCombinedView = true;

    public static void Start()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.Start()");
        Initialize();
        configView = new ConfigView($"{Name}###Config", config) { IsOpen = config is { IsConfigOpen: true, PreserveConfigState: true } };
        configView.WindowConfigChanged += WindowConfigChanged;
        configView.PlayerConfigChanged += presenter.ReloadPlayer;
        configView.PlayerConfigChanged += NameplateHandler.RefreshNameplates;
        configView.ContextMenuUpdated += ContextMenuHandler.Restart;
        DalamudContext.WindowManager.AddWindows(configView);
        if (config.IsWindowCombined)
        {
            StartCombinedWindow();
            isCombinedView = true;
        }
        else
        {
            StartSeparateWindows();
            isCombinedView = false;
        }

        DalamudContext.PluginInterface.UiBuilder.OpenMainUi += OnPlayerWindowToggled;
        DalamudContext.PluginInterface.UiBuilder.OpenConfigUi += OnConfigWindowToggled;
        DalamudContext.WindowManager.Enable();
    }

    public static void Dispose()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.Dispose()");
        try
        {
            DalamudContext.PluginInterface.UiBuilder.OpenMainUi -= OnPlayerWindowToggled;
            DalamudContext.PluginInterface.UiBuilder.OpenConfigUi -= OnConfigWindowToggled;
            CommandHandler.ConfigWindowToggled -= OnConfigWindowToggled;
            CommandHandler.PlayerWindowToggled -= OnPlayerWindowToggled;
            ServiceContext.PlayerProcessService.PlayerSelected -= OnPlayerSelected;
            if (combinedView != null)
            {
                combinedView.OpenConfig -= OpenConfig;
            }

            if (configView == null) return;
            configView.WindowConfigChanged -= WindowConfigChanged;
            configView.PlayerConfigChanged -= presenter.ClearCache;
            configView.PlayerConfigChanged -= NameplateHandler.RefreshNameplates;
            configView.ContextMenuUpdated -= ContextMenuHandler.Restart;
        }
        catch (Exception)
        {
            DalamudContext.PluginLog.Warning("Failed to dispose GuiController.");
        }
    }

    private static void StartCombinedWindow()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.StartCombinedWindow()");
        combinedView = presenter.Combined;
        combinedView.OpenConfig += OpenConfig;
        DalamudContext.WindowManager.AddWindows(combinedView);
    }

    private static void CloseCombinedWindow()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.CloseCombinedWindow()");
        if (combinedView == null) return;
        combinedView.OpenConfig -= OpenConfig;
        DalamudContext.WindowManager.RemoveWindows(combinedView);
    }

    private static void StartSeparateWindows()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.StartSeparateWindows()");
        playerListView = presenter.PlayerList;
        playerListView.OpenConfig += OpenConfig;
        panelView = presenter.PanelView;
        DalamudContext.WindowManager.AddWindows(playerListView, panelView);
    }

    private static void CloseSeparateWindows()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.CloseSeparateWindows()");
        if (playerListView == null || panelView == null) return;
        playerListView.OpenConfig -= OpenConfig;
        DalamudContext.WindowManager.RemoveWindows(playerListView, panelView);
    }

    private static void OpenConfig() => configView?.Toggle();

    private static void WindowConfigChanged()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.WindowConfigChanged()");
        if (isCombinedView && !config.IsWindowCombined)
        {
            CloseCombinedWindow();
            StartSeparateWindows();
            isCombinedView = false;
            panelView!.IsOpen = true;
            playerListView!.IsOpen = true;
        }
        else if (!isCombinedView && config.IsWindowCombined)
        {
            CloseSeparateWindows();
            StartCombinedWindow();
            isCombinedView = true;
            combinedView!.IsOpen = true;
        }

        combinedView?.RefreshWindowConfig();
        playerListView?.RefreshWindowConfig();
        panelView?.ResetWindow();
    }

    private static void Initialize()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.Initialize()");
        DalamudContext.LocManager.LoadLanguagesFromAssembly($"{Name}.Resource.Loc");
        LocGui.Initialize(ServiceContext.Localization);
        ServiceContext.ConfigService.GetConfig().PanelType = PanelType.None;
        config = ServiceContext.ConfigService.GetConfig();
        presenter = new MainPresenter { OnConfigMenuOptionSelected = configMenuOption => configView?.Open(configMenuOption), };
        ServiceContext.PlayerCacheService.CacheUpdated += presenter.ClearCache;
        ServiceContext.PlayerProcessService.PlayerSelected += OnPlayerSelected;
        CommandHandler.ConfigWindowToggled += OnConfigWindowToggled;
        CommandHandler.PlayerWindowToggled += OnPlayerWindowToggled;
    }

    private static void OnPlayerSelected(Player player)
    {
        DalamudContext.PluginLog.Verbose($"Entering GuiController.OnPlayerSelected(): {player.Id}");
        presenter.SelectPlayer(player);
        presenter.ShowPanel(PanelType.Player);
    }

    private static void OnConfigWindowToggled()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.OnConfigWindowToggled()");
        configView?.Toggle();
    }

    private static void OnPlayerWindowToggled()
    {
        DalamudContext.PluginLog.Verbose("Entering GuiController.OnPlayerWindowToggled()");
        if (isCombinedView)
        {
            combinedView?.Toggle();
        }
        else
        {
            playerListView?.Toggle();
            panelView?.Toggle();
        }
    }
}
