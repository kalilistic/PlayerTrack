using System;
using Dalamud.DrunkenToad.Core;
using Dalamud.Loc.ImGui;
using Dalamud.Logging;
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
        PluginLog.LogVerbose("Entering GuiController.Start()");
        Initialize();
        configView = new ConfigView($"{Name}###Config", config) { IsOpen = config.IsConfigOpen };
        configView.WindowConfigChanged += WindowConfigChanged;
        configView.PlayerConfigChanged += presenter.ReloadPlayer;
        configView.PlayerConfigChanged += NameplateHandler.RefreshNameplates;
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
        PluginLog.LogVerbose("Entering GuiController.Dispose()");
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

            if (configView != null)
            {
                configView.WindowConfigChanged -= WindowConfigChanged;
                configView.PlayerConfigChanged -= presenter.ClearCache;
                configView.PlayerConfigChanged -= NameplateHandler.RefreshNameplates;
            }
        }
        catch (Exception)
        {
            PluginLog.LogWarning("Failed to dispose GuiController.");
        }
    }

    private static void StartCombinedWindow()
    {
        PluginLog.LogVerbose("Entering GuiController.StartCombinedWindow()");
        combinedView = presenter.Combined;
        combinedView.OpenConfig += OpenConfig;
        DalamudContext.WindowManager.AddWindows(combinedView);
    }

    private static void CloseCombinedWindow()
    {
        PluginLog.LogVerbose("Entering GuiController.CloseCombinedWindow()");
        if (combinedView != null)
        {
            combinedView.OpenConfig -= OpenConfig;
            DalamudContext.WindowManager.RemoveWindows(combinedView);
        }
    }

    private static void StartSeparateWindows()
    {
        PluginLog.LogVerbose("Entering GuiController.StartSeparateWindows()");
        playerListView = presenter.PlayerList;
        playerListView.OpenConfig += OpenConfig;
        panelView = presenter.PanelView;
        DalamudContext.WindowManager.AddWindows(playerListView, panelView);
    }

    private static void CloseSeparateWindows()
    {
        PluginLog.LogVerbose("Entering GuiController.CloseSeparateWindows()");
        if (playerListView != null && panelView != null)
        {
            playerListView.OpenConfig -= OpenConfig;
            DalamudContext.WindowManager.RemoveWindows(playerListView, panelView);
        }
    }

    private static void OpenConfig() => configView?.Toggle();

    private static void WindowConfigChanged()
    {
        PluginLog.LogVerbose("Entering GuiController.WindowConfigChanged()");
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
        PluginLog.LogVerbose("Entering GuiController.Initialize()");
        DalamudContext.LocManager.LoadLanguagesFromAssembly($"{Name}.Resource.Loc");
        LocGui.Initialize(ServiceContext.Localization);
        ServiceContext.ConfigService.GetConfig().PanelType = PanelType.None;
        config = ServiceContext.ConfigService.GetConfig();
        presenter = new MainPresenter { OnConfigMenuOptionSelected = configMenuOption => configView?.Open(configMenuOption), };
        ServiceContext.PlayerDataService.CacheUpdated += presenter.ClearCache;
        ServiceContext.PlayerProcessService.PlayerSelected += OnPlayerSelected;
        CommandHandler.ConfigWindowToggled += OnConfigWindowToggled;
        CommandHandler.PlayerWindowToggled += OnPlayerWindowToggled;
    }

    private static void OnPlayerSelected(Player player)
    {
        PluginLog.LogVerbose($"Entering GuiController.OnPlayerSelected(): {player.Id}");
        presenter.SelectPlayer(player);
        presenter.ShowPanel(PanelType.Player);
    }

    private static void OnConfigWindowToggled()
    {
        PluginLog.LogVerbose("Entering GuiController.OnConfigWindowToggled()");
        configView?.Toggle();
    }

    private static void OnPlayerWindowToggled()
    {
        PluginLog.LogVerbose("Entering GuiController.OnPlayerWindowToggled()");
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
