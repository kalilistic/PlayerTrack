namespace PlayerTrack
{
	public class TrackLodestoneResponse
	{
		public string PlayerKey;
		public TrackLodestoneStatus Status { get; set; } = TrackLodestoneStatus.Unverified;
		public string PlayerName { get; set; }
		public uint LodestoneId { get; set; }
		public TrackWorld HomeWorld { get; set; }
	}
}