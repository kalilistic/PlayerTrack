using System;

namespace PlayerTrack
{
	public class PlayerListPresenter : PresenterBase
	{
		private readonly PlayerListView _playerListView;

		public PlayerListPresenter(IPlayerTrackPlugin plugin) : base(plugin)
		{
			_view = new PlayerListView {IsVisible = plugin.Configuration.ShowOverlay};
			_playerListView = (PlayerListView) _view;
			_playerListView.Configuration = _plugin.Configuration;
			_playerListView.WorldNames = new[] {string.Empty};
        }

        public void Initialize()
        {
            _playerListView.CategoryNames = _plugin.CategoryService.GetCategoryNames();
            _plugin.PlayerService.PlayersProcessed += PlayerServiceOnPlayersProcessed;
            _plugin.CategoryService.CategoriesUpdated += CategoryServiceOnCategoriesUpdated;
            _playerListView.ViewModeChanged += PlayerListViewOnViewModeChanged;
            _playerListView.NewSearch += PlayerListViewOnNewSearch;
            _playerListView.AddPlayer += PlayerListViewOnAddPlayer;
            _playerListView.OpenPlayer += PlayerListViewOnOpenPlayer;
            _playerListView.TargetPlayer += PlayerListViewOnTargetPlayer;
            _playerListView.HoverPlayer += PlayerListViewOnHoverPlayer;
            _playerListView.StopHoverPlayer += PlayerListViewOnStopHoverPlayer;
            _playerListView.NewCategoryFilter += PlayerListViewOnNewCategoryFilter;
            _playerListView.ConfigUpdated += PlayerListViewOnConfigUpdated;
            InitializeList();
            _playerListView.IsInitialized = true;
        }

		private void InitializeList()
		{
			var trackViewMode = TrackViewMode.GetViewModeByIndex(_plugin.Configuration.DefaultViewMode);
			if (trackViewMode == TrackViewMode.PlayersByCategory)
				PlayerListViewOnNewCategoryFilter(_plugin.Configuration.SelectedCategory);
			else if (trackViewMode == TrackViewMode.SearchForPlayers)
				PlayerListViewOnNewSearch(string.Empty);
			else if (trackViewMode == TrackViewMode.AddPlayer) _playerListView.WorldNames = _plugin.GetWorldNames();
			_plugin.TrackViewMode = trackViewMode;
			_playerListView.TrackViewMode = trackViewMode;
		}

		private void PlayerListViewOnConfigUpdated(object sender, bool e)
		{
			_plugin.SaveConfig();
		}

		private void CategoryServiceOnCategoriesUpdated(object sender, bool e)
		{
			_plugin.Configuration.SelectedCategory = 0;
			_plugin.SaveConfig();
			_playerListView.CategoryNames = _plugin.CategoryService.GetCategoryNames();
			PlayerListViewOnNewCategoryFilter(_plugin.Configuration.SelectedCategory);
		}

		private void PlayerListViewOnNewCategoryFilter(int categoryIndex)
		{
			try
			{
				var category = _plugin.CategoryService.GetCategoryByIndex(categoryIndex);
				_playerListView.Players = TrackViewPlayer.Map(_plugin.PlayerService.SearchByCategory(category.Id));
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to filter by category.");
			}
		}

		private void PlayerListViewOnStopHoverPlayer()
		{
			if (_plugin.Configuration.SetFocusTargetOnHover) _plugin.RevertFocusTarget();
		}

		private void PlayerListViewOnHoverPlayer(int actorId)
		{
			if (_plugin.Configuration.SetFocusTargetOnHover) _plugin.SetFocusTarget(actorId);
		}

		private void PlayerListViewOnTargetPlayer(int actorId)
		{
			if (_plugin.Configuration.SetCurrentTargetOnRightClick) _plugin.SetCurrentTarget(actorId);
		}

		private void PlayerListViewOnOpenPlayer(string playerKey)
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
			if (trackViewMode == TrackViewMode.PlayersByCategory)
				PlayerListViewOnNewCategoryFilter(_plugin.Configuration.SelectedCategory);
			if (trackViewMode == TrackViewMode.AddPlayer) _playerListView.WorldNames = _plugin.GetWorldNames();
			if (trackViewMode == TrackViewMode.SearchForPlayers ||
			    trackViewMode == TrackViewMode.AddPlayer)
				if (_playerListView.Players != null && _playerListView.Players.Count > 0)
					_playerListView.Players.Clear();
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
			_playerListView.OpenPlayer -= PlayerListViewOnOpenPlayer;
			_playerListView.TargetPlayer -= PlayerListViewOnTargetPlayer;
			_playerListView.HoverPlayer -= PlayerListViewOnHoverPlayer;
			_playerListView.StopHoverPlayer -= PlayerListViewOnStopHoverPlayer;
			_playerListView.NewCategoryFilter -= PlayerListViewOnNewCategoryFilter;
			_playerListView.ConfigUpdated -= PlayerListViewOnConfigUpdated;
		}
	}
}