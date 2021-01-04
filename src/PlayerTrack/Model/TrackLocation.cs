using Newtonsoft.Json;

namespace PlayerTrack
{
	[JsonObject(MemberSerialization.OptIn)]
	public class TrackLocation
	{
		[JsonProperty] public uint TerritoryType { get; set; }

		public string PlaceName { get; set; }
		public string ContentName { get; set; }

		public override string ToString()
		{
			return string.IsNullOrEmpty(ContentName) ? PlaceName : ContentName;
		}
	}
}