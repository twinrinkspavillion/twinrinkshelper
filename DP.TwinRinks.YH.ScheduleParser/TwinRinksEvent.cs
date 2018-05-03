using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DP.TwinRinks.YH.ScheduleParser
{

    public enum TwinRinksEventType : int
    {
        Game,
        Practice
    }

    public enum TwinRinksRink : int
    {
        Blue,
        Red,
        Away
    }

    public enum TwinRinksTeamLevel : int
    {
        Mite,
        Squirt,
        PeeWee,
        Bantam,

    }

    [Flags]
    public enum TwinRinksEventField : int
    {
        None = 1 << 0,
        EventDate  = 1 << 1,
        EventType = 1 << 2,
        Rink = 1 << 3,
        EventStart = 1 << 4,
        EventEnd = 1 << 5,
        Location = 1 << 6,
        EventDescription = 1 << 7,
        HomeTeamName = 1 << 8,
        AwayTeamName = 1 << 9
    }
    public class TwinRinksEvent
    { 
        public DateTime EventDate { get; set; }
        public TwinRinksEventType EventType { get; set; }
        public TwinRinksRink Rink { get; set; }
        public TimeSpan EventStart { get; set; }
        public TimeSpan EventEnd { get; set; }
        public string Location { get; set; }
        public string EventDescription { get; set; }
        public string HomeTeamName { get; set; }
        public string AwayTeamName { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
