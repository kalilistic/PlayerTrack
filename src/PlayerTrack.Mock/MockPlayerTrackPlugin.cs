// ReSharper disable UnassignedGetOnlyAutoProperty

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PlayerTrack.Mock
{
	public class MockPlayerTrackPlugin : IPlayerTrackPlugin, IPluginBase
	{
		public PlayerService PlayerService { get; }
		public PlayerTrackConfig Configuration { get; set; }
		public Localization Localization { get; }
		public string PluginName { get; }
		void IPluginBase.PrintMessage(string message)
		{
			throw new NotImplementedException();
		}

		public string GetSeIcon(SeIconChar seIconChar)
		{
			throw new NotImplementedException();
		}

		public uint? GetLocalPlayerHomeWorld()
		{
			throw new NotImplementedException();
		}

		void IPluginBase.LogInfo(string messageTemplate)
		{
			throw new NotImplementedException();
		}

		public void LogInfo(string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		public void LogError(string messageTemplate)
		{
			throw new NotImplementedException();
		}

		public void LogError(string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		void IPluginBase.LogError(Exception exception, string messageTemplate, params object[] values)
		{
			throw new NotImplementedException();
		}

		public bool IsKeyPressed(ModifierKey.Enum key)
		{
			throw new NotImplementedException();
		}

		public bool IsKeyPressed(PrimaryKey.Enum key)
		{
			throw new NotImplementedException();
		}

		public void SaveConfig(dynamic config)
		{
			throw new NotImplementedException();
		}

		public dynamic LoadConfig()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public string PluginFolder()
		{
			throw new NotImplementedException();
		}

		public void UpdateResources()
		{
			throw new NotImplementedException();
		}

		public string PluginVersion()
		{
			throw new NotImplementedException();
		}

		bool IPluginBase.IsLoggedIn()
		{
			throw new NotImplementedException();
		}

		public TrackViewMode TrackViewMode { get; set; }
		public void PrintHelpMessage()
		{
			throw new NotImplementedException();
		}

		public void SaveConfig()
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.PrintMessage(string message)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.LogInfo(string messageTemplate)
		{
			throw new NotImplementedException();
		}

		void IPlayerTrackPlugin.LogError(Exception exception, string messageTemplate, params object[] values)
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

		public DataManager DataManager { get; set; }
		public LodestoneService LodestoneService { get; set; }
		public uint? GetWorldId(string worldName)
		{
			throw new NotImplementedException();
		}

		bool IPlayerTrackPlugin.IsLoggedIn()
		{
			throw new NotImplementedException();
		}

		public bool IsValidCharacterName(string name)
		{
			throw new NotImplementedException();
		}

		public string[] GetContentNames()
		{
			throw new NotImplementedException();
		}

		public uint[] GetContentIds()
		{
			throw new NotImplementedException();
		}

		public CategoryService CategoryService { get; set; }
		public bool InContent { get; set; }
		public string[] GetWorldNames()
		{
			throw new NotImplementedException();
		}

		public JsonSerializerSettings JsonSerializerSettings { get; set; }
		public void ToggleOverlay(string command, string args)
		{
			throw new NotImplementedException();
		}

		public void SelectPlayer(string playerKey)
		{
			throw new NotImplementedException();
		}

		public void ReloadList()
		{
			throw new NotImplementedException();
		}

		public string[] GetIconNames()
		{
			throw new NotImplementedException();
		}

		public int[] GetIconCodes()
		{
			throw new NotImplementedException();
		}
	}
}