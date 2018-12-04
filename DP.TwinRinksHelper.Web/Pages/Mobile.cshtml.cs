using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DP.TwinRinks.YH.ScheduleParser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DP.TwinRinksHelper.Web.Pages
{
    public class MobileSchedule : PageModel
    {
        private readonly TwinRinksScheduleParserService _twinRinksService;

        public MobileSchedule(TwinRinksScheduleParserService twinRinksService)
        {
            this._twinRinksService = twinRinksService;
        }

        public SelectList Teams { get; private set; }

        public string SelectedTeam { get; set; }
        public IEnumerable<TwinRinksEvent> Events { get; private set; }
        public void OnGet(string SelectedTeam)
        {
            SelectedTeam = EnsureCookie(SelectedTeam);

            Teams = new SelectList(new[] { "Select Team" }.Union(_twinRinksService.GetTeamsList()).ToArray());

            this.SelectedTeam = SelectedTeam;

            Events = _twinRinksService.GetEvents(SelectedTeam);
        }

        private string EnsureCookie(string selectedTeam)
        {
            if(string.IsNullOrWhiteSpace(selectedTeam))
            {
                var cookieValue = Request.Cookies["SelectedTeam"];

                if (!string.IsNullOrWhiteSpace(cookieValue))
                    return cookieValue;
                else
                    return selectedTeam;
            }
            else
            {
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddMonths(6)
                };
                Response.Cookies.Append("SelectedTeam", selectedTeam, cookieOptions);

                return selectedTeam;
            }

         
        
        }
    }
}
