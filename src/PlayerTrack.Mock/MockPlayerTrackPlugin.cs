// ReSharper disable UnassignedGetOnlyAutoProperty

using System;
using System.Collections.Generic;

namespace PlayerTrack.Mock
{
	public class MockPlayerTrackPlugin : IPlayerTrackPlugin, IPluginBase
	{
		public RosterService RosterService { get; }
		public PlayerTrackConfig Configuration { get; set; }
		public Localization Localization { get; }
		public string PluginName { get; }

		public void PrintHelpMessage()
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.Dispose()
		{
			throw new NotImplementedException();
		}

		public void SaveConfig()
		{
			throw new NotImplementedException();
		}

		public void SetupCommands()
		{
			throw new NotImplementedException();
		}

		public void RemoveCommands()
		{
			throw new NotImplementedException();
		}

		public void TogglePlayerTrack(string command, string args)
		{
			throw new NotImplementedException();
		}

		public void ToggleConfig(string command, string args)
		{
			throw new NotImplementedException();
		}

		public void ExportLocalizable(string command, string args)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.PrintMessage(string message)
		{
			throw new NotImplementedException();
		}

		string IPlayerTrackPlugin.GetSeIcon(SeIconChar seIconChar)
		{
			throw new NotImplementedException();
		}

		uint? IPlayerTrackPlugin.GetLocalPlayerHomeWorld()
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.LogInfo(string messageTemplate)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.LogInfo(string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.LogError(string messageTemplate)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.LogError(string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.LogError(Exception exception, string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		bool IPlayerTrackPlugin.IsKeyPressed(ModifierKey.Enum key)
		{
			throw new NotImplementedException();
		}

		bool IPlayerTrackPlugin.IsKeyPressed(PrimaryKey.Enum key)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.SaveConfig(dynamic config)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.UpdateResources()
		{
			throw new NotImplementedException();
		}

		string IPlayerTrackPlugin.PluginVersion()
		{
			throw new NotImplementedException();
		}

		string IPlayerTrackPlugin.PluginFolder()
		{
			throw new NotImplementedException();
		}

		public Dictionary<string, TrackPlayer> GetCurrentPlayers()
		{
			throw new NotImplementedException();
		}

		public Dictionary<string, TrackPlayer> GetAllPlayers()
		{
			throw new NotImplementedException();
		}

		public Dictionary<string, TrackPlayer> GetRecentPlayers()
		{
			throw new NotImplementedException();
		}

		public Dictionary<string, TrackPlayer> GetPlayersByName(string name)
		{
			throw new NotImplementedException();
		}

		public string GetWorldName(uint worldId)
		{
			throw new NotImplementedException();
		}

		public string GetPlaceName(uint territoryTypeId)
		{
			throw new NotImplementedException();
		}

		public string GetContentName(uint contentId)
		{
			throw new NotImplementedException();
		}

		public uint GetContentId(uint territoryTypeId)
		{
			throw new NotImplementedException();
		}

		public string GetJobCode(uint classJobId)
		{
			throw new NotImplementedException();
		}

		public void SetDefaultIcons()
		{
			throw new NotImplementedException();
		}

		public void RestartTimers()
		{
			throw new NotImplementedException();
		}

		public DataManager GetDataManager()
		{
			throw new NotImplementedException();
		}

		public LodestoneService GetLodestoneService()
		{
			throw new NotImplementedException();
		}

		public uint? GetWorldId(string worldName)
		{
			throw new NotImplementedException();
		}

		public dynamic LoadConfig()
		{
			throw new NotImplementedException();
		}

		void IPluginBase.Dispose()
		{
			throw new NotImplementedException();
		}

		string IPluginBase.PluginFolder()
		{
			throw new NotImplementedException();
		}

		void IPluginBase.UpdateResources()
		{
			throw new NotImplementedException();
		}

		string IPluginBase.PluginVersion()
		{
			throw new NotImplementedException();
		}

		string IPluginBase.GetSeIcon(SeIconChar seIconChar)
		{
			throw new NotImplementedException();
		}

		uint? IPluginBase.GetLocalPlayerHomeWorld()
		{
			throw new NotImplementedException();
		}

		void IPluginBase.LogInfo(string messageTemplate)
		{
			throw new NotImplementedException();
		}

		void IPluginBase.LogInfo(string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		void IPluginBase.LogError(string messageTemplate)
		{
			throw new NotImplementedException();
		}

		void IPluginBase.LogError(string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		void IPluginBase.LogError(Exception exception, string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		bool IPluginBase.IsKeyPressed(ModifierKey.Enum key)
		{
			throw new NotImplementedException();
		}

		bool IPluginBase.IsKeyPressed(PrimaryKey.Enum key)
		{
			throw new NotImplementedException();
		}

		void IPluginBase.SaveConfig(dynamic config)
		{
			throw new NotImplementedException();
		}

		void IPluginBase.PrintMessage(string message)
		{
			throw new NotImplementedException();
		}
	}
}