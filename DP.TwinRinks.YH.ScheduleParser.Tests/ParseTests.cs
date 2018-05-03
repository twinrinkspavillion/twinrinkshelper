using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HtmlAgilityPack;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections.Generic;
using DP.TwinRinks.YH.ScheduleParser;
using System.Linq;
using System.Text;
using System.IO;

namespace DP.TwinRinks.YH.ScheduleParser.Tests
{

    [TestClass]
    public class ParseTests
    {
        [TestMethod]
        [DeploymentItem("Samples\\Schedule.html")]
        public void TestTeamSnapExport()
        {
            var doc = new HtmlDocument();

            doc.Load("Schedule.html");

            var trEvents = doc.ParseTwinRinksEvents();

            StringBuilder sb = new StringBuilder();

            using (StringWriter sw = new StringWriter(sb))
            {
                trEvents.FilterTeamEvents(TwinRinksTeamLevel.Squirt, 2).WriteTeamSnapImportFile(sw);

                Debug.Write(sb.ToString());

            }

        }

        [TestMethod]
        [DeploymentItem("Samples\\Schedule.html")]
        public void TestParseTeamMonikers()
        {
            var doc = new HtmlDocument();

            doc.Load("Schedule.html");

            var monikers = doc.ParseTwinRinksEvents().GetTeamMonikers();

        }

        [TestMethod]
        [DeploymentItem("Samples\\Schedule.html")]
        public void TestFindConflicts()
        {
            var doc = new HtmlDocument();

            doc.Load("Schedule.html");

            var gamesOnly = doc.ParseTwinRinksEvents().Where(x => x.EventType == TwinRinksEventType.Game).ToArray();

            var s1 = gamesOnly.FilterTeamEvents(TwinRinksTeamLevel.Squirt, 1);

            var s2 = gamesOnly.FilterTeamEvents(TwinRinksTeamLevel.Squirt, 2);

            var conflicts = s1.FindConflictsWith(s2).ToArray();

            Console.WriteLine(JsonConvert.SerializeObject(conflicts));

        }

        [TestMethod]
        public void TestEventJsonRoundTrip()
        {

            TwinRinksEvent tr = new TwinRinksEvent();
            tr.AwayTeamName = "ICE DOGS";
            tr.HomeTeamName = "SQUIRT 1";
            tr.EventDate = DateTime.Today.AddDays(30);
            tr.EventDescription = "SQUIRT 1 SQUIRT 2";

            tr.EventStart = DateTime.Now.TimeOfDay;
            tr.EventType = TwinRinksEventType.Game;
            tr.Location = "HIGHLAND PARK";
            tr.Rink = TwinRinksRink.Blue;

            var str = JsonConvert.SerializeObject(tr);

            var tr1 = JsonConvert.DeserializeObject<TwinRinksEvent>(str);

            Assert.IsFalse(tr.IsDifferentFrom(tr1, out HashSet<TwinRinksEventField> whichFields));

        }
        [TestMethod]
        public void TestEventJson_DiffTest()
        {

            TwinRinksEvent tr = new TwinRinksEvent();
            tr.AwayTeamName = "ICE DOGS";
            tr.HomeTeamName = "SQUIRT 1";
            tr.EventDate = DateTime.Today.AddDays(30);
            tr.EventDescription = "SQUIRT 1 SQUIRT 2";

            tr.EventStart = DateTime.Now.TimeOfDay;
            tr.EventType = TwinRinksEventType.Game;
            tr.Location = "HIGHLAND PARK";
            tr.Rink = TwinRinksRink.Blue;
            tr.EventEnd = DateTime.Now.TimeOfDay;

            TwinRinksEvent tr1 = new TwinRinksEvent();
            tr1.AwayTeamName = "ICE DOGS 2";
            tr1.HomeTeamName = "SQUIRT 2";
            tr1.EventDate = DateTime.Today.AddDays(31);
            tr1.EventDescription = "SQUIRT 5 SQUIRT 4";

            tr1.EventStart = DateTime.Now.TimeOfDay.Add(TimeSpan.FromDays(1));
            tr1.EventType = TwinRinksEventType.Practice;
            tr1.Location = "HIGHLAND PARK 3";
            tr1.Rink = TwinRinksRink.Away;

            if (tr.IsDifferentFrom(tr1, out HashSet<TwinRinksEventField> whichFields))
            {
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.AwayTeamName));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.EventDate));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.EventDescription));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.EventEnd));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.EventStart));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.EventType));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.HomeTeamName));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.Location));
                Assert.IsTrue(whichFields.Contains(TwinRinksEventField.Rink));
            }

        }
    }
}

