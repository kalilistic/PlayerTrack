using System;
using System.Collections.Generic;

namespace PlayerTrack
{
	public interface IPlayerTrackPlugin
	{
		RosterService RosterService { get; }
		PlayerTrackConfig Configuration { get; set; }
		Localization Localization { get; }
		string PluginName { get; }
		void PrintHelpMessage();
		void Dispose();
		void SaveConfig();
		void SetupCommands();
		void RemoveCommands();
		void TogglePlayerTrack(string command, string args);
		void ToggleConfig(string command, string args);
		void ExportLocalizable(string command, string args);
		void PrintMessage(string message);
		string GetSeIcon(SeIconChar seIconChar);
		uint? GetLocalPlayerHomeWorld();
		void LogInfo(string messageTemplate);
		void LogInfo(string messageTemplate, params object[] values);
		void LogError(string messageTemplate);
		void LogError(string messageTemplate, params object[] values);
		void LogError(Exception exception, string messageTemplate, params object[] values);
		bool IsKeyPressed(ModifierKey.Enum key);
		bool IsKeyPressed(PrimaryKey.Enum key);
		void SaveConfig(dynamic config);
		void UpdateResources();
		string PluginVersion();
		string PluginFolder();
		string GetWorldName(uint worldId);
		string GetPlaceName(uint territoryTypeId);
		string GetContentName(uint contentId);
		uint GetContentId(uint territoryTypeId);
		string GetJobCode(uint classJobId);
		void SetDefaultIcons();
		void RestartTimers();
		DataManager GetDataManager();
		LodestoneService GetLodestoneService();
		uint? GetWorldId(string worldName);
		bool IsLoggedIn();
		List<string> GetWorldNames();
		bool IsValidCharacterName(string name);
		string[] GetContentNames();
		uint[] GetContentIds();
		CategoryService GetCategoryService();
	}
}