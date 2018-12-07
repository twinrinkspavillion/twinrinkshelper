using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;
namespace DP.TwinRinksHelper.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/ScheduleCheck")]
    public class ScheduleCheckController : Controller
    {


        private readonly TwinRinksScheduleParserService parser;
        private readonly Models.TwinRinksHelperContext _dbContext;

        public ScheduleCheckController(TwinRinksScheduleParserService parser, Models.TwinRinksHelperContext dbContext)
        {

            this.parser = parser ?? throw new System.ArgumentNullException(nameof(parser));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }



        [HttpGet]
        public async Task<ScheduleCheckResult> Get()
        {
            foreach (Models.ScheduleSyncSpec s in _dbContext.ScheduleSyncSpecs.ToArray())
            {
                try
                {
                    if(s.Expires > DateTime.Today)
                    {
                        _dbContext.Remove(s);

                        continue;
                    }
                    
                    TeamSnapApi api = new TeamSnapApi(s.TeamSnapToken);

                    System.Collections.Generic.IEnumerable<ScheduleComparer.CompareResult> res = await new ScheduleComparer(api, parser, s.TeamSnapTeamId, s.TwinRinkTeamName).RunCompareAsync();

                    ///send email
                    ///

                   ///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
                   ///
                   ///send grid

                }
                catch (Exception)
                {

                }
            }

            await _dbContext.SaveChangesAsync();


            return new ScheduleCheckResult();
        }

        public class ScheduleCheckResult
        {

        }

    }
}