using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DP.TwinRinksHelperWeb.Controllers
{
    [Produces("text/calendar")]
    [Route("api/ICS")]
    public class ICSController : Controller
    {
        private readonly TwinRinksScheduleParserService _twinRinksService;

        public ICSController(TwinRinksScheduleParserService twinRinksService)
        {
            this._twinRinksService = twinRinksService;
        }

        public IActionResult Get(string team)
        {
            team = team.Replace("_", " ");

            byte[] file = _twinRinksService.GetICalFile(team,null);

            if (file != null)
                return this.File(file, "text/calendar", $"{team.Replace(" ", "_")}.ics");
            else
                return this.NotFound();
        }
    }
}