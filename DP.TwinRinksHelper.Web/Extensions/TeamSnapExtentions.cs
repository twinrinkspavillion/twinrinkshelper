using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;


public static class TeamSnapExtentions
{
    public const string TeamSnapBearerTokenClaimName = "urn:teamsnap-bearer-token";
    public static IServiceCollection AddTeamSnapOauth(this IServiceCollection me, IConfiguration configuration)
    {
        me.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        me.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "TeamSnap";
        })
       .AddCookie()
       .AddOAuth("TeamSnap", options =>
             {


                 options.ClientId = Environment.GetEnvironmentVariable("TeamSnap:ClientId");
                 options.ClientSecret = Environment.GetEnvironmentVariable("TeamSnap:ClientSecret");


                 options.CallbackPath = new PathString("/signin-teamsnap");
                 options.Scope.Add("read");
                 options.Scope.Add("write");

                 options.AuthorizationEndpoint = "https://auth.teamsnap.com/oauth/authorize";
                 options.TokenEndpoint = "https://auth.teamsnap.com/oauth/token";
                 options.UserInformationEndpoint = "https://api.teamsnap.com/v3/me";

                 options.ClaimActions.MapCustomJson(ClaimTypes.Name, (u) =>
                 {
                     var data = ((JArray)u["collection"]["items"])[0]["data"] as JArray; //get email address

                     return data[5]["value"].Value<string>();

                 });

                 options.ClaimActions.MapCustomJson(ClaimTypes.NameIdentifier, (u) =>
                 {

                     var data = ((JArray)u["collection"]["items"])[0]["data"] as JArray; /// get id

                     return data[0]["value"].Value<string>();

                 });

                 options.Events = new OAuthEvents
                 {
                     OnCreatingTicket = async context =>
                     {
                         var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                         request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                         var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);

                         response.EnsureSuccessStatusCode();

                         var user = JObject.Parse(await response.Content.ReadAsStringAsync());



                         context.Principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(TeamSnapBearerTokenClaimName, context.AccessToken) }));

                         context.RunClaimActions(user);
                     }

                 };
             });

        me.AddTransient<TeamSnapApi>(sp =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>();

            if (ctx.HttpContext == null || ctx.HttpContext.User == null)
                throw new Exception("No HttpContext or HttpContext.User was found!");

            var tokenClaim = ctx.HttpContext.User.FindFirst(TeamSnapBearerTokenClaimName) ?? throw new Exception($"Could not find following claim in HttpContext.User: {TeamSnapBearerTokenClaimName}!");

            return new TeamSnapApi(tokenClaim.Value);
        });

        return me;
    }


    public static int GetTeamSnapUserId(this ClaimsPrincipal me)
    {
        return int.Parse(me.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

    }
}

