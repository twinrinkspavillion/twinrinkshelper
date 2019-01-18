
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using DP.TwinRinks.YH.ScheduleParser;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DP.TwinRinksHelper.Web.Services
{
    public class AlexaTwinRinksSkillRequestProcessor
    {

        public static class Intents
        {
            public const string ListEvents = "listeventsintent";
            public const string ListTeams = "listteamsintent";
            public const string EventQuestion = "eventquestionintent";

        }

        public enum EventType
        {
            Power,
            Practice,
            Game,
            Event

        }
        public static class Phrases
        {
            public const string HelpString = "You can say things like: list teams!, list games for this weekend!, list events for tomorrow!, are there games this weekend?";
        }


        private readonly TwinRinksScheduleParserService parserService;

        public AlexaTwinRinksSkillRequestProcessor(TwinRinksScheduleParserService parserService)
        {
            this.parserService = parserService;
        }
        internal SkillResponse ProcessRequest(SkillRequest request)
        {
            Type reqType = request.GetRequestType();

            if (reqType == typeof(LaunchRequest))
            {
                return ProcessStartIntent();

            }
            else if (reqType == typeof(IntentRequest))
            {
                IntentRequest intentRequest = request.Request as IntentRequest;

                switch (intentRequest.Intent.Name)
                {
                    case Intents.EventQuestion:
                        return ProcessListEventsRequest(intentRequest, false);
                    case Intents.ListEvents:
                        return ProcessListEventsRequest(intentRequest);
                    case Intents.ListTeams:
                        return ProcessListTeamsRequest(intentRequest);
                    case "AMAZON.HelpIntent":
                        return ProcessHelpRequest(intentRequest);
                    case "AMAZON.FallbackIntent":
                        return ProcessHelpRequest(intentRequest, "I did not understand! ");
                    case "AMAZON.StopIntent":
                    case "AMAZON.CancelIntent":
                    default:
                        return ProcessStopIntentRequest(intentRequest);
                }
            }

            return ResponseBuilder.Empty();
        }





        private SkillResponse ProcessListTeamsRequest(IntentRequest request)
        {
            return ResponseBuilder.Tell(new SsmlOutputSpeech() { Ssml = "<speak>" + GetTeamsListSsml() + "</speak>" });
        }

        private SkillResponse ProcessListEventsRequest(IntentRequest request, bool listEvents = true)
        {
            EventType evtType = ExtractEventType(request.Intent.Slots["eventType"]);

            AmazonDateParser.DateRange range = ExtractDateRange(request.Intent.Slots["eventHorizon"]);

            string teamName = request.Intent.Slots["teamname"].Value.ToUpperInvariant();

            if (parserService.GetTeamsList().Contains(teamName))
            {
                System.Collections.Generic.IEnumerable<TwinRinks.YH.ScheduleParser.TwinRinksEvent> candidateEvents = parserService.GetEvents(teamName).Where(x => (x.EventDate + x.EventStart) > range.Start && (x.EventDate + x.EventStart) < range.End);

                if (evtType == EventType.Practice)
                {
                    candidateEvents = candidateEvents.Where(x => x.EventType == TwinRinksEventType.Practice);
                }
                else if (evtType == EventType.Power)
                {
                    candidateEvents = candidateEvents.Where(x => x.IsPowerSkatingEvent());
                }
                else if (evtType == EventType.Game)
                {
                    candidateEvents = candidateEvents.Where(x => x.EventType == TwinRinksEventType.Game);

                }
                if (listEvents)
                {
                    return BuildEventListRespose(candidateEvents);
                }
                else
                {
                    return BuildEventQuestionRespose(candidateEvents);
                }
            }
            else
            {
                return ResponseBuilder.Tell(new SsmlOutputSpeech() { Ssml = $"<speak>Team {teamName} not found! Available teams are: {GetTeamsListSsml()}</speak>" });

            }
        }

        private SkillResponse BuildEventQuestionRespose(IEnumerable<TwinRinksEvent> candidateEvents)
        {
            if (!candidateEvents.Any())
            {
                return ResponseBuilder.Tell("No. There are no requested events during time range requested.");
            }
            else
            {
                return ResponseBuilder.Tell(new SsmlOutputSpeech() { Ssml = $"<speak>Yes. There are events in time range requested<break time=\"500ms\"/>: {BuildEventListSsmlFragment(candidateEvents)}</speak>" });
            }
        }
        private SkillResponse BuildEventListRespose (IEnumerable<TwinRinksEvent> candidateEvents)
        {
            if (!candidateEvents.Any())
            {
                return ResponseBuilder.Tell("There are no events during time range requested.");
            }
            else
            {
                return ResponseBuilder.Tell(new SsmlOutputSpeech() { Ssml = $"<speak>Following are the events in time range requested<break time=\"500ms\"/>: {BuildEventListSsmlFragment(candidateEvents)}</speak>" });
            }
        }

        private string BuildEventListSsmlFragment(IEnumerable<TwinRinksEvent> candidateEvents)
        {
            StringBuilder res = new StringBuilder();

            foreach (TwinRinksEvent e in candidateEvents)
            {
                string eventNameString = e.EventType == TwinRinksEventType.Game ? "game vs " + e.AwayTeamName : e.IsPowerSkatingEvent() ? "power skating" : "practice";

                string dateString = e.EventDate.ToString("dddd, MMM dd") + e.EventDate.GetDaySuffix();
                string timeString = (e.EventDate + e.EventStart).ToString("hh:mm tt");

                if (e.EventType == TwinRinksEventType.Game)
                {
                    string awayHomeString = e.Rink == TwinRinksRink.Away ? "an away" : "a home";
                    string locationString = e.Rink == TwinRinksRink.Away ? " at " + e.Location : " on " + e.Rink.ToString() + " Rink";

                    res.Append($"{dateString} at {timeString} <break time=\"250ms\"/> there is {awayHomeString} {eventNameString} {locationString}<break time=\"500ms\"/>");
                }
                else
                {
                    string locationString = e.Rink.ToString() + " Rink";

                    res.Append($"{dateString} at {timeString} <break time=\"250ms\"/> there is {eventNameString} on {locationString}<break time=\"500ms\"/>");
                }

            }

            return res.ToString();
        }
        private SkillResponse ProcessHelpRequest(IntentRequest request, string prefix = null)
        {
            return ResponseBuilder.Tell(prefix + Phrases.HelpString);
        }

        private SkillResponse ProcessStartIntent()
        {
            return ResponseBuilder.Tell("Welcome to Twin Rinks Youth Hockey! " + Phrases.HelpString);
        }

        private SkillResponse ProcessStopIntentRequest(IntentRequest request)
        {
            return ResponseBuilder.Empty();
        }

        private AmazonDateParser.DateRange ExtractDateRange(Slot slot)
        {
            if (string.IsNullOrWhiteSpace(slot.Value) || !AmazonDateParser.TryParse(slot.Value, out AmazonDateParser.DateRange dateRange))
            {
                DateTime weekendStart = DateTime.Today.Next(DayOfWeek.Friday);
                DateTime weekendEnd = DateTime.Today.Next(DayOfWeek.Sunday).AddHours(23);

                return new AmazonDateParser.DateRange() { Type = AmazonDateParser.DateRangeType.Weekend, Start = weekendStart, End = weekendEnd };

            }
            else
            {
                if (dateRange.Type == AmazonDateParser.DateRangeType.Weekend)
                {
                    dateRange.Start = dateRange.Start.AddDays(-1);

                }
                return dateRange;
            }

        }

        private EventType ExtractEventType(Slot slot)
        {
            return Enum.Parse<EventType>(slot.Resolution.Authorities[0].Values[0].Value.Id, true);
        }
        private string GetTeamsListSsml()
        {
            return "Available teams are: " + string.Join(", <break time=\"500ms\"/>", parserService.GetTeamsList().ToArray());
        }
    }
    public static class AmazonDateParser
    {
        public static DateTime ToCSTTime(this DateTime d)
        {
            DateTime clientDateTime = d;

            DateTime centralDateTime = System.TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clientDateTime, "Central Standard Time");

            return centralDateTime;
        }
        public static string GetDaySuffix(this DateTime dt)
        {
            string suffix;

            if (new[] { 11, 12, 13 }.Contains(dt.Day))
            {
                suffix = "th";
            }
            else if (dt.Day % 10 == 1)
            {
                suffix = "st";
            }
            else if (dt.Day % 10 == 2)
            {
                suffix = "nd";
            }
            else if (dt.Day % 10 == 3)
            {
                suffix = "rd";
            }
            else
            {
                suffix = "th";
            }

            return suffix;
        }
        public static DateRange Parse(string amazonDateString)
        {
            if (TryParse(amazonDateString, out DateRange range))
            {
                return range;
            }

            throw new Exception($"Could not parse: {amazonDateString}");
        }
        public static bool TryParse(string amazonDateString, out DateRange range)
        {
            range = new DateRange();

            if (string.IsNullOrWhiteSpace(amazonDateString))
            {
                throw new ArgumentNullException(nameof(amazonDateString));
            }

            if (DateTime.TryParseExact(amazonDateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime exactDate))
            {
                range.Start = exactDate;
                range.End = exactDate.AddHours(23).AddMinutes(59).AddSeconds(59);
                range.Type = DateRangeType.ExactDate;

                return true;
            }

            string[] parts = amazonDateString.Split('-');

            if (parts.Length == 2 && (parts[1] == "SP" || parts[1] == "SU" || parts[1] == "FA" || parts[1] == "WI"))
            {
                ParseSeason(parts[0], parts[1], range);

                return true;
            }
            else if (parts.Length == 2 && parts[1].Contains("W"))
            {
                ParseWeek(range, parts);

                return true;
            }
            else if (parts.Length == 2 && parts[1].Contains("Q"))
            {
                ParseQuarter(range, parts);

                return true;

            }
            else if (parts.Length == 2 && int.TryParse(parts[0], out int year) && int.TryParse(parts[1], out int month))
            {

                range.Start = new DateTime(year, month, 1);
                range.End = new DateTime(year, month + 1, 1, 23, 59, 59).AddDays(-1);
                range.Type = DateRangeType.Month;


                return true;
            }
            else if (parts.Length == 3 && parts[2].Equals("WE"))
            {
                int weekNum = int.Parse(parts[1].Substring(1, 2));

                DateTime startDate = FirstDateOfWeekISO8601(int.Parse(parts[0]), weekNum);

                range.Type = DateRangeType.Weekend;
                range.Start = startDate.AddDays(5);
                range.End = startDate.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);


                return true;

            }
            else if (parts.Length == 1 && int.TryParse(parts[0], out int year1))
            {

                range.Start = new DateTime(year1, 1, 1);
                range.End = new DateTime(year1, 12, 31, 23, 59, 59).AddDays(-1);
                range.Type = DateRangeType.Year;

                return true;
            }

            return false;

        }
        private static void ParseQuarter(DateRange range, string[] parts)
        {
            int year = int.Parse(parts[0]);
            int quarter = int.Parse(parts[1].Substring(1, 1));
            int startMonth = (((12 / 4) * quarter) - 3) + 1;
            int endMonth = startMonth + 2;

            range.Start = new DateTime(year, startMonth, 1);
            range.End = new DateTime(year, endMonth + 1, 1, 23, 59, 59).AddDays(-1);
            range.Type = DateRangeType.Quarter;
        }
        private static void ParseWeek(DateRange range, string[] parts)
        {
            int weekNum = int.Parse(parts[1].Substring(1, 2));

            DateTime startDate = FirstDateOfWeekISO8601(int.Parse(parts[0]), weekNum);

            range.Start = startDate;
            range.End = startDate.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
            range.Type = DateRangeType.Week;
        }
        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            Calendar cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            int weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            DateTime result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return result.AddDays(-3);
        }
        private static void ParseSeason(string yearStr, string seasonStr, DateRange range)
        {
            int year = int.Parse(yearStr);

            range.Type = DateRangeType.Season;

            switch (seasonStr)
            {
                case "SP":

                    range.Start = new DateTime(year, 3, 1);
                    range.End = new DateTime(year, 5, 31, 11, 59, 59);

                    break;

                case "SU":

                    range.Start = new DateTime(year, 6, 1);
                    range.End = new DateTime(year, 8, 31, 11, 59, 59);

                    break;

                case "FA":

                    range.Start = new DateTime(year, 9, 1);
                    range.End = new DateTime(year, 11, 30, 11, 59, 59);

                    break;

                case "WI":

                    range.Start = new DateTime(year, 12, 1);
                    range.End = new DateTime(year + 1, 3, 1, 11, 59, 59).AddDays(-1);

                    break;

            }
        }
        public enum DateRangeType
        {
            ExactDate,
            Week,
            Weekend,
            Month,
            Quarter,
            Season,
            Year,
            Century
        }
        public class DateRange
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public DateRangeType Type { get; set; }
        }
        public static DateTime Next(this DateTime from, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;

            if (target < start)
            {
                target += 7;
            }

            return from.AddDays(target - start);
        }
    }
}

public static class AlexaSkillsExtentions
{
    public static IServiceCollection AddAlexaSkills(this IServiceCollection me)
    {
        return me.AddTransient<DP.TwinRinksHelper.Web.Services.AlexaTwinRinksSkillRequestProcessor>();
    }

}
