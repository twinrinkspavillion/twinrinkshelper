using DP.TwinRinks.YH.ScheduleParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public class ScheduleComparer
{
    private readonly TeamSnapApi tsApi;
    private readonly TwinRinksScheduleParserService trApi;
    private readonly long teamSnapTeamId;
    private readonly string twinRinksTeam;
    private readonly HashSet<DateTime> exclusions;

    public CompareOptions Options { get; }
    public class CompareOptions
    {


    }
    public ScheduleComparer(TeamSnapApi tsApi, TwinRinksScheduleParserService trApi, long teamSnapTeamId, string twinRinksTeam, HashSet<DateTime> exclusions=null, CompareOptions options = null)
    {
        if (string.IsNullOrWhiteSpace(twinRinksTeam))
        {
            throw new System.ArgumentException("message", nameof(twinRinksTeam));
        }

        this.tsApi = tsApi ?? throw new System.ArgumentNullException(nameof(tsApi));
        this.trApi = trApi ?? throw new System.ArgumentNullException(nameof(trApi));

        this.teamSnapTeamId = teamSnapTeamId;
        this.twinRinksTeam = twinRinksTeam;
        this.exclusions = exclusions;

        Options = options ?? new CompareOptions();
    }
    public Task<IEnumerable<CompareResult>> RunCompareAsync()
    {
        return Task.Run<IEnumerable<CompareResult>>(async () =>
        {
            IEnumerable<TeamSnapApi.Event> tsEvents = await tsApi.GetEventsForTeam(teamSnapTeamId);

            IEnumerable<TwinRinksEvent> trEvents = trApi.GetEvents(twinRinksTeam);

            return Compare(tsEvents, trEvents, exclusions);
        });
    }
    public static IEnumerable<CompareResult> Compare(IEnumerable<TeamSnapApi.Event> tsEvents, IEnumerable<TwinRinksEvent> trEvents, HashSet<DateTime> exlusions = null)
    {
        List<CompareResult> res = new List<CompareResult>();

        HashSet<long> seenTsEvents = new HashSet<long>();

        foreach (TwinRinksEvent trEvent in trEvents)
        {
            if (exlusions != null && exlusions.Contains(trEvent.EventDate))
            {
                continue;
            }

            IEnumerable<TeamSnapApi.Event> tsEventsOnDay = tsEvents.Where(x => ToCSTTime(x.StartDate).Date == trEvent.EventDate.Date && x.IsCancelled == false).ToArray();

            seenTsEvents = new HashSet<long>(tsEventsOnDay.Select(e => e.Id));

            if (tsEventsOnDay.Any())
            {
                TeamSnapApi.Event foundTsEventByExactTime = tsEventsOnDay.Where(x => ToCSTTime(x.StartDate).TimeOfDay == trEvent.EventStart).FirstOrDefault();

                if (foundTsEventByExactTime != null)
                {
                    if (foundTsEventByExactTime.LocationName.Equals(trEvent.Location, StringComparison.InvariantCultureIgnoreCase) && foundTsEventByExactTime.IsGame == (trEvent.EventType == TwinRinksEventType.Game))
                    {
                        //matched location and event type
                    }
                    else
                    {

                        res.Add(new CompareResult() { Type = DifferenceType.WrongLocOrEvtType, TR_EventTime = (trEvent.EventDate + trEvent.EventStart), TR_EventType = trEvent.EventType, TR_Location = trEvent.Location, TS_Location = foundTsEventByExactTime.LocationName, TS_EventType = foundTsEventByExactTime.IsGame ? TwinRinksEventType.Game : TwinRinksEventType.Practice, TS_NumEvents = tsEventsOnDay.Count() });

                        //not matched on location or event type
                    }

                }
                else
                {
                    TeamSnapApi.Event foundTsEventByEventTypeAndLocation = tsEventsOnDay.Where(x => x.LocationName.Equals(trEvent.Location, StringComparison.InvariantCultureIgnoreCase) && x.IsGame == (trEvent.EventType == TwinRinksEventType.Game)).FirstOrDefault();

                    if (foundTsEventByEventTypeAndLocation != null)
                    {
                        //time not matched
                        res.Add(new CompareResult() { Type = DifferenceType.WrongTimeInTeamSnap, TR_EventTime = (trEvent.EventDate + trEvent.EventStart), TR_EventType = trEvent.EventType, TR_Location = trEvent.Location, TS_EventTime = ToCSTTime(foundTsEventByEventTypeAndLocation.StartDate), TS_NumEvents = tsEventsOnDay.Count(), TS_EventID = foundTsEventByEventTypeAndLocation.Id });

                    }
                    else
                    {
                        //nothing matching
                        res.Add(new CompareResult() { Type = DifferenceType.NotInTeamSnap, TR_EventTime = (trEvent.EventDate + trEvent.EventStart), TR_EventType = trEvent.EventType, TR_Location = trEvent.Location, TS_NumEvents = tsEventsOnDay.Count() });

                    }
                }
            }
            else
            {
                //nothing on this date

                res.Add(new CompareResult() { Type = DifferenceType.NotInTeamSnap, TR_EventTime = (trEvent.EventDate + trEvent.EventStart), TR_EventType = trEvent.EventType, TR_Location = trEvent.Location });

            }
        }

        foreach (TeamSnapApi.Event tsEvent in tsEvents.Where(x => !x.IsCancelled && ToCSTTime(x.StartDate) > ToCSTTime(DateTime.Now.Date)))
        {
            if (exlusions != null && exlusions.Contains(ToCSTTime(tsEvent.StartDate).Date))
            {
                continue;
            }

            if (!seenTsEvents.Contains(tsEvent.Id))
            {
                TwinRinksEvent trEvent = trEvents.Where(x => ToCSTTime(tsEvent.StartDate).Date == x.EventDate.Date).Where(x => ToCSTTime(tsEvent.StartDate).TimeOfDay == x.EventStart).FirstOrDefault();

                int cnt = trEvents.Where(x => ToCSTTime(tsEvent.StartDate).Date == x.EventDate.Date).Count();

                if (trEvent == null)
                {
                    res.Add(new CompareResult() { Type = DifferenceType.NotOnTwinRinksWebSite, TS_EventTime = ToCSTTime(tsEvent.StartDate), TS_Location = tsEvent.LocationName, TS_EventType = tsEvent.IsGame ? TwinRinksEventType.Game : TwinRinksEventType.Practice, TR_NumEvents = cnt, TS_EventID = tsEvent.Id });

                }
            }
        }




        return res;
    }
    public static DateTime ToCSTTime(DateTime d)
    {
        DateTime clientDateTime = d;

        DateTime centralDateTime = System.TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clientDateTime, "Central Standard Time");

        return centralDateTime;
    }
    public enum DifferenceType
    {
        NotInTeamSnap,
        WrongTimeInTeamSnap,
        WrongLocOrEvtType,
        NotOnTwinRinksWebSite
    }
    public class CompareResult
    {
        public DifferenceType? Type { get; set; }
        public DateTime? TR_EventTime { get; internal set; }
        public TwinRinksEventType? TR_EventType { get; internal set; }
        public string TR_Location { get; internal set; }
        public int? TR_NumEvents { get; internal set; }
        public int? TS_NumEvents { get; internal set; }
        public DateTime? TS_EventTime { get; internal set; }
        public TwinRinksEventType? TS_EventType { get; internal set; }
        public string TS_Location { get; internal set; }

        public long? TS_EventID;
    }
}

