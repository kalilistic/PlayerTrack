using System.Collections.Generic;

namespace PlayerTrack
{
    public interface ILodestoneService
    {
        List<TrackLodestoneResponse> GetResponses();
        void AddRequest(TrackLodestoneRequest request);
        void Dispose();
    }
}