using System;
using Newtonsoft.Json;

namespace PlayerTrack
{
	public interface IPlayerTrackPlugin
	{
		PlayerService PlayerService { get; }
		PlayerTrackConfig Configuration { get; set; }
		Localization Localization { get; }
		string PluginName { get; }
		TrackViewMode TrackViewMode { get; set; }
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
		DataManager DataManager { get; set; }
		LodestoneService LodestoneService { get; set; }
		uint? GetWorldId(string worldName);
		bool IsLoggedIn();
		bool IsValidCharacterName(string name);
		string[] GetContentNames();
		uint[] GetContentIds();
		CategoryService CategoryService { get; set; }
		bool InContent { get; set; }
		string[] GetWorldNames();
		JsonSerializerSettings JsonSerializerSettings { get; set; }
		void ToggleOverlay(string command, string args);
		void SelectPlayer(string playerKey);
		void ReloadList();
		string[] GetIconNames();
		int[] GetIconCodes();
		string GetGender(int? genderId);
		string GetRace(int id, int genderId);
		string GetTribe(int id, int genderId);
		double ConvertHeightToInches(int raceId, int tribeId, int genderId, int sliderHeight);
	}
}