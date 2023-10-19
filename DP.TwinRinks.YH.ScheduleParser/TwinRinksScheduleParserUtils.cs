using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

            if (!me.AwayTeamName.Equals(other.AwayTeamName, StringComparison.InvariantCultureIgnoreCase))
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

            if (!me.HomeTeamName.Equals(other.HomeTeamName, StringComparison.InvariantCultureIgnoreCase))
            {
                res = true;

                whichFields.Add(TwinRinksEventField.HomeTeamName);
            }


            if (!me.Location.Equals(other.Location, StringComparison.InvariantCultureIgnoreCase))
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

        private static string Sha1Hash(string input)
        {
            byte[] hash = (new SHA1Managed()).ComputeHash(Encoding.ASCII.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }

        public static TwinRinksEvent ToEvent(this TwinRinksParsedScheduleItem item)
        {

            TwinRinksEvent tre = new TwinRinksEvent
            {
                AwayTeamName = item.Away,
                HomeTeamName = item.Home,
                Location = ParseLocation(item),
                Rink = ParseRink(item),
                EventType = ParseEventType(item),
                EventDescription = item.Description,
                EventDate = DateTime.Parse(item.Date),
           
                EventStart = DateTime.Parse(item.Start + "M").TimeOfDay
            };


            tre.EventEnd = ParseEndDate(tre.EventStart, item.End);

            return tre;


        }

        public static TimeSpan ParseEndDate(TimeSpan start, string endStr)
        {
            var parts = endStr.Split(' ');

            if (parts.Length == 2)
            {
                int.TryParse(parts[0], out int minutes);

                if(minutes==0)
                    minutes = 60;

                return start + new TimeSpan(0, minutes, 0);
            }
            else
                return start + new TimeSpan(0, 60, 0);
        }

        public static IEnumerable<TwinRinksEvent> FilterTeamEvents(this IEnumerable<TwinRinksEvent> me, TwinRinksTeamLevel level, object teamDesignator)
        {
            string teamName = $"{level.ToString().ToUpperInvariant()} {teamDesignator.ToString().ToUpperInvariant()}";
            string allTeamsName = $"ALL {level.ToString().ToUpperInvariant()}S";
            string levelStr = level.ToString().ToUpperInvariant();

            foreach (TwinRinksEvent e in me)
            {

                if (e.EventType == TwinRinksEventType.Game)
                {
                    if (e.HomeTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return e;
                    }
                    else if (e.AwayTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return e;
                    }
                }
                else // practice
                {
                    bool isPowerSkate = e.IsPowerSkatingEvent();

                    if (e.HomeTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return e;
                    }
                    else if (e.AwayTeamName.Equals(teamName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return e;
                    }
                    else if (e.HomeTeamName.Equals(allTeamsName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return e;
                    }
                    else if (e.AwayTeamName.Equals(allTeamsName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        yield return e;
                    }
                    else if (isPowerSkate && (e.HomeTeamName.ToUpperInvariant().Contains(levelStr) || e.AwayTeamName.ToUpperInvariant().Contains(levelStr) || (string.IsNullOrWhiteSpace(e.AwayTeamName) && string.IsNullOrWhiteSpace(e.HomeTeamName))))
                    {
                        yield return e;
                    }
                }


            }

        }

        public static IEnumerable<TwinRinksEventConflict> FindConflictsWith(this IEnumerable<TwinRinksEvent> me, IEnumerable<TwinRinksEvent> other, double minEventStartHoursDiff = 1.5)
        {
            Dictionary<DateTime, List<TwinRinksEvent>> byDateIndex = new Dictionary<DateTime, List<TwinRinksEvent>>();

            foreach (TwinRinksEvent evt in other)
            {
                if (!byDateIndex.TryGetValue(evt.EventDate, out List<TwinRinksEvent> lst))
                {
                    lst = byDateIndex[evt.EventDate] = new List<TwinRinksEvent>();
                }

                lst.Add(evt);
            }

            foreach (TwinRinksEvent evt in me)
            {
                if (byDateIndex.TryGetValue(evt.EventDate, out List<TwinRinksEvent> lst))
                {
                    foreach (TwinRinksEvent candidate in lst)
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
                string[] parts = selectedTeam.Trim().Split(' ');

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

            foreach (TwinRinksEvent evt in me)
            {
                if (evt.EventType == TwinRinksEventType.Game)
                {
                    string homeOrAway = evt.Rink == TwinRinksRink.Away ? "Away" : "Home";
                    string eventName = $"vs {evt.AwayTeamName}";
                    string extraLabel = evt.Rink == TwinRinksRink.Away ? "" : evt.Rink.ToString() + " Rink";

                    dest.WriteLine($"{evt.EventDate.ToString("MM/dd/yyyy")},{evt.EventStart.ToTeamSnapTime()},{eventName},{evt.AwayTeamName},,,,{evt.Location},,,,{homeOrAway},,01:00,40,{extraLabel},");
                }
                else // practice
                {
                    string extraLabel = evt.Rink == TwinRinksRink.Away ? "" : evt.Rink.ToString() + " Rink";

                    string eventName = evt.IsPowerSkatingEvent() ? "Power Skating" : "Practice";

                    dest.WriteLine($"{evt.EventDate.ToString("MM/dd/yyyy")},{evt.EventStart.ToTeamSnapTime()},{eventName},,,,,{evt.Location},,,,,,01:00,20,{extraLabel},{evt.HomeTeamName} {evt.AwayTeamName}");

                }
            }



        }

        public static IEnumerable<TwinRinksEvent> ParseTwinRinksEvents(this HtmlAgilityPack.HtmlDocument me)
        {
            HtmlAgilityPack.HtmlNodeCollection rows = me.DocumentNode.SelectNodes("//td");

            int i = 0;

            TwinRinksParsedScheduleItem currItem = new TwinRinksParsedScheduleItem();

            List<TwinRinksParsedScheduleItem> items = new List<TwinRinksParsedScheduleItem>();

            foreach (HtmlAgilityPack.HtmlNode r in rows)
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
            return evt.EventDescription.Contains("Hockey Clinics") || evt.HomeTeamName.EndsWith(" POW") || evt.HomeTeamName.EndsWith(" P") || evt.HomeTeamName.EndsWith(" POWER") ||

               evt.AwayTeamName.EndsWith(" POW") || evt.AwayTeamName.EndsWith(" P") || evt.AwayTeamName.EndsWith(" POWER");

        }
        private static TwinRinksEventType ParseEventType(TwinRinksParsedScheduleItem item)
        {
            return item.Description.Contains("Game") ? TwinRinksEventType.Game : TwinRinksEventType.Practice;
        }

        private static TwinRinksRink ParseRink(TwinRinksParsedScheduleItem item)
        {
            if (item.Rink.Equals("Blue"))
            {
                return TwinRinksRink.Blue;
            }
            else if (item.Rink.Equals("Red"))
            {
                return TwinRinksRink.Red;
            }
            else
            {
                return TwinRinksRink.Away;
            }
        }

        private static string ParseLocation(TwinRinksParsedScheduleItem parsed)
        {
            if (parsed.Location.StartsWith("AT "))
            {
                return parsed.Location.Replace("AT ", "");
            }
            else
            {
                return parsed.Location;
            }
        }

        public static IReadOnlyDictionary<TwinRinksTeamLevel, IReadOnlyList<string>> GetTeamMonikers(this IEnumerable<TwinRinksEvent> events)
        {
            Dictionary<TwinRinksTeamLevel, IReadOnlyList<string>> result = new Dictionary<TwinRinksTeamLevel, IReadOnlyList<string>>();

            foreach (TwinRinksTeamLevel v in Enum.GetValues(typeof(TwinRinksTeamLevel)))
            {
                HashSet<string> monikers = new HashSet<string>();

                foreach (TwinRinksEvent e in events)
                {
                    if (e.EventType == TwinRinksEventType.Game)
                    {
                        if (TryParseTeamMonikers(v, e.HomeTeamName, out string teamNameResult))
                        {
                            monikers.Add(teamNameResult);
                        }
                    }
                    else if (e.EventType == TwinRinksEventType.Practice)
                    {
                        if (TryParseTeamMonikers(v, e.HomeTeamName, out string teamNameResult))
                        {
                            monikers.Add(teamNameResult);
                        }

                        if (TryParseTeamMonikers(v, e.AwayTeamName, out string teamNameResult2))
                        {
                            monikers.Add(teamNameResult2);
                        }
                    }
                }

                result[v] = monikers.ToArray();
            }

            return result;
        }

        private static bool TryParseTeamMonikers(TwinRinksTeamLevel v, string fullTeamDescription, out string teamNameResult)
        {
            string teamName = fullTeamDescription.ToUpperInvariant();

            string teamLevel = $"{v.ToString().ToUpperInvariant()} ";

            if (teamName.StartsWith(teamLevel))
            {
                string moniker = teamName.Replace(teamLevel, "");

                if (!string.IsNullOrWhiteSpace(moniker) && !moniker.Equals("POWER", StringComparison.InvariantCultureIgnoreCase))
                {
                    teamNameResult = moniker;
                    return true;
                }
            }

            teamNameResult = null;

            return false;
        }

        public static string WriteICalFileString(this IEnumerable<TwinRinksEvent> me, string title)
        {
            Calendar calendar = new Calendar();

            calendar.AddProperty("X-WR-CALNAME", title);


            foreach (TwinRinksEvent e in me)
            {
                calendar.Events.Add(BuildCalendarEvent(e));
            }

            CalendarSerializer serializer = new CalendarSerializer();

            return serializer.SerializeToString(calendar);

        }

        private static CalendarEvent BuildCalendarEvent(TwinRinksEvent evt)
        {
            CalendarEvent vEvent = new CalendarEvent
            {
                Location = $"Twin Rinks - {evt.Rink} Rink",
                Created = new CalDateTime(DateTime.Now),
                Class = "PUBLIC"
            };

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
                {
                    vEvent.Summary = $"Power Skating@{evt.Rink} Rink";
                }
                else
                {
                    vEvent.Summary = $"Practice@{evt.Rink} Rink";
                }
            }

            DateTime startDate = evt.EventDate.Add(evt.EventStart);

            DateTime endDate = evt.EventDate.Add(evt.EventEnd);

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

    public class TwinRinksEventKey : IEquatable<TwinRinksEventKey>
    {
        public TwinRinksEventKey(TwinRinksEvent evt)
        {
            if (evt == null)
            {
                throw new ArgumentNullException(nameof(evt));
            }

            EventStartDate = evt.EventDate + evt.EventStart;
            EventType = evt.EventType;
            Location = evt.Location;

        }
        public TwinRinksEventKey(DateTime eventStartDate, TwinRinksEventType eventType, string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("message", nameof(location));
            }

            EventStartDate = eventStartDate;
            EventType = eventType;
            Location = location;
        }

        public DateTime EventStartDate { get; }
        public TwinRinksEventType EventType { get; }
        public string Location { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as TwinRinksEventKey);
        }

        public bool Equals(TwinRinksEventKey other)
        {
            return other != null &&
                   EventStartDate == other.EventStartDate &&
                   EventType == other.EventType &&
                   Location == other.Location;
        }

        public override int GetHashCode()
        {
            int hashCode = -2103952962;
            hashCode = hashCode * -1521134295 + EventStartDate.GetHashCode();
            hashCode = hashCode * -1521134295 + EventType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Location);
            return hashCode;
        }
    }
}
