﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DP.TwinRinksHelper.Web.Pages
{
    [Authorize]
    public class CompareScheduleModel : PageModel
    {
        private readonly TeamSnapApi tsApi;
        private readonly TwinRinksScheduleParserService parser;


        public CompareScheduleModel(TeamSnapApi tsApi, TwinRinksScheduleParserService parser)
        {
            this.tsApi = tsApi ?? throw new System.ArgumentNullException(nameof(tsApi));
            this.parser = parser ?? throw new System.ArgumentNullException(nameof(parser));
        }
        public IEnumerable<ScheduleComparer.CompareResult> CompareResults { get; private set; }
        [BindProperty(SupportsGet =true)]
        public long SelectedTeamSnapTeamId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string SelectedTwinRinksTeam { get; set; }
        public IEnumerable<TeamSnapApi.Team> TeamSnapTeams => tsApi.GetActiveTeamsForUser(User.GetTeamSnapUserId()).Result;
        public IEnumerable<SelectListItem> GetTeamSnapTeamsItems()
        {
            return TeamSnapTeams.Select(x => new SelectListItem() { Value = x.Id.ToString(), Text = x.Name });
        }
        public IEnumerable<SelectListItem> GetTwinRinksTeamsItems()
        {
            return parser.GetTeamsList().Select(x => new SelectListItem() { Value = x.ToString(), Text = x.ToString() });
        }

        public async Task OnGet()
        {
            if(!string.IsNullOrWhiteSpace(SelectedTwinRinksTeam))
            {
                CompareResults = await new ScheduleComparer(tsApi, parser, SelectedTeamSnapTeamId, SelectedTwinRinksTeam).RunCompareAsync();
            }
        }

        public async Task OnPost()
        {
         
            CompareResults = await new ScheduleComparer(tsApi, parser, SelectedTeamSnapTeamId, SelectedTwinRinksTeam).RunCompareAsync();

       }
    }
}