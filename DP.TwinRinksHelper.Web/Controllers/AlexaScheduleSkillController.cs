
using Alexa.NET.Request;
using Alexa.NET.Response;
using DP.TwinRinksHelper.Web.Services;
using Microsoft.AspNetCore.Mvc;

using System.IO;
using System.Linq;

namespace DP.TwinRinksHelper.Web.Controllers
{


    public class AlexaScheduleSkillController : Controller
    {
        private readonly AlexaTwinRinksSkillRequestProcessor _service;

        public AlexaScheduleSkillController(AlexaTwinRinksSkillRequestProcessor service)
        {
            _service = service;
        }

        [Route("api/AlexaScheduleSkill")]
        [HttpPost]
        public SkillResponse Post([FromBody]SkillRequest request)
        {
            return _service.ProcessRequest(request);
        }
    }
}