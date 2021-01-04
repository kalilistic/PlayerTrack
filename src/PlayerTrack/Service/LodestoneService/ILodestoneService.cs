using System.Collections.Generic;

namespace PlayerTrack
{
	public interface ILodestoneService
	{
		List<TrackLodestoneResponse> GetVerificationResponses();
		List<TrackLodestoneResponse> GetUpdateResponses();
		void AddIdRequest(TrackLodestoneRequest request);
		void AddUpdateRequest(TrackLodestoneRequest request);
		void Dispose();
	}
}