
using Alexa.NET.Request;
using Alexa.NET.Response;
using DP.TwinRinksHelper.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Text;

namespace DP.TwinRinksHelper.Web.Controllers
{


    public class AlexaScheduleSkillController : Controller
    {
        private readonly AlexaTwinRinksSkillRequestProcessor _service;
        private readonly SpeechletRequestSignatureVerifier _reqVerifier;

        public AlexaScheduleSkillController(AlexaTwinRinksSkillRequestProcessor service, SpeechletRequestSignatureVerifier verifier)
        {
            _service = service;
            _reqVerifier = verifier;
        }

        [Route("api/AlexaScheduleSkill")]
        [HttpPost]
        public ActionResult<SkillResponse> Post([FromBody]SkillRequest request)
        {
            string chainUrl = null;

            if (Request.Headers.Keys.Where(x => x.Equals(SpeechletRequestSignatureVerifier.Sdk.SIGNATURE_CERT_URL_REQUEST_HEADER, System.StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                chainUrl = Request.Headers.Where(x => x.Key.Equals((SpeechletRequestSignatureVerifier.Sdk.SIGNATURE_CERT_URL_REQUEST_HEADER), System.StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value.FirstOrDefault(y => !string.IsNullOrWhiteSpace(y))).FirstOrDefault();
            }

            string signature = null;

            if (Request.Headers.Keys.Where(x => x.Equals(SpeechletRequestSignatureVerifier.Sdk.SIGNATURE_REQUEST_HEADER, System.StringComparison.InvariantCultureIgnoreCase)).Any())
            {
                signature = Request.Headers.Where(x => x.Key.Equals((SpeechletRequestSignatureVerifier.Sdk.SIGNATURE_REQUEST_HEADER), System.StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value.FirstOrDefault(y => !string.IsNullOrWhiteSpace(y))).FirstOrDefault();
            }

            Request.Body.Position = 0;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                Request.Body.CopyTo(memoryStream);

                byte[] alexaBytes = memoryStream.ToArray();

                if (!_reqVerifier.VerifyRequestSignature(alexaBytes, signature, chainUrl))
                {
                    return BadRequest();
                }
            }

            return new ActionResult<SkillResponse>(_service.ProcessRequest(request));
        }
    }
}