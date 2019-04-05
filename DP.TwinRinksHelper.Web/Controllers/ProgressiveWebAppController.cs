using DP.TwinRinks.YH.ScheduleParser;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DP.TwinRinksHelperWeb.Controllers
{
    [Produces("application/json")]
    [Route("api/mobile/[action]")]
    public class ProgressiveWebAppController : Controller
    {
        private readonly TwinRinksScheduleParserService _twinRinksService;

        public ProgressiveWebAppController(TwinRinksScheduleParserService twinRinksService)
        {
            _twinRinksService = twinRinksService;
        }

        [HttpGet]
        public string[] GetTeams()
        {
            return _twinRinksService.GetTeamsList().ToArray();
        }

        [HttpGet]
        public WeeklyScheduleResult[] GetSchedule(string team)
        {
            if (team == null)
            {
                throw new ArgumentException(nameof(team));
            }

            ConcurrentDictionary<int, List<TwinRinksEvent>> eventsByWeek = new ConcurrentDictionary<int, List<TwinRinksEvent>>();

            foreach (var e in _twinRinksService.GetEvents(team).Select(x => new { Week = x.EventDate.GetIso8601WeekOfYear(), Event = x }))
            {
                eventsByWeek.AddOrUpdate(e.Week, (i) => new List<TwinRinksEvent>() { e.Event }, (i, l) => { l.Add(e.Event); return l; });
            }

            return eventsByWeek.OrderBy(x => x.Key).Select(x => new WeeklyScheduleResult() { Events = x.Value.Select(e => new EventResult(e)).ToList() }).ToArray();
        }
    }

    public class EventResult
    {
        public EventResult(TwinRinksEvent evt)
        {
            IsPowerSkating = evt.IsPowerSkatingEvent();
            IsGame = evt.EventType == TwinRinksEventType.Game;
            IsAway = evt.Rink == TwinRinksRink.Away;
            OpponentName = IsGame ? evt.AwayTeamName : "";
            Description = IsGame ? "" : evt.HomeTeamName == evt.AwayTeamName ? evt.HomeTeamName : evt.HomeTeamName + " " + evt.AwayTeamName;
            LocationString = "@" + (IsAway ? evt.Location : evt.Rink.ToString() + " Rink");
            EventTypeString = evt.EventType.ToString();
            DateString = evt.EventDate.Date.ToString("ddd, MMM d").ToUpper();
            TimeString = DateTime.Today.Add(evt.EventStart).ToString("h:mm tt");

        }
        public bool IsPowerSkating { get; private set; }
        public bool IsGame { get; private set; }
        public bool IsAway { get; private set; }
        public string OpponentName { get; private set; }
        public string Description { get; private set; }
        public string LocationString { get; private set; }
        public string EventTypeString { get; private set; }
        public string DateString { get; private set; }
        public string TimeString { get; private set; }
    }


    public class WeeklyScheduleResult
    {
        public List<EventResult> Events { get; set; }
    }
}