using System;

namespace PlayerTrack
{
	public class PlayerDetailPresenter : PresenterBase
	{
		private readonly PlayerDetailView _playerDetailView;

		public PlayerDetailPresenter(IPlayerTrackPlugin plugin) : base(plugin)
		{
			_view = new PlayerDetailView();
			_playerDetailView = (PlayerDetailView) _view;
			_playerDetailView.Configuration = _plugin.Configuration;
			_playerDetailView.ResetPlayer += PlayerDetailViewOnResetPlayer;
			_playerDetailView.DeletePlayer += PlayerDetailViewOnDeletePlayer;
			_playerDetailView.SavePlayer += PlayerDetailViewOnSavePlayer;
		}

		private void PlayerDetailViewOnSavePlayer(TrackViewPlayerDetail player)
		{
			var updatedPlayer = TrackViewPlayerDetail.Map(player, _plugin);
			_plugin.PlayerService.UpdatePlayer(updatedPlayer);
			_plugin.ReloadList();
		}

		private void PlayerDetailViewOnDeletePlayer(string playerKey)
		{
			var deleteSuccessful = _plugin.PlayerService.DeletePlayer(playerKey);
			if (!deleteSuccessful) return;
			_plugin.ReloadList();
		}

		private void PlayerDetailViewOnResetPlayer(string playerKey)
		{
			var resetSuccessful = _plugin.PlayerService.ResetPlayer(playerKey);
			if (resetSuccessful) SelectPlayer(playerKey);
		}

		public void SelectPlayer(string playerKey)
		{
			var player = _plugin.PlayerService.GetPlayer(playerKey);
			if (player == null) return;
			try
			{
				_playerDetailView.Player = TrackViewPlayerDetail.Map(player, _plugin);
				_playerDetailView.SelectedCategory = player.CategoryIndex;
				_playerDetailView.SelectedIcon = player.IconIndex;
				ShowView();
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to load player " + playerKey);
			}
		}

		public void Dispose()
		{
			_playerDetailView.ResetPlayer -= PlayerDetailViewOnResetPlayer;
			_playerDetailView.DeletePlayer -= PlayerDetailViewOnDeletePlayer;
			_playerDetailView.SavePlayer -= PlayerDetailViewOnSavePlayer;
		}
	}
}