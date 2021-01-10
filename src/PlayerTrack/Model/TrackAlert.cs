using System.ComponentModel;
using Newtonsoft.Json;

namespace PlayerTrack
{
	[JsonObject(MemberSerialization.OptIn)]
	public class TrackAlert
	{
		[JsonProperty] [DefaultValue(false)] public bool Enabled { get; set; }
		[JsonProperty] [DefaultValue(0)] public long LastSent { get; set; }
	}
}