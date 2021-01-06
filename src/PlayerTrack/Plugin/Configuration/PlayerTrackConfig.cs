using System.Collections.Generic;
using System.Numerics;

namespace PlayerTrack
{
	public abstract class PlayerTrackConfig
	{
		public int BackupFrequency = 14400000;
		public int BackupRetention = 10;
		public List<FontAwesomeIcon> EnabledIcons = new List<FontAwesomeIcon>();
		public long LastBackup = 0;
		public int LodestoneCooldownDuration = 3600000;
		public int LodestoneFailureDelay = 86400000;
		public int LodestoneMaxFailure = 3;
		public int LodestoneMaxRetry = 3;
		public int LodestoneRequestDelay = 10000;
		public int LodestoneTimeout = 30000;
		public int LodestoneUpdateFrequency = 172800000;
		public TrackLodestoneLocale LodestoneLocale = TrackLodestoneLocale.na;
		public int NewEncounterThreshold = 86400000;
		public int RecentPlayerThreshold = 300000;
		public bool RestrictInCombat = true;
		public bool RestrictToContent = true;
		public bool RestrictToHighEndDuty = false;
		public int SaveFrequency = 60000;
		public bool ShowPlayerCount = true;
		public bool SyncToLodestone = true;
		public int UpdateFrequency = 5000;
		public bool FreshInstall { get; set; } = true;
		public bool Compressed { get; set; } = true;
		public int SchemaVersion { get; set; } = 1;
		public bool Enabled { get; set; } = true;
		public bool ShowOverlay { get; set; } = true;
		public int PluginLanguage { get; set; } = 0;
		public bool ShowIcons { get; set; } = true;
		public Vector4 DefaultColor { get; set; } = new Vector4(255, 255, 255, 1);
		public FontAwesomeIcon DefaultIcon { get; set; } = FontAwesomeIcon.User;
	}
}