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
		void SaveConfig();
		void PrintMessage(string message);
		void LogInfo(string messageTemplate);
		void LogError(Exception exception, string messageTemplate, params object[] values);
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
		bool InContent();
	}
}