using System;
using System.Collections.Generic;
using System.Linq;
using DP.TwinRinks.YH.ScheduleParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DP.TwinRinksHelper.Web.Pages
{
    public class Conflicts : PageModel
    {
        private readonly TwinRinksScheduleParserService _twinRinksService;

        public Conflicts(TwinRinksScheduleParserService twinRinksService)
        {
            this._twinRinksService = twinRinksService;
        }

        public SelectList Teams { get; private set; }

        public string SelectedTeam1 { get; set; }
        public string SelectedTeam2 { get; set; }
        public IEnumerable<TwinRinksEventConflict> ConflictingGames { get; private set; }
        public void OnGet(string SelectedTeam1, string SelectedTeam2) 
        {
            Teams = new SelectList(new[] { "Select Team" }.Union(_twinRinksService.GetTeamsList()).ToArray());

            this.SelectedTeam1 = SelectedTeam1;
            this.SelectedTeam2 = SelectedTeam2;

            this.ConflictingGames = this._twinRinksService.GetGameConficts(SelectedTeam1, SelectedTeam2);
        }





    }
}
