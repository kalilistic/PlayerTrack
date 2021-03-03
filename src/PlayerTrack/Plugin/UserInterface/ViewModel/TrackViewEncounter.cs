using System.Collections.Generic;
using System.Linq;

namespace PlayerTrack
{
    public class TrackViewEncounter
    {
        public string Duration;
        public string JobCode;
        public string JobLvl;
        public string Location;
        public string Time;


        public static List<TrackViewEncounter> Map(List<TrackEncounter> encounters)
        {
            return encounters.ToList()
                .AsEnumerable()
                .Reverse()
                .Select(encounter => new TrackViewEncounter
                {
                    Time = encounter.Time,
                    Duration = encounter.Duration,
                    JobCode = encounter.Job.Code,
                    JobLvl = !encounter.Job.Lvl.ToString().Equals("0") ? encounter.Job.Lvl.ToString() : "",
                    Location = encounter.Location.ToString()
                })
                .ToList();
        }
    }
}