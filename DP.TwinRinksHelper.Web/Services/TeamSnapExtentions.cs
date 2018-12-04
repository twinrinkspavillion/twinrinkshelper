using DP.TwinRinks.YH.ScheduleParser;
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
                     JArray data = ((JArray)u["collection"]["items"])[0]["data"] as JArray; //get email address

                     return data[5]["value"].Value<string>();

                 });

                 options.ClaimActions.MapCustomJson(ClaimTypes.NameIdentifier, (u) =>
                 {

                     JArray data = ((JArray)u["collection"]["items"])[0]["data"] as JArray; /// get id

                     return data[0]["value"].Value<string>();

                 });

                 options.Events = new OAuthEvents
                 {
                     OnCreatingTicket = async context =>
                     {
                         HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                         request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                         HttpResponseMessage response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);

                         response.EnsureSuccessStatusCode();

                         JObject user = JObject.Parse(await response.Content.ReadAsStringAsync());

                         context.Principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(TeamSnapBearerTokenClaimName, context.AccessToken) }));

                         context.RunClaimActions(user);
                     }

                 };
             });

        me.AddTransient<TeamSnapApi>(sp =>
        {
            IHttpContextAccessor ctx = sp.GetRequiredService<IHttpContextAccessor>();

            if (ctx.HttpContext == null || ctx.HttpContext.User == null)
            {
                throw new Exception("No HttpContext or HttpContext.User was found!");
            }

            string bearer = ctx.HttpContext.GetCurrentTeamSnapBearerToken();

            return new TeamSnapApi(bearer);
        });

        return me;
    }

    public static string GetCurrentTeamSnapBearerToken(this HttpContext ctx)
    {
        return ctx.User.FindFirst(TeamSnapBearerTokenClaimName).Value ?? throw new Exception($"Could not find following claim in HttpContext.User: {TeamSnapBearerTokenClaimName}!");
    }

    public static int GetTeamSnapUserId(this ClaimsPrincipal me)
    {
        return int.Parse(me.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

    }


    public static TeamSnapApi.CreateEventRequest ToCreateTeamSnapEventRequest(this TwinRinksEvent evt, long teamId, bool notifyTeam = true)
    {
        TeamSnapApi.CreateEventRequest res = new TeamSnapApi.CreateEventRequest
        {
            NotifyTeam = notifyTeam,
            TeamId = teamId,

            IsGame = evt.EventType == TwinRinksEventType.Game,
            LocationName = evt.Location
        };

        if (evt.Rink != TwinRinksRink.Away)
        {
            res.LocationDetails = $"{evt.Rink} Rink";
        }
        else
        {
            res.LocationDetails = $"AWAY";
        }

        if (res.IsGame)
        {
            res.OpponentName = evt.AwayTeamName;

            res.ArriveEarlyMinutes = 60;
        }
        else
        {
            res.ArriveEarlyMinutes = 30;

            res.Name = "Practice";

            if (evt.IsPowerSkatingEvent())
            {
                res.Label = "Power Skate";

            }
        }

        res.Notes = evt.EventDescription;

        res.DurationMinutes = 60;
        res.StartDate = evt.EventDate + evt.EventStart;


        return res;
    }
}

