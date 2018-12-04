
using DP.TwinRinks.YH.ScheduleParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DP.TwinRinksHelper.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TwinRinksScheduleParserService _twinRinksService;

        public IndexModel(TwinRinksScheduleParserService twinRinksService)
        {
            _twinRinksService = twinRinksService;
        }

        public SelectList Teams { get; private set; }

        public string SelectedTeam { get; set; }
        public IEnumerable<TwinRinksEvent> Events { get; private set; }
        public void OnGet(string SelectedTeam)
        {
            Teams = new SelectList(new[] { "Select Team" }.Union(_twinRinksService.GetTeamsList()).ToArray());
            this.SelectedTeam = SelectedTeam;
            Events = _twinRinksService.GetEvents(SelectedTeam);
        }


        public IActionResult OnGetExportTeamSnap(string team, string dates)
        {
            byte[] file = _twinRinksService.GetTeamSnapScheduleImportFile(team, ParseDatesList(dates));

            if (file != null)
            {
                return File(file, "text/csv", $"{team.Replace(" ", "_")}_TeamSnap_Import.csv");
            }
            else
            {
                return RedirectToPage();
            }
        }

        private static IEnumerable<DateTime> ParseDatesList(string dates)
        {
            if (!string.IsNullOrWhiteSpace(dates))
            {
                return dates.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => DateTime.ParseExact(x.Trim(), "yyyy-MM-dd", null));
            }
            else
            {
                return Enumerable.Empty<DateTime>();
            }
        }
        public IActionResult OnGetICalFile(string team, string dates)
        {
            byte[] file = _twinRinksService.GetICalFile(team, ParseDatesList(dates));

            if (file != null)
            {
                return File(file, "text/calendar", $"{team.Replace(" ", "_")}.ics");
            }
            else
            {
                return RedirectToPage(new { SelectedTeam = team });
            }
        }
    }
}
