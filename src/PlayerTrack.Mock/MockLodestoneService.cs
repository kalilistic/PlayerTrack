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

		public void AddRequest(TrackLodestoneRequest request)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}