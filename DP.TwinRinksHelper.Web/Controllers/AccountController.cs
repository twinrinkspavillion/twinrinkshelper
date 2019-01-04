using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
            var redirectUrl = Url.Page("/Index");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl }   
            );
        }


        [HttpGet]
        public IActionResult SignOut()
        {

            this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();

            var appUrl = System.Net.WebUtility.UrlEncode($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}");

            var callbackUrl = $"https://auth.teamsnap.com/logout?redirect_uri={appUrl}";

            return SignOut(
                new AuthenticationProperties {  RedirectUri = callbackUrl }
            );
        }
    }
}
