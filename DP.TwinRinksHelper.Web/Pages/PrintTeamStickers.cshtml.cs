using DP.TwinRinksHelper.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace DP.TwinRinksHelper.Web.Pages
{
    [Authorize]
    public class PrintTeamStickers : PageModel
    {
        private readonly TeamStickerPrintingService _teamStickerService;
        private readonly TeamSnapApi tsApi;
        public PrintTeamStickers(TeamSnapApi tsApi, TeamStickerPrintingService ts)
        {
            _teamStickerService = ts ?? throw new System.ArgumentNullException(nameof(ts));
            this.tsApi = tsApi ?? throw new System.ArgumentNullException(nameof(tsApi));
        }

        [BindProperty(SupportsGet = true)]
        public long? SelectedTeamSnapTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? SelectedTeamSnapCoachId { get; set; }

        [BindProperty(SupportsGet = true)]
        public long[] SelectedMemberId { get; set; }

        public IEnumerable<TeamSnapApi.Team> TeamSnapTeams => tsApi.GetActiveTeamsForUser(User.GetTeamSnapUserId()).Result;
        [NonHandler]
        public IEnumerable<SelectListItem> GetTeamSnapTeamsItems()
        {
            return TeamSnapTeams.Select(x => new SelectListItem() { Value = x.Id.ToString(), Text = x.Name });
        }
        [NonHandler]
        public IEnumerable<SelectListItem> GetTeamSnapTeamPlayers()
        {
            if (SelectedTeamSnapTeamId.HasValue)
            {
                IEnumerable<TeamSnapApi.TeamMember> members = GetMembers().Where(x => !x.IsNonPlayer);

                return members.OrderBy(x=>x.LastName).Select(x => new SelectListItem() { Value = x.MemberId.ToString(), Text = $"{x.JerseyNumber} {x.FirstName} {x.LastName}" });

            }
            return new SelectListItem[0];
        }
        [NonHandler]
        public IEnumerable<SelectListItem> GetTeamSnapNonPlayngTeamMembers()
        {
            if (SelectedTeamSnapTeamId.HasValue)
            {
                IEnumerable<TeamSnapApi.TeamMember> members = GetMembers().Where(x => x.IsNonPlayer);

                return (new SelectListItem[] { new SelectListItem() { Value = "0", Text = $"No Coach" } }).Union(members.Select(x => new SelectListItem() { Value = x.MemberId.ToString(), Text = $"{x.FirstName} {x.LastName}" }));

            }
            return new SelectListItem[0];
        }

        private IEnumerable<TeamSnapApi.TeamMember> GetMembers()
        {
            return tsApi.GetTeamMembers(SelectedTeamSnapTeamId.Value).Result;

        }


        public ActionResult OnPostDownload()
        {
            if (SelectedTeamSnapTeamId.HasValue && SelectedTeamSnapCoachId.HasValue && SelectedMemberId != null)
            {

                HashSet<long> members = new HashSet<long>(SelectedMemberId);

                string teamname = TeamSnapTeams.Where(x => x.Id == SelectedTeamSnapTeamId.Value).FirstOrDefault()?.Name;

                TeamSnapApi.TeamMember coach = GetMembers().Where(x => x.MemberId == SelectedTeamSnapCoachId.Value).FirstOrDefault();

                List<TeamStickerPrintingService.TeamStickerDescriptor.PlayerDescriptor> players = new List<TeamStickerPrintingService.TeamStickerDescriptor.PlayerDescriptor>();

                foreach (TeamSnapApi.TeamMember plr in GetMembers().Where(x => members.Contains(x.MemberId)).OrderBy(x=>x.LastName))
                {
                    TeamStickerPrintingService.TeamStickerDescriptor.PlayerDescriptor p = new TeamStickerPrintingService.TeamStickerDescriptor.PlayerDescriptor
                    {
                        PlayerName = $"{plr.FirstName} {plr.LastName}",
                        PlayerNumber = plr.JerseyNumber
                    };

                    players.Add(p);
                }

                TeamStickerPrintingService.TeamStickerDescriptor teamDescr = new TeamStickerPrintingService.TeamStickerDescriptor() { TeamName = teamname, CoachName = $"{coach?.FirstName} {coach?.LastName}", CoachPhone = $"{coach?.PrimaryPhoneNumber}", Players = players };
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{teamDescr.TeamName} Stickers.pdf\"");
                return File(_teamStickerService.CreateTeamLabelsPDFReport(teamDescr), "application/pdf");

            }

            return new PageResult();
        }
        public void OnPost()
        {
            EnsureDefaultTeamSelection();

        }

        private void EnsureDefaultTeamSelection()
        {
            if (SelectedTeamSnapTeamId == null)
            {
                SelectedTeamSnapTeamId = tsApi.GetActiveTeamsForUser(User.GetTeamSnapUserId()).Result.FirstOrDefault()?.Id;
            }
        }

        public void OnGet()
        {
            EnsureDefaultTeamSelection();

        }
    }
}