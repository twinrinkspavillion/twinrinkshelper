using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DP.TwinRinksHelper.Web.Pages.ScheduleSyncSpec
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly DP.TwinRinksHelper.Web.Models.TwinRinksHelperContext _context;

        public IndexModel(DP.TwinRinksHelper.Web.Models.TwinRinksHelperContext context)
        {
            _context = context;
        }

        public IList<DP.TwinRinksHelper.Web.Models.ScheduleSyncSpec> ScheduleSyncSpec { get; set; }

        public async Task OnGetAsync()
        {
            ScheduleSyncSpec = (await _context.ScheduleSyncSpecs.ToListAsync()).Where(x => x.TeamSnapUserId == User.GetTeamSnapUserId()).ToList();
        }
    }
}
