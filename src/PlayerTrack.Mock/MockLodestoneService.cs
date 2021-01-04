using System;
using System.Collections.Generic;

namespace PlayerTrack.Mock
{
	public class MockLodestoneService : ILodestoneService
	{
		public List<TrackLodestoneResponse> GetVerificationResponses()
		{
			throw new NotImplementedException();
		}

		public List<TrackLodestoneResponse> GetUpdateResponses()
		{
			throw new NotImplementedException();
		}

		public void AddIdRequest(TrackLodestoneRequest request)
		{
			throw new NotImplementedException();
		}

		public void AddUpdateRequest(TrackLodestoneRequest request)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}