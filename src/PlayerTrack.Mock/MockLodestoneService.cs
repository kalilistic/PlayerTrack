using System;
using System.Collections.Generic;

namespace PlayerTrack.Mock
{
	public class MockLodestoneService : ILodestoneService
	{
		public List<TrackLodestoneResponse> GetResponses()
		{
			throw new NotImplementedException();
		}

		public List<TrackLodestoneResponse> GetUpdateResponses()
		{
			throw new NotImplementedException();
		}

		public void AddRequest(TrackLodestoneRequest request)
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