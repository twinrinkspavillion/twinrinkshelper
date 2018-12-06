using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DP.TwinRinksHelper.Web.Pages.ScheduleSyncSpec
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly TeamSnapApi tsApi;
        private readonly TwinRinksScheduleParserService parser;
        private readonly Models.TwinRinksHelperContext _dbContext;

        public CreateModel(TeamSnapApi tsApi, TwinRinksScheduleParserService parser, Models.TwinRinksHelperContext dbContext)
        {
            this.tsApi = tsApi ?? throw new System.ArgumentNullException(nameof(tsApi));
            this.parser = parser ?? throw new System.ArgumentNullException(nameof(parser));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        public long SelectedTeamSnapTeamId { get; set; }
        public string SelectedTwinRinksTeam { get; set; }
        public DateTime SelectedExpiresDate { get; set; }
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
            SelectedExpiresDate = DateTime.Today.AddMonths(6);
        }

        public async Task<IActionResult> OnPost(long SelectedTeamSnapTeamId, string SelectedTwinRinksTeam, DateTime SelectedExpiresDate)
        {
            Models.ScheduleSyncSpec spec = new Models.ScheduleSyncSpec();

            spec.CreatedDate = DateTime.UtcNow;
            spec.LastUpdated = DateTime.UtcNow;
            spec.TeamSnapTeamId = SelectedTeamSnapTeamId;
            spec.TeamSnapUserId = User.GetTeamSnapUserId();
            spec.TeamSnapToken = this.HttpContext.GetCurrentTeamSnapBearerToken();
            spec.TwinRinkTeamName = SelectedTwinRinksTeam;
            spec.Recipients = this.User.GetTeamSnapUserEmail();

            var team = await tsApi.GetTeamAsync(SelectedTeamSnapTeamId);

            spec.TeamSnapTeamName = team.Name;

            spec.Expires = SelectedExpiresDate;

            _dbContext.Add(spec);

            await _dbContext.SaveChangesAsync();

            return Redirect("/ScheduleSyncSpec");
        }
    }
}