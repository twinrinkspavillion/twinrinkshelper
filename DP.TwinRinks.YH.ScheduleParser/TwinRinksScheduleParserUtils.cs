using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Ical.Net.Serialization;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using System.Security.Cryptography;

namespace DP.TwinRinks.YH.ScheduleParser
{
    public static class TwinRinksScheduleParserUtils
    {
        public static bool IsDifferentFrom(this TwinRinksEvent me, TwinRinksEvent other, out HashSet<TwinRinksEventField> whichFields)
        {
            if (me == null)
            {
                throw new ArgumentNullException(nameof(me));
            }

            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }


            bool res = false;

            whichFields = new HashSet<TwinRinksEventField>();

            if(!me.AwayTeamName.Equals(other.AwayTeamName, StringComparison.InvariantCultureIgnoreCase))
            {
                res = true;

                whichFields.Add(TwinRinksEventField.AwayTeamName);
            }

            if (me.EventDate != other.EventDate)
            {
                res = true;

                whichFields.Add(TwinRinksEventField.EventDate);
            }


            if (me.EventDescription != other.EventDescription)
            {
                res = true;

                whichFields.Add(TwinRinksEventField.EventDescription);
            }


            if (me.EventEnd != other.EventEnd)
            {
                res = true;

                whichFields.Add(TwinRinksEventField.EventEnd);
            }

            if (me.EventStart != other.EventStart)
            {
                res = true;

                whichFields.Add(TwinRinksEventField.EventStart);
            }

            if (me.EventType != other.EventType)
            {
                res = true;

                whichFields.Add(TwinRinksEventField.EventType);
            }

            if (!me.HomeTeamName.Equals(other.HomeTeamName))
            {
                res = true;

                whichFields.Add(TwinRinksEventField.HomeTeamName);
            }


            if (!me.Location.Equals(other.Location))
            {
                res = true;

                whichFields.Add(TwinRinksEventField.Location);
            }


            if (me.Rink != other.Rink)
            {
                res = true;

                whichFields.Add(TwinRinksEventField.Rink);
            }

            return res;
        }


        public static string GenerateIdentifier(this TwinRinksEvent me)
        {
            return Sha1Hash(me.ToString());
        }
        static string Sha1Hash(string input)
        {
            var hash = (new SHA1Managed()).ComputeHash(Encoding.ASCII.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }

        public static TwinRinksEvent ToEvent(this TwinRinksParsedScheduleItem item)
        {

            TwinRinksEvent tre = new TwinRinksEvent();
            tre.AwayTeamName = item.Away;
            tre.HomeTeamName = item.Home;
            tre.Location = ParseLocation(item);
            tre.Rink = ParseRink(item);
            tre.EventType = ParseEventType(item);
            tre.EventDescription = item.Description;
            tre.EventDate = DateTime.Parse(item.Date);
            tre.EventEnd = DateTime.Parse(item.End + "M").TimeOfDay;
            tre.EventStart = DateTime.Parse(item.Start + "M").TimeOfDay;

            return tre;


        }

        public static IEnumerable<TwinRinksEvent> FilterTeamEvents(this IEnumerable<TwinRinksEvent> me, TwinRinksTeamLevel level, object teamDesignator)
        {
            string teamName = $"{level.ToString().ToUpperInvariant()} {teamDesignator.ToString().ToUpperInvariant()}";
            string allTeamsName = $"ALL {level.ToString().ToUpperInvariant()}S";
            string levelStr = level.ToString().ToUpperInvariant();

            foreach (var e in me)
            {

                if (e.EventType == TwinRinksEventType.Game)
                {
                    if (e.HomeTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                        yield return e;
                    else if (e.AwayTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                        yield return e;
                }
                else // practice
                {
                    var isPowerSkate = e.IsPowerSkatingEvent();

                    if (e.HomeTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                        yield return e;
                    else if (e.AwayTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                        yield return e;
                    else if (e.HomeTeamName.Equals(allTeamsName, StringComparison.InvariantCultureIgnoreCase))
                        yield return e;
                    else if (e.AwayTeamName.Equals(allTeamsName, StringComparison.InvariantCultureIgnoreCase))
                        yield return e;
                    else if (isPowerSkate && (e.HomeTeamName.ToUpperInvariant().Contains(levelStr) || e.AwayTeamName.ToUpperInvariant().Contains(levelStr)))
                        yield return e;

                }


            }

        }

        public static IEnumerable<TwinRinksEventConflict> FindConflictsWith(this IEnumerable<TwinRinksEvent> me, IEnumerable<TwinRinksEvent> other, double minEventStartHoursDiff = 1.5)
        {
            Dictionary<DateTime, List<TwinRinksEvent>> byDateIndex = new Dictionary<DateTime, List<TwinRinksEvent>>();

            foreach (var evt in other)
            {
                if (!byDateIndex.TryGetValue(evt.EventDate, out List<TwinRinksEvent> lst))
                    lst = byDateIndex[evt.EventDate] = new List<TwinRinksEvent>();

                lst.Add(evt);
            }

            foreach (var evt in me)
            {
                if (byDateIndex.TryGetValue(evt.EventDate, out List<TwinRinksEvent> lst))
                {
                    foreach (var candidate in lst)
                    {
                        if (Math.Abs((candidate.EventStart - evt.EventStart).TotalHours) < minEventStartHoursDiff)
                        {
                            yield return new TwinRinksEventConflict() { FirstEvent = evt, SecondEvent = candidate };
                        }
                    }

                }
            }
        }
        public static bool TryParseTeamLevelAndMoniker(string selectedTeam, out TwinRinksTeamLevel level, out string moniker)
        {
            level = default(TwinRinksTeamLevel);
            moniker = null;

            if (!string.IsNullOrWhiteSpace(selectedTeam))
            {
                var parts = selectedTeam.Trim().Split(' ');

                if (parts.Length == 2)
                {
                    moniker = parts[1];

                    if (Enum.TryParse<TwinRinksTeamLevel>(parts[0], true, out TwinRinksTeamLevel lvl))
                    {
                        level = lvl;

                        return true;
                    }


                }
            }
            return false;
        }

        public static void WriteTeamSnapImportFile(this IEnumerable<TwinRinksEvent> me, TextWriter dest)
        {
            dest.WriteLine("Date,Time,Name,Opponent Name,Opponent Contact Name,Opponent Contact Phone Number,Opponent Contact E-mail Address,Location Name,Location Address,Location URL,Location Details,Home or Away,Uniform,Duration (HH:MM),Arrival Time (Minutes),Extra Label,Notes");

            foreach (var evt in me)
            {
                if (evt.EventType == TwinRinksEventType.Game)
                {
                    var homeOrAway = evt.Rink == TwinRinksRink.Away ? "Away" : "Home";
                    var eventName = $"vs {evt.AwayTeamName}";
                    var extraLabel = evt.Rink == TwinRinksRink.Away ? "" : evt.Rink.ToString() + " Rink";

                    dest.WriteLine($"{evt.EventDate.ToString("MM/dd/yyyy")},{evt.EventStart.ToTeamSnapTime()},{eventName},{evt.AwayTeamName},,,,{evt.Location},,,,{homeOrAway},,01:00,40,{extraLabel},");
                }
                else // practice
                {
                    var extraLabel = evt.Rink == TwinRinksRink.Away ? "" : evt.Rink.ToString() + " Rink";

                    var eventName = evt.IsPowerSkatingEvent() ? "Power Skating" : "Practice";

                    dest.WriteLine($"{evt.EventDate.ToString("MM/dd/yyyy")},{evt.EventStart.ToTeamSnapTime()},{eventName},,,,,{evt.Location},,,,,,01:00,20,{extraLabel},{evt.HomeTeamName} {evt.AwayTeamName}");

                }
            }



        }

        public static IEnumerable<TwinRinksEvent> ParseTwinRinksEvents(this HtmlAgilityPack.HtmlDocument me)
        {
            var rows = me.DocumentNode.SelectNodes("//td");

            int i = 0;

            TwinRinksParsedScheduleItem currItem = new TwinRinksParsedScheduleItem();

            List<TwinRinksParsedScheduleItem> items = new List<TwinRinksParsedScheduleItem>();

            foreach (var r in rows)
            {
                switch (i)
                {
                    case 0:

                        currItem.Date = r.InnerText.Trim();

                        break;

                    case 1:

                        currItem.Day = r.InnerText.Trim();
                        break;

                    case 2:
                        currItem.Rink = r.InnerText.Trim();

                        break;
                    case 3:

                        currItem.Start = r.InnerText.Trim();
                        break;

                    case 4:
                        currItem.End = r.InnerText.Trim();

                        break;

                    case 5:

                        currItem.Location = r.InnerText.Trim();

                        break;
                    case 6:

                        currItem.Description = r.InnerText.Trim();

                        break;

                    case 7:

                        currItem.Home = r.InnerText.Trim();

                        break;

                    case 8:

                        currItem.Away = r.InnerText.Trim();

                        break;
                }


                i++;

                if (i == 9)
                {
                    i = 0;

                    items.Add(currItem);

                    currItem = new TwinRinksParsedScheduleItem();
                }
            }

            return items.Select(x => x.ToEvent());
        }
        private static string ToTeamSnapTime(this TimeSpan me)
        {
            DateTime time = DateTime.Today.Add(me);

            return time.ToString("hh:mm tt");
        }

        public static bool IsPowerSkatingEvent(this TwinRinksEvent evt)
        {
            return evt.HomeTeamName.EndsWith(" POW") || evt.HomeTeamName.EndsWith(" P") || evt.HomeTeamName.EndsWith(" POWER") ||
               evt.AwayTeamName.EndsWith(" POW") || evt.AwayTeamName.EndsWith(" P") || evt.AwayTeamName.EndsWith(" POWER");

        }
        private static TwinRinksEventType ParseEventType(TwinRinksParsedScheduleItem item)
        {
            return item.Description.Contains("Game") ? TwinRinksEventType.Game : TwinRinksEventType.Practice;
        }

        private static TwinRinksRink ParseRink(TwinRinksParsedScheduleItem item)
        {
            if (item.Rink.Equals("Blue"))
                return TwinRinksRink.Blue;
            else if (item.Rink.Equals("Red"))
                return TwinRinksRink.Red;
            else
                return TwinRinksRink.Away;
        }

        private static string ParseLocation(TwinRinksParsedScheduleItem parsed)
        {
            if (parsed.Location.StartsWith("AT "))
                return parsed.Location.Replace("AT ", "");
            else
                return parsed.Location;
        }

        public static IReadOnlyDictionary<TwinRinksTeamLevel, IReadOnlyList<string>> GetTeamMonikers(this IEnumerable<TwinRinksEvent> events)
        {
            Dictionary<TwinRinksTeamLevel, IReadOnlyList<string>> result = new Dictionary<TwinRinksTeamLevel, IReadOnlyList<string>>();

            var games = events.Where(x => x.EventType == TwinRinksEventType.Game);

            foreach (TwinRinksTeamLevel v in Enum.GetValues(typeof(TwinRinksTeamLevel)))
            {
                HashSet<string> monikers = new HashSet<string>();

                foreach (var g in games)
                {
                    var teamLevel = $"{v.ToString().ToUpperInvariant()} ";

                    var teamName = g.HomeTeamName.ToUpperInvariant();

                    if (teamName.StartsWith(teamLevel))
                    {
                        var moniker = teamName.Replace(teamLevel, "");

                        if (!string.IsNullOrWhiteSpace(moniker))
                        {
                            monikers.Add(moniker);
                        }
                    }
                }

                result[v] = monikers.ToArray();
            }

            return result;
        }


        public static string WriteICalFileString(this IEnumerable<TwinRinksEvent> me, string title)
        {
            var calendar = new Calendar();

            calendar.AddProperty("X-WR-CALNAME", title);


            foreach (var e in me)
            {
                calendar.Events.Add(BuildCalendarEvent(e));
            }

            var serializer = new CalendarSerializer();

            return serializer.SerializeToString(calendar);

        }

        private static CalendarEvent BuildCalendarEvent(TwinRinksEvent evt)
        {
            var vEvent = new CalendarEvent();

            vEvent.Location = $"Twin Rinks - {evt.Rink} Rink";
            vEvent.Created = new CalDateTime(DateTime.Now);
            vEvent.Class = "PUBLIC";

            if (evt.EventType == TwinRinksEventType.Game)
            {
                if (evt.Rink == TwinRinksRink.Away)
                {
                    vEvent.Summary = $"Away Game vs {evt.AwayTeamName}@{evt.Location}";
                    vEvent.Location = evt.Location;
                }
                else
                {
                    vEvent.Summary = $"Home Game vs {evt.AwayTeamName}@{evt.Rink} Rink";
                }

            }
            else
            {
                if (evt.IsPowerSkatingEvent())
                    vEvent.Summary = $"Power Skating@{evt.Rink} Rink";

                else
                    vEvent.Summary = $"Practice@{evt.Rink} Rink";
            }

            var startDate = evt.EventDate.Add(evt.EventStart);

            var endDate = evt.EventDate.Add(evt.EventEnd);

            vEvent.Start = new CalDateTime(startDate, "America/Chicago");

            vEvent.End = new CalDateTime(endDate, "America/Chicago");

            return vEvent;

        }
    }

    public class TwinRinksEventConflict
    {

        public TwinRinksEvent FirstEvent { get; set; }
        public TwinRinksEvent SecondEvent { get; set; }
    }
}
