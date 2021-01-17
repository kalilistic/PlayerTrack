using System.ComponentModel;
using Newtonsoft.Json;

namespace PlayerTrack
{
	[JsonObject(MemberSerialization.OptIn)]
	public class TrackAlert
	{
		[JsonProperty]
		[DefaultValue(TrackAlertState.NotSet)]
		public TrackAlertState State { get; set; }

		[JsonProperty] [DefaultValue(0)] public long LastSent { get; set; }
	}
}