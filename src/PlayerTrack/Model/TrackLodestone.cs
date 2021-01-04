using System.ComponentModel;
using Newtonsoft.Json;

namespace PlayerTrack
{
	[JsonObject(MemberSerialization.OptIn)]
	public class TrackLodestone
	{
		[JsonProperty] [DefaultValue(0)] public uint Id { get; set; }
		[JsonProperty] [DefaultValue(0)] public long LastUpdated { get; set; }
		[JsonProperty] [DefaultValue(0)] public long LastFailed { get; set; }
		[JsonProperty] [DefaultValue(0)] public int FailureCount { get; set; }
		[JsonProperty] [DefaultValue(0)] public TrackLodestoneStatus Status { get; set; }

		public string LastUpdatedDisplay => LastUpdated == 0 ? "Never" : LastUpdated.ToTimeSpan();
	}
}