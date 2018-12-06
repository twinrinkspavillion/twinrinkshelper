using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DP.TwinRinksHelper.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace DP.TwinRinksHelper.Web.Pages.ScheduleSyncSpec
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly DP.TwinRinksHelper.Web.Models.TwinRinksHelperContext _context;

        public DeleteModel(DP.TwinRinksHelper.Web.Models.TwinRinksHelperContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DP.TwinRinksHelper.Web.Models.ScheduleSyncSpec ScheduleSyncSpec { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ScheduleSyncSpec = await _context.ScheduleSyncSpecs.FirstOrDefaultAsync(m => m.ID == id);

            if (ScheduleSyncSpec == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ScheduleSyncSpec = await _context.ScheduleSyncSpecs.FindAsync(id);

            if (ScheduleSyncSpec != null)
            {
                _context.ScheduleSyncSpecs.Remove(ScheduleSyncSpec);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
