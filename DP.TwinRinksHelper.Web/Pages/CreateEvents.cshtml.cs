using DP.TwinRinks.YH.ScheduleParser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DP.TwinRinksHelper.Web.Pages
{
    [Authorize]
    public class CreateEventsModel : PageModel
    {
        private readonly TeamSnapApi tsApi;
        private readonly TwinRinksScheduleParserService trApi;

        public CreateEventsResult Result { get; } = new CreateEventsResult();
        public CreateEventsModel(TeamSnapApi tsApi, TwinRinksScheduleParserService trApi)
        {
            this.tsApi = tsApi ?? throw new System.ArgumentNullException(nameof(tsApi));
            this.trApi = trApi ?? throw new System.ArgumentNullException(nameof(trApi));
        }

        public async Task<ActionResult> OnPost(string SelectedEvents, long? SelectedTeamSnapTeamId, string SelectedTwinRinksTeam)
        {
            if (string.IsNullOrWhiteSpace(SelectedEvents) || !SelectedTeamSnapTeamId.HasValue || string.IsNullOrWhiteSpace(SelectedTwinRinksTeam))
            {
                return new BadRequestResult();
            }

            HashSet<TwinRinksEventKey> filters = ParseSelectedEventsString(SelectedEvents);

            IEnumerable<TwinRinksEvent> eventsToCreate = trApi.GetEvents(SelectedTwinRinksTeam).Where(x => filters.Contains(new TwinRinksEventKey(x)));

            IEnumerable<TeamSnapApi.CreateEventRequest> createReqs = eventsToCreate.Select(x => x.ToCreateTeamSnapEventRequest(SelectedTeamSnapTeamId.Value));

            await tsApi.CreateEvents(createReqs);

            Result.CountCreated = createReqs.Count();

            Result.TeamName = (await tsApi.GetTeamAsync(SelectedTeamSnapTeamId.Value)).Name;
        
            return new PageResult();
        }

        private static HashSet<TwinRinksEventKey> ParseSelectedEventsString(string selectedEvents)
        {
            HashSet<TwinRinksEventKey> res = new HashSet<TwinRinksEventKey>();

            string[] events = selectedEvents.Split('|');

            foreach (string e in events)
            {
                string[] parts = e.Split('_');

                DateTime eventTime = new DateTime(long.Parse(parts[0]));

                Enum.TryParse<TwinRinksEventType>(parts[1], out TwinRinksEventType et);

                string loc = parts[2];

                res.Add(new TwinRinksEventKey(eventTime, et, loc));

            }

            return res;
        }

        public class CreateEventsResult
        {
            public int CountCreated { get; internal set; }
            public string TeamName { get; internal set; }
        }

    }
}