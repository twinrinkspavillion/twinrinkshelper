using DP.TwinRinksHelper.Web.Models;
using Microsoft.AspNetCore.Mvc;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP.TwinRinksHelper.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/ScheduleCheck")]
    public class ScheduleCheckController : Controller
    {


        private readonly TwinRinksScheduleParserService _parser;
        private readonly Models.TwinRinksHelperContext _dbContext;
        private readonly SendGrid.ISendGridClient _sndGrdClient;

        public ScheduleCheckController(TwinRinksScheduleParserService parser, Models.TwinRinksHelperContext dbContext, SendGrid.ISendGridClient sndGrdClient)
        {

            _parser = parser ?? throw new System.ArgumentNullException(nameof(parser));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _sndGrdClient = sndGrdClient;
        }



        [HttpGet]
        public async Task<ScheduleCheckResult> Get()
        {
            foreach (Models.ScheduleSyncSpec s in _dbContext.ScheduleSyncSpecs.ToArray())
            {
                try
                {
                    if (s.Expires < DateTime.Today)
                    {
                        _dbContext.Remove(s);

                        continue;
                    }

                    TeamSnapApi api = new TeamSnapApi(s.TeamSnapToken);

                    HashSet<DateTime> exclusions = new HashSet<DateTime>(_dbContext.ScheduleSyncSpecExclusions.Where(x=>x.ScheduleSyncSpecID == s.ID).Select(x=>x.ExcludedDate));

                    IEnumerable<ScheduleComparer.CompareResult> res = await new ScheduleComparer(api, _parser, s.TeamSnapTeamId, s.TwinRinkTeamName, exclusions).RunCompareAsync();

                    if (res.Any())
                    {
                        await BuildAndSendEmail(s, res);
                    }

                    s.LastChecked = DateTime.UtcNow;
                }
                catch (Exception)
                {


                }
            }

            await _dbContext.SaveChangesAsync();

            return new ScheduleCheckResult();
        }

        private async Task BuildAndSendEmail(ScheduleSyncSpec s, IEnumerable<ScheduleComparer.CompareResult> res)
        {
            EmailAddress from = new EmailAddress("twinrinkspavillion@gmail.com", "Twin Rinks Schedule Checker");

            string subject = $"[{s.TeamSnapTeamName}] {res.Count()} Schedule differences found vs [{s.TwinRinkTeamName}]@TwinRinks.com";

            EmailAddress to = new EmailAddress(s.Recipients); //TODO: split by comma

            StringBuilder mailBuider = new StringBuilder();

            string host = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

            mailBuider.Append($"<p><a href='{host}/CompareSchedule?SelectedTeamSnapTeamId={s.TeamSnapTeamId}&SelectedTwinRinksTeam={s.TwinRinkTeamName}'>Go to Site </a></p>");

            mailBuider.Append(HtmlHelperExtentions.ToHtmlTable(null, res, "id", border: 0, actionGenerator: (t) => $"<a href='{host + "/ScheduleSyncSpec/CreateExclusion?Date=" + t.TR_EventTime.Value.ToShortDateString() ?? ScheduleComparer.ToCSTTime(t.TS_EventTime.Value).ToShortDateString()}&SpecID={s.ID}'>Mute</a>"));

            string htmlContent = mailBuider.ToString();

            SendGridMessage msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);

            SendGrid.Response response = await _sndGrdClient.SendEmailAsync(msg);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception(response.StatusCode.ToString());
            }
        }

        public class ScheduleCheckResult
        {

        }

    }
}