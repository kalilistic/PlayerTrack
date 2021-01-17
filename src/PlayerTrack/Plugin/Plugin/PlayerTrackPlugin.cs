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
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Timer = System.Timers.Timer;

namespace PlayerTrack
{
	public sealed class PlayerTrackPlugin : PluginBase, IPlayerTrackPlugin
	{
		private DataManager _dataManager;
		private bool _inContent;
		private bool _isProcessing = true;
		private Timer _onSaveTimer;
		private Timer _onSettingsTimer;
		private Timer _onUpdateTimer;
		private DalamudPluginInterface _pluginInterface;
		private PluginUI _pluginUI;

		public PlayerTrackPlugin(string pluginName, DalamudPluginInterface pluginInterface) : base(pluginName,
			pluginInterface)
		{
			Task.Run(() =>
			{
				_pluginInterface = pluginInterface;
				_dataManager = new DataManager(this);
				ResourceManager.UpdateResources();
				FontAwesomeUtil.Init();
				InitContent();
				LoadConfig();
				LoadServices();
				SetupCommands();
				LoadUI();
				BackupOnStart();
				HandleFreshInstall();
				StartTimers();
				_isProcessing = false;
			});
		}

		private LodestoneService LodestoneService { get; set; }
		public CategoryService CategoryService { get; set; }

		public DataManager GetDataManager()
		{
			return _dataManager;
		}

		public LodestoneService GetLodestoneService()
		{
			return LodestoneService;
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

		public RosterService RosterService { get; set; }
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
				"If this happens, you'll see an asterisk next to their home world and " +
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

		public void RestartTimers()
		{
			StopTimers();
			StartTimers();
		}

		public CategoryService GetCategoryService()
		{
			return CategoryService;
		}

		public bool InContent()
		{
			return _inContent;
		}

		public string[] GetWorldNames()
		{
			return GetWorldNames(GetDataCenterId()).ToArray();
		}

		public new void Dispose()
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
			StopTimers();
			RosterService.SaveData();
			CategoryService.SaveCategories();
			LodestoneService.Dispose();
			base.Dispose();
			_pluginInterface.UiBuilder.OnOpenConfigUi -= (sender, args) => DrawConfigUI();
			_pluginInterface.UiBuilder.OnBuildUi -= DrawUI;
			_pluginInterface.Dispose();
			_isProcessing = false;
		}

		public new void SetupCommands()
		{
			_pluginInterface.CommandManager.AddHandler("/ptrack", new CommandInfo(TogglePlayerTrack)
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

		public void TogglePlayerTrack(string command, string args)
		{
			LogInfo("Running command {0} with args {1}", command, args);
			Configuration.ShowOverlay = !Configuration.ShowOverlay;
			_pluginUI.OverlayWindow.IsVisible = !_pluginUI.OverlayWindow.IsVisible;
		}

		public void ToggleConfig(string command, string args)
		{
			LogInfo("Running command {0} with args {1}", command, args);
			_pluginUI.SettingsWindow.IsVisible = !_pluginUI.SettingsWindow.IsVisible;
			_onSettingsTimer.Enabled = _pluginUI.SettingsWindow.IsVisible;
		}

		private void BackupOnStart()
		{
			if (!Configuration.FreshInstall) RosterService.BackupRoster(true);
		}

		private void StartTimers()
		{
			_onUpdateTimer = new Timer {Interval = Configuration.UpdateFrequency, Enabled = true};
			_onUpdateTimer.Elapsed += OnActorUpdate;
			_onSaveTimer = new Timer {Interval = Configuration.SaveFrequency, Enabled = true};
			_onSaveTimer.Elapsed += OnRosterSave;
			_onSettingsTimer = new Timer
				{Interval = Configuration.ProcessSettingsFrequency, Enabled = _pluginUI.SettingsWindow.IsVisible};
			_onSettingsTimer.Elapsed += OnSettings;
		}

		private void OnSettings(object sender, ElapsedEventArgs e)
		{
			// processing check
			if (_isProcessing) return;
			_isProcessing = true;

			// update categories
			CategoryService.ProcessCategoryModifications();

			_isProcessing = false;
		}

		private void StopTimers()
		{
			_onUpdateTimer.Elapsed -= OnActorUpdate;
			_onSaveTimer.Elapsed -= OnRosterSave;
			_onSettingsTimer.Elapsed -= OnSettings;
			_onUpdateTimer.Stop();
			_onSaveTimer.Stop();
			_onSettingsTimer.Stop();
		}

		private void OnRosterSave(object sender, ElapsedEventArgs e)
		{
			try
			{
				if (_isProcessing) return;
				_isProcessing = true;
				RosterService.SaveData();
				_isProcessing = false;
			}
			catch (Exception ex)
			{
				LogError(ex, "Failed to save players - will try again shortly.");
				_isProcessing = false;
			}
		}

		private void OnActorUpdate(object sender, ElapsedEventArgs e)
		{
			try
			{
				// processing check
				if (_isProcessing) return;
				_isProcessing = true;

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
					_isProcessing = false;
					return;
				}

				// content check
				var contentId = GetContentId(territoryTypeId);
				if (contentId == 0)
				{
					_inContent = false;
					if (Configuration.RestrictToContent)
					{
						_isProcessing = false;
						return;
					}
				}
				else
				{
					_inContent = true;
				}

				// high end duty check
				if (Configuration.RestrictToHighEndDuty && !IsHighEndDuty(contentId))
				{
					_isProcessing = false;
					return;
				}

				// custom content filter check
				if (Configuration.RestrictToCustom && !Configuration.PermittedContent.Contains(contentId))
				{
					_isProcessing = false;
					return;
				}

				// process pending requests
				RosterService.ProcessRequests();

				// player check
				var players = GetPlayerCharacters();
				if (players == null || !players.Any())
				{
					_isProcessing = false;
					return;
				}

				// build new roster of track players
				var placeName = GetPlaceName(territoryTypeId);
				var contentName = GetContentName(contentId);
				var newRoster = BuildNewRoster(territoryTypeId, placeName, contentName, players);

				// check roster built successfully
				if (newRoster == null)
				{
					_isProcessing = false;
					return;
				}

				// pass to roster service for processing against existing
				RosterService.ProcessPlayers(newRoster);

				// finish processing
				_isProcessing = false;
			}
			catch
			{
				_isProcessing = false;
			}
		}

		private List<TrackPlayer> BuildNewRoster(uint territoryType, string placeName, string contentName,
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
			RosterService = new RosterService(this);
			CategoryService = new CategoryService(this);
		}

		public void LoadUI()
		{
			Localization.SetLanguage(Configuration.PluginLanguage);
			_pluginUI = new PluginUI(this);
			_pluginInterface.UiBuilder.OnBuildUi += DrawUI;
			_pluginInterface.UiBuilder.OnOpenConfigUi += (sender, args) => DrawConfigUI();
		}

		private void HandleFreshInstall()
		{
			if (!Configuration.FreshInstall) return;
			PrintMessage(Loc.Localize("InstallThankYou", "Thank you for installing PlayerTrack!"));
			PrintHelpMessage();
			Configuration.FreshInstall = false;
			SetDefaultIcons();
			SaveConfig();
			_pluginUI.SettingsWindow.IsVisible = true;
		}

		private void DrawUI()
		{
			_pluginUI.Draw();
		}

		private void DrawConfigUI()
		{
			_pluginUI.SettingsWindow.IsVisible = false;
		}

		public new void LoadConfig()
		{
			try
			{
				Configuration = base.LoadConfig() as PluginConfig ?? new PluginConfig();
			}
			catch
			{
				LogInfo("Couldn't load config so creating one.");
				Configuration = new PluginConfig();
				SaveConfig();
			}
		}
	}
}