using System;

namespace PlayerTrack
{
	public class PluginUIBase : IDisposable
	{
		public OverlayWindow OverlayWindow;
		public IPlayerTrackPlugin PlayerTrackPlugin;
		public SettingsWindow SettingsWindow;

		public PluginUIBase(IPlayerTrackPlugin playerTrackPlugin)
		{
			PlayerTrackPlugin = playerTrackPlugin;
			BuildWindows();
			SetWindowVisibility();
			AddEventHandlers();
		}

		public void Dispose()
		{
			SettingsWindow.OverlayVisibilityUpdated -= UpdateOverlayVisibility;
		}

		private void BuildWindows()
		{
			OverlayWindow = new OverlayWindow(PlayerTrackPlugin);
			SettingsWindow = new SettingsWindow(PlayerTrackPlugin);
		}

		private void SetWindowVisibility()
		{
			OverlayWindow.IsVisible = PlayerTrackPlugin.Configuration.ShowOverlay;
			SettingsWindow.IsVisible = false;
		}

		private void AddEventHandlers()
		{
			SettingsWindow.OverlayVisibilityUpdated += UpdateOverlayVisibility;
		}

		private void UpdateOverlayVisibility(object sender, bool e)
		{
			OverlayWindow.IsVisible = e;
		}

		public void Draw()
		{
			OverlayWindow.DrawWindow();
			SettingsWindow.DrawWindow();
		}
	}
}