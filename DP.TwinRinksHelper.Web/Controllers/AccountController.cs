using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace DP.TwinRinksHelper.Web.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        public AccountController()
        {

        }

        [HttpGet]
        public IActionResult SignIn()
        {
            string redirectUrl = Url.Page("/Index");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl }
            );
        }


        [HttpGet]
        public IActionResult SignOut()
        {

            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();

            var appUrl = System.Net.WebUtility.UrlEncode($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}");

            var callbackUrl = $"https://auth.teamsnap.com/logout?redirect_uri={appUrl}";

            return Redirect(callbackUrl);
        }
    }
}
