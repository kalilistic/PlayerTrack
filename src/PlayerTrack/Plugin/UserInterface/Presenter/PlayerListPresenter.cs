namespace PlayerTrack
{
	public class PlayerListPresenter : PresenterBase
	{
		private readonly PlayerListView _playerListView;

		public PlayerListPresenter(IPlayerTrackPlugin plugin) : base(plugin)
		{
			_view = new PlayerListView{IsVisible = plugin.Configuration.ShowOverlay};
			_playerListView = (PlayerListView) _view;
			_playerListView.WorldNames = new[] {string.Empty};
			_plugin.PlayerService.PlayersProcessed += PlayerServiceOnPlayersProcessed;
			_playerListView.ViewModeChanged += PlayerListViewOnViewModeChanged;
			_playerListView.NewSearch += PlayerListViewOnNewSearch;
			_playerListView.AddPlayer += PlayerListViewOnAddPlayer;
			_playerListView.SelectPlayer += PlayerListViewOnSelectPlayer;
		}

		private void PlayerListViewOnSelectPlayer(string playerKey)
		{
			_plugin.SelectPlayer(playerKey);
		}

		private void PlayerListViewOnAddPlayer(string playerName, string worldName)
		{
			if (!_plugin.IsValidCharacterName(playerName))
			{
				_playerListView.CurrentModal = PlayerListView.PlayerListModal.InvalidCharacterName;
				return;
			}

			var isAdded = _plugin.PlayerService.AddPlayer(playerName, worldName);
			_playerListView.CurrentModal = isAdded
				? PlayerListView.PlayerListModal.AddCharacterSuccess
				: PlayerListView.PlayerListModal.DuplicateCharacter;
		}

		private void PlayerListViewOnNewSearch(string input)
		{
			_playerListView.Players = TrackViewPlayer.Map(_plugin.PlayerService.SearchByName(input));
		}

		private void PlayerListViewOnViewModeChanged(TrackViewMode trackViewMode)
		{
			if (trackViewMode == TrackViewMode.AddPlayer) _playerListView.WorldNames = _plugin.GetWorldNames();
			if (trackViewMode == TrackViewMode.SearchForPlayers ||
			    trackViewMode == TrackViewMode.AddPlayer) _playerListView.Players.Clear();
			_plugin.TrackViewMode = trackViewMode;
		}

		public void PlayerServiceOnPlayersProcessed()
		{
			if (_plugin.TrackViewMode == TrackViewMode.CurrentPlayers)
				_playerListView.Players = TrackViewPlayer.Map(_plugin.PlayerService.CurrentPlayers);
			else if (_plugin.TrackViewMode == TrackViewMode.AllPlayers)
				_playerListView.Players = TrackViewPlayer.Map(_plugin.PlayerService.AllPlayers);
			else if (_plugin.TrackViewMode == TrackViewMode.RecentPlayers)
				_playerListView.Players = TrackViewPlayer.Map(_plugin.PlayerService.RecentPlayers);
		}

		public void Dispose()
		{
			_plugin.PlayerService.PlayersProcessed -= PlayerServiceOnPlayersProcessed;
			_playerListView.ViewModeChanged -= PlayerListViewOnViewModeChanged;
			_playerListView.NewSearch -= PlayerListViewOnNewSearch;
			_playerListView.AddPlayer -= PlayerListViewOnAddPlayer;
		}
	}
}