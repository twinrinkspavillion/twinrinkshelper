using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DP.TwinRinksHelper.Web.Pages.ScheduleSyncSpec
{
    [Authorize]
    public class CreateExclusion : PageModel
    {

        private readonly Models.TwinRinksHelperContext _dbContext;

        public CreateExclusion(Models.TwinRinksHelperContext dbContext)
        {

            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        [BindProperty(SupportsGet = true)]
        public int SpecID { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public async Task OnGet()
        {
            Models.ScheduleSyncSpec spec = _dbContext.ScheduleSyncSpecs.Where(x => x.ID == SpecID && x.TeamSnapUserId == User.GetTeamSnapUserId()).FirstOrDefault();

            if (spec == null)
            { 
                Message = $"Could not create exlusion!";
            }
            else
            {
                var exists = _dbContext.ScheduleSyncSpecExclusions.Where(x => x.ScheduleSyncSpecID == spec.ID && x.ExcludedDate == this.Date).Any();

                if(!exists)
                {
                    _dbContext.ScheduleSyncSpecExclusions.Add(new Models.ScheduleSyncExclusion() { ScheduleSyncSpecID = spec.ID, ExcludedDate = this.Date });

                    _dbContext.SaveChanges();
                }

                Message = $"Created exclusion for {Date.ToShortDateString()}!";
            }

        }
    }
}