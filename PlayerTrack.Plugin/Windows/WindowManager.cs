using Dalamud.Interface.Windowing;
using PlayerTrack.Windows.Main;

namespace PlayerTrack.Windows;

/// <summary>
/// Wrapper for WindowSystem using implementations to simplify ImGui windowing.
/// </summary>
public class WindowManager
{
    private readonly WindowSystem WindowSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowManager" /> class.
    /// </summary>
    public WindowManager()
    {
        WindowSystem = new WindowSystem(Plugin.PluginInterface.InternalName);
        Plugin.PluginInterface.UiBuilder.Draw += Draw;
    }

    private bool IsEnabled { get; set; }

    /// <summary>
    /// Enable windows.
    /// </summary>
    public void Enable()
    {
        foreach (var window in WindowSystem.Windows)
        {
            var windowEx = (WindowEx)window;
            windowEx.Initialize();
        }

        IsEnabled = true;
    }

    /// <summary>
    /// Disable windows.
    /// </summary>
    public void Disable() => IsEnabled = false;

    /// <summary>
    /// Add a window to this <see cref="Dalamud.Interface.Windowing.WindowSystem" />.
    /// </summary>
    /// <param name="newWindows">The window(s) to add.</param>
    public void AddWindows(params Window[] newWindows)
    {
        foreach (var window in newWindows)
            WindowSystem.AddWindow(window);
    }

    /// <summary>
    /// Remove window to this <see cref="Dalamud.Interface.Windowing.WindowSystem" />.
    /// </summary>
    /// <param name="windows">The window(s) to remove.</param>
    public void RemoveWindows(params Window[] windows)
    {
        foreach (var window in windows)
            WindowSystem.RemoveWindow(window);
    }

    /// <summary>
    /// Clean up windows and events.
    /// </summary>
    public void Dispose()
    {
        Disable();
        Plugin.PluginInterface.UiBuilder.Draw -= Draw;
        WindowSystem.RemoveAllWindows();
    }

    private void Draw()
    {
        if (!IsEnabled)
            return;

        WindowSystem.Draw();
    }
}
