// ReSharper disable DelegateSubtraction
// ReSharper disable ReturnTypeCanBeEnumerable.Local
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable PossibleMultipleEnumeration

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CheapLoc;
using Dalamud.Game.ClientState.Actors;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace PlayerTrack
{
	public sealed class PlayerTrackPlugin : PluginBase, IPlayerTrackPlugin
	{
		private uint _currentTerritoryTypeId;
        public bool IsInitializing { get; set; } = true;
		private bool _isProcessing = true;
        private Timer _onSaveTimer;
		private Timer _onUpdateTimer;
		private PlayerDetailPresenter _playerDetailPresenter;
		private PlayerListPresenter _playerListPresenter;
		private DalamudPluginInterface _pluginInterface;
		private SettingsPresenter _settingsPresenter;

		public PlayerTrackPlugin(string pluginName, DalamudPluginInterface pluginInterface) : base(pluginName,
			pluginInterface)
		{
			Task.Run(() =>
			{
				_pluginInterface = pluginInterface;
				DataManager = new DataManager(this);
				JsonSerializerSettings = SerializerUtil.CamelCaseIncludeJsonSerializer();
				ResourceManager.UpdateResources();
				FontAwesomeUtil.Init();
				InitContent();
				LoadConfig();
				UpgradeBackup();
                LoadUI();
				LoadServices();
				InitializePresenters();
                SetupCommands();
				HandleFreshInstall();
				StartTimers();
				_currentTerritoryTypeId = GetTerritoryType();
				LocationLastChanged = DateUtil.CurrentTime();
                IsInitializing = false;
                _isProcessing = false;
            });
		}

        private void InitializePresenters()
        {
            _playerListPresenter.Initialize();
            _settingsPresenter.Initialize();
		}

		public long LocationLastChanged { get; set; }

		public DataManager DataManager { get; set; }
		public bool InContent { get; set; }
		public JsonSerializerSettings JsonSerializerSettings { get; set; }

		public LodestoneService LodestoneService { get; set; }
		public CategoryService CategoryService { get; set; }
		public TrackViewMode TrackViewMode { get; set; } = TrackViewMode.CurrentPlayers;

		public void ToggleOverlay(string command, string args)
		{
			_playerListPresenter.ToggleView();
			Configuration.ShowOverlay = !Configuration.ShowOverlay;
			SaveConfig();
		}

		public string[] GetIconNames()
		{
			var namesList = new List<string> {Loc.Localize("Default", "Default")};
			namesList.AddRange(Configuration.EnabledIcons.ToList()
				.Select(icon => icon.ToString()));
			return namesList.ToArray();
		}

		public int[] GetIconCodes()
		{
			var codesList = new List<int> {CategoryService.GetDefaultIcon()};
			codesList.AddRange(Configuration.EnabledIcons.ToList().Select(icon => (int) icon));
			return codesList.ToArray();
		}


		public void SetDefaultIcons()
		{
			Configuration.EnabledIcons = new List<FontAwesomeIcon>
			{
				FontAwesomeIcon.GrinBeam,
				FontAwesomeIcon.Grin,
				FontAwesomeIcon.Meh,
				FontAwesomeIcon.Frown,
				FontAwesomeIcon.Angry,
				FontAwesomeIcon.Flushed,
				FontAwesomeIcon.Surprise,
				FontAwesomeIcon.Tired
			};
		}

		public PlayerService PlayerService { get; set; }
		public PlayerTrackConfig Configuration { get; set; }

		public void PrintHelpMessage()
		{
			PrintMessage(Loc.Localize("HelpMessage1",
				"PlayerTrack helps you keep a record of who you meet and the content you played together. " +
				"By default, this is instanced content only - but you can expand or restrict this in settings. " +
				"You can see all the details on a player by clicking on their name in the overlay. " +
				"Here you can also record notes and set a personalized icon/color."));
			Thread.Sleep(500);
			PrintMessage(Loc.Localize("HelpMessage2",
				"PlayerTrack uses Lodestone to keep the data updated (e.g. world transfers). " +
				"If this happens, you'll see an indicator next to their home world and " +
				"can mouse-over to see their previous residence."));
			Thread.Sleep(500);
			PrintMessage(Loc.Localize("HelpMessage3",
				"If you need help, reach out on discord or open an issue on GitHub. If you want to " +
				"help add translations, you can submit updates on Crowdin. Links to both GitHub and Crowdin are available in settings."));
		}

		public void SaveConfig()
		{
			SaveConfig(Configuration);
		}

		public string[] GetWorldNames()
		{
			try
			{
				return GetWorldNames(GetDataCenterId()).ToArray();
			}
			catch (Exception ex)
			{
				LogError(ex, "Failed to load world names");
				return new string[] { };
			}
		}

		public void SelectPlayer(string playerKey)
		{
			_playerDetailPresenter.SelectPlayer(playerKey);
		}

		public void ReloadList()
		{
			_playerListPresenter.PlayerServiceOnPlayersProcessed();
		}

		public new void Dispose()
		{
			try
			{
				var delayCount = 0;
				while (_isProcessing)
					if (delayCount == 3)
					{
						_isProcessing = false;
					}
					else
					{
						Thread.Sleep(1000);
						delayCount++;
					}

				_isProcessing = true;
				RemoveCommands();
                try
                {
                    StopTimers();
                }
                catch
                {
                    // ignored
                }
                try
                {
                    PlayerService.SaveData();
                    CategoryService.Dispose();
                    LodestoneService.Dispose();
                    _settingsPresenter.Dispose();
                    _playerListPresenter.Dispose();
                    _playerDetailPresenter.Dispose();
				}
                catch
                {
                    // ignored
                }
				base.Dispose();
				_pluginInterface.UiBuilder.OnOpenConfigUi -= (sender, args) => DrawConfigUI();
				_pluginInterface.UiBuilder.OnBuildUi -= DrawUI;
				_pluginInterface.Dispose();
				_isProcessing = false;
			}
			catch (Exception ex)
			{
				LogError(ex, "Failed to dispose properly.");
			}
		}

		public new void SetupCommands()
		{
			_pluginInterface.CommandManager.AddHandler("/ptrack", new CommandInfo(ToggleOverlay)
			{
				HelpMessage = "Show PlayerTrack plugin.",
				ShowInHelp = true
			});
			_pluginInterface.CommandManager.AddHandler("/ptrackconfig", new CommandInfo(ToggleConfig)
			{
				HelpMessage = "Show PlayerTrack config.",
				ShowInHelp = true
			});
		}

		public new void RemoveCommands()
		{
			_pluginInterface.CommandManager.RemoveHandler("/ptrack");
			_pluginInterface.CommandManager.RemoveHandler("/ptrackconfig");
		}

		public void ToggleConfig(string command, string args)
		{
			LogInfo("Running command {0} with args {1}", command, args);
			_settingsPresenter.ToggleView();
		}

		private void StartTimers()
		{
			_onUpdateTimer = new Timer {Interval = Configuration.UpdateFrequency, Enabled = true};
			_onUpdateTimer.Elapsed += OnUpdate;
			_onSaveTimer = new Timer {Interval = Configuration.SaveFrequency, Enabled = true};
			_onSaveTimer.Elapsed += OnPlayersSave;
		}

		private void StopTimers()
		{
			_onUpdateTimer.Elapsed -= OnUpdate;
			_onSaveTimer.Elapsed -= OnPlayersSave;
			_onUpdateTimer.Stop();
			_onSaveTimer.Stop();
		}

		private void OnPlayersSave(object sender, ElapsedEventArgs e)
		{
			try
			{
				if (_isProcessing) return;
				_isProcessing = true;
				PlayerService.SaveData();
				_isProcessing = false;
			}
			catch (Exception ex)
			{
				LogError(ex, "Failed to save players - will try again shortly.");
				_isProcessing = false;
			}
		}

        private void OnUpdate(object sender, ElapsedEventArgs e)
		{
			try
			{
				// processing check
                if (_isProcessing)
                {
                    return;
                }
				_isProcessing = true;


				// enabled check
                if (!Configuration.Enabled)
                {
                    _isProcessing = false;
                    return;
                }

				// combat check
				if (Configuration.RestrictInCombat && InCombat())
				{
                    _isProcessing = false;
					return;
				}

				// territory check
				var territoryTypeId = GetTerritoryType();
				if (territoryTypeId == 0)
				{
                    PlayerService.ProcessExistingOnly();
					_isProcessing = false;
					return;
				}

				// update territory if needed
				if (territoryTypeId != _currentTerritoryTypeId)
				{
                    _currentTerritoryTypeId = territoryTypeId;
					LocationLastChanged = DateUtil.CurrentTime();
				}

				// content check
				var contentId = GetContentId(territoryTypeId);
				if (contentId == 0)
				{
                    InContent = false;
					if (Configuration.RestrictToContent)
					{
                        PlayerService.ProcessExistingOnly();
						_isProcessing = false;
						return;
					}
				}
				else
				{
					InContent = true;
				}

				// high end duty check
				if (Configuration.RestrictToHighEndDuty && !IsHighEndDuty(contentId))
				{
                    PlayerService.ProcessExistingOnly();
					_isProcessing = false;
					return;
				}

				// custom content filter check
				if (Configuration.RestrictToCustom && !Configuration.PermittedContent.Contains(contentId))
				{
                    PlayerService.ProcessExistingOnly();
					_isProcessing = false;
					return;
				}

				// player check
				var players = GetPlayerCharacters();

				// build new roster of track players
				var placeName = GetPlaceName(territoryTypeId);
				var contentName = GetContentName(contentId);
				var newPlayers = BuildNewPlayerList(territoryTypeId, placeName, contentName, players);

				// pass to player service for processing against existing
				PlayerService.ProcessPlayers(newPlayers);

				// finish processing
				_isProcessing = false;
			}
			catch
			{
				_isProcessing = false;
			}
		}

		private List<TrackPlayer> BuildNewPlayerList(uint territoryType, string placeName, string contentName,
			IEnumerable<PlayerCharacter> players)
		{
			try
			{
				var currentDateTime = DateUtil.CurrentTime();
				return players.ToList().Select(player => new TrackPlayer
				{
					ActorId = player.ActorId,
					Names = new List<string> {player.Name},
					HomeWorlds = new List<TrackWorld>
					{
						new TrackWorld
						{
							Id = player.HomeWorld.GameData.RowId,
							Name = player.HomeWorld.GameData.Name
						}
					},
					FreeCompany = player.CompanyTag,
					Gender = player.Customize[(int) CustomizeIndex.Gender],
					Race = player.Customize[(int) CustomizeIndex.Race],
					Tribe = player.Customize[(int) CustomizeIndex.Tribe],
					Height = player.Customize[(int) CustomizeIndex.Height],
					Encounters = new List<TrackEncounter>
					{
						new TrackEncounter
						{
							Created = currentDateTime,
							Updated = currentDateTime,
							Location = new TrackLocation
							{
								TerritoryType = territoryType,
								PlaceName = placeName,
								ContentName = contentName
							},
							Job = new TrackJob
							{
								Id = player.ClassJob.GameData.RowId,
								Lvl = player.Level,
								Code = player.ClassJob.GameData.Abbreviation
							}
						}
					}
				}).ToList();
			}
			catch
			{
				return null;
			}
		}

		public void LoadServices()
		{
            LodestoneService = new LodestoneService(this);
            CategoryService = new CategoryService(this);
            PlayerService = new PlayerService(this);
        }

		public void UpgradeBackup()
		{
			if (Configuration.FreshInstall) return;
			var pluginVersion = PluginVersionNumber();
			if (PluginVersionNumber() <= Configuration.PluginVersion) return;
			DataManager.CreateBackup("upgrade/v" + Configuration.PluginVersion + "_");
			Configuration.PluginVersion = pluginVersion;
			SaveConfig();
		}

		public void LoadUI()
		{
			Localization.SetLanguage(Configuration.PluginLanguage);
			_playerListPresenter = new PlayerListPresenter(this);
			_playerDetailPresenter = new PlayerDetailPresenter(this);
			_settingsPresenter = new SettingsPresenter(this);
			_pluginInterface.UiBuilder.OnBuildUi += DrawUI;
			_pluginInterface.UiBuilder.OnOpenConfigUi += (sender, args) => DrawConfigUI();
			if (Configuration.ShowOverlay) _playerListPresenter.ShowView();
		}

		private void HandleFreshInstall()
		{
			if (!Configuration.FreshInstall) return;
			PrintMessage(Loc.Localize("InstallThankYou", "Thank you for installing PlayerTrack!"));
			PrintHelpMessage();
			Configuration.FreshInstall = false;
			Configuration.PluginVersion = PluginVersionNumber();
			SetDefaultIcons();
			SaveConfig();
			_settingsPresenter.ShowView();
			_playerListPresenter.ShowView();
		}

		private void DrawUI()
		{
			_playerListPresenter.DrawView();
			_playerDetailPresenter.DrawView();
			_settingsPresenter.DrawView();
		}

		private void DrawConfigUI()
		{
			_settingsPresenter.ToggleView();
		}

		public new void LoadConfig()
		{
			try
			{
				Configuration = base.LoadConfig() as PluginConfig ?? new PluginConfig();
				EnforceSettings();
				SaveConfig();
			}
			catch
			{
				LogInfo("Couldn't load config so creating one.");
				Configuration = new PluginConfig();
				SaveConfig();
			}
		}

		private void EnforceSettings()
		{
			Configuration.UpdateFrequency = 1000;
			Configuration.SaveFrequency = 15000;
			Configuration.LodestoneRequestDelay = 30000;
		}
	}
}