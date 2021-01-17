using System.ComponentModel;
using System.Numerics;
using Newtonsoft.Json;

namespace PlayerTrack
{
	[JsonObject(MemberSerialization.OptIn)]
	public class TrackCategory
	{
		[JsonProperty] [DefaultValue(0)] public int Id;
		[JsonProperty] [DefaultValue("")] public string Name = string.Empty;
		[JsonProperty] [DefaultValue(0)] public int Icon { get; set; }
		[JsonProperty] public Vector4 Color { get; set; }
		[JsonProperty] [DefaultValue(false)] public bool EnableAlerts { get; set; }
		[JsonProperty] [DefaultValue(false)] public bool IsDefault { get; set; }
	}
}