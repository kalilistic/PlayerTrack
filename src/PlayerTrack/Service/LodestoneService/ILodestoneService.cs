using System.Collections.Generic;

namespace PlayerTrack
{
	public interface ILodestoneService
	{
		List<TrackLodestoneResponse> GetVerificationResponses();
		void AddIdRequest(TrackLodestoneRequest request);
		void Dispose();
	}
}