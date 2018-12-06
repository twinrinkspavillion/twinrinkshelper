using CollectionJson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;


public class TeamSnapApi : IDisposable
{
    private HttpClient _httpClient;
    public string BaseAddress { get; set; } = "https://apiv3.teamsnap.com/v3/";

    private const string ActiveTeamsQueryTemplate = "teams/active?user_id={0}";
    private const string TeamByIdQueryTemplate = "teams/{0}";
    private const string TeamEventQueryTemplate = "events/search?team_id={0}";
    private const string TeamOwnerQueryTemplate = "/members/owner?team_id={0}";
    private const string MeQuery = "/me";
    private const string FindTeamMemberIdByEmailTemplate = "member_email_addresses/search?email={0}&team_id={1}";
    public TeamSnapApi(string bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            throw new ArgumentException("must provide bearer token!", nameof(bearerToken));
        }
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        _httpClient.BaseAddress = new Uri(BaseAddress);


    }
    public async Task<long> GetTeamOwner(long teamId)
    {

        if (teamId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(teamId));
        }

        string str = await _httpClient.GetStringAsync(string.Format(TeamOwnerQueryTemplate, teamId));

        ReadDocument doc = JsonConvert.DeserializeObject<ReadDocument>(str);

        return doc.Collection.Items.Select(x => long.Parse(x.Data.GetDataByName("id").Value.ToString())).First();
    }
    public async Task<IEnumerable<Team>> GetActiveTeamsForUser(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        string str = await _httpClient.GetStringAsync(string.Format(ActiveTeamsQueryTemplate, userId));

        ReadDocument doc = JsonConvert.DeserializeObject<ReadDocument>(str);

        return doc.UnpackTeams().ToList();

    }

    public async Task<Team> GetTeamAsync(long teamId)
    {
        if (teamId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(teamId));
        }

        string str = await _httpClient.GetStringAsync(string.Format(TeamByIdQueryTemplate, teamId));

        ReadDocument doc = JsonConvert.DeserializeObject<ReadDocument>(str);

        return doc.UnpackTeams().FirstOrDefault();

    }

    public async Task CreateEvents(IEnumerable<CreateEventRequest> cers)
    {
        CreateEventRequest first = cers.First();

        BulkCreateEventRequest req = new BulkCreateEventRequest
        {
            NotifyTeam = first.NotifyTeam,
            NotifyTeamAsMemberId = await GetTeamOwner(first.TeamId),
            TeamId = first.TeamId
        };

        List<BulkCreateEventRequest.BulkEventTemplate> templates = new List<BulkCreateEventRequest.BulkEventTemplate>();

        foreach (CreateEventRequest cer in cers)
        {
            BulkCreateEventRequest.BulkEventTemplate t = new BulkCreateEventRequest.BulkEventTemplate();

            templates.Add(t);

            long locationId = await FindOrCreateLocationIdByName(cer.LocationName, cer.TeamId);

            t.team_id = cer.TeamId;

            t.location_id = locationId;

            t.is_game = cer.IsGame;

            if (cer.DurationMinutes > 0)
            {
                t.duration_in_minutes = cer.DurationMinutes;
            }

            if (cer.ArriveEarlyMinutes > 0)
            {
                t.minutes_to_arrive_early = cer.ArriveEarlyMinutes;
            }

            if (cer.IsGame)
            {
                long opponenentId = await FindOrCreateOpponentIdByName(cer.OpponentName, cer.TeamId);

                t.opponent_id = opponenentId;

                if (!string.IsNullOrWhiteSpace(cer.GameType))
                {
                    t.game_type = cer.GameType;

                    t.game_type_code = cer.GameType == "Home" ? 1 : 2;
                }
            }
            else
            {
                t.name = cer.Name;
            }

            if (!string.IsNullOrWhiteSpace(cer.Label))
            {
                t.label = cer.Label;
            }

            if (!string.IsNullOrWhiteSpace(cer.Notes))
            {
                t.notes = cer.Notes;
            }

            if (!string.IsNullOrWhiteSpace(cer.LocationDetails))
            {
                t.additional_location_details = cer.LocationDetails;
            }

            if (cer.IsTimeTBD)
            {
                t.is_tbd = true;
                t.start_date = cer.StartDate.Date;

            }
            else
            {
                t.start_date = ConvertToUtc(cer.StartDate);

            }
        }

        req.Templates = templates.ToArray();

        HttpResponseMessage resp = await _httpClient.PostAsJsonAsync("/events/bulk_create", req);

        string str = await resp.Content.ReadAsStringAsync();

        ReadDocument rDoc = JsonConvert.DeserializeObject<ReadDocument>(str);

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(rDoc.Collection.Error.Message);
        }
    }

    private DateTime ConvertToUtc(DateTime startDate)
    {
        return System.TimeZoneInfo.ConvertTimeToUtc(startDate, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

    }
    private class BulkCreateEventRequest
    {
        [JsonProperty("templates")]
        public BulkEventTemplate[] Templates { get; set; }

        [JsonProperty("team_id")]
        public long TeamId { get; set; }

        [JsonProperty("notify_team_as_member_id")]
        public long NotifyTeamAsMemberId { get; set; }

        [JsonProperty("notify_team")]
        public bool NotifyTeam { get; set; }

        public class BulkEventTemplate
        {
            public long team_id { get; set; }

            public long location_id { get; set; }

            public bool is_game { get; set; }

            public int duration_in_minutes { get; set; }

            public int minutes_to_arrive_early { get; set; }

            public long opponent_id { get; set; }

            public string name { get; set; }

            public string label { get; set; }

            public string notes { get; set; }

            public string additional_location_details { get; set; }
            public bool is_tbd { get; set; }
            public DateTime start_date { get; set; }
            public string game_type { get; set; }
            public int game_type_code { get; set; }


        }
    }


    public async Task<IEnumerable<Event>> GetEventsForTeam(long teamId)
    {
        if (teamId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(teamId));
        }

        string str = await _httpClient.GetStringAsync(string.Format(TeamEventQueryTemplate, teamId));

        ReadDocument doc = JsonConvert.DeserializeObject<ReadDocument>(str);

        return doc.UnpackEvents().ToList();
    }
    public async Task CancelEvent(long eventId, bool notifyTeam = true)
    {
        WriteDocument doc = new WriteDocument
        {
            Template = new Template()
        };

        doc.Template.Data.Add(new Data() { Name = "is_canceled", Value = true });

        doc.Template.Data.Add(new Data() { Name = "notify_team", Value = notifyTeam });

        HttpResponseMessage resp = await _httpClient.PutAsJsonAsync($"/events/{eventId}", doc);

        resp.EnsureSuccessStatusCode();
    }
    public async Task<long> FindOrCreateLocationIdByName(string locationName, long teamId)
    {
        if (teamId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(teamId));
        }

        string str = await _httpClient.GetStringAsync($"/locations/search?team_id={teamId}");

        ReadDocument rdoc = JsonConvert.DeserializeObject<ReadDocument>(str);


        var target = rdoc.Collection.Items
            .Select(x => new { Id = long.Parse(x.Data.GetDataByName("id").Value.ToString()), Name = x.Data.GetDataByName("name").Value.ToString() })
            .Where(x => x.Name.Equals(locationName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

        if (target != null)
        {
            return target.Id;
        }
        else
        {
            WriteDocument doc = new WriteDocument
            {
                Template = new Template()
            };

            doc.Template.Data.Add(new Data() { Name = "name", Value = locationName });

            doc.Template.Data.Add(new Data() { Name = "team_id", Value = teamId });

            HttpResponseMessage resp = await _httpClient.PostAsJsonAsync("/locations", doc);

            string str2 = await resp.Content.ReadAsStringAsync();

            ReadDocument rdoc2 = JsonConvert.DeserializeObject<ReadDocument>(str2);

            resp.EnsureSuccessStatusCode();

            return rdoc2.Collection.Items.Select(x => long.Parse(x.Data.GetDataByName("id").Value.ToString())).First();
        }

    }
    public async Task<long> FindOrCreateOpponentIdByName(string opponentName, long teamId)
    {
        if (teamId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(teamId));
        }

        string str = await _httpClient.GetStringAsync($"/opponents/search?team_id={teamId}");

        ReadDocument rdoc = JsonConvert.DeserializeObject<ReadDocument>(str);

        var target = rdoc.Collection.Items
            .Select(x => new { Id = long.Parse(x.Data.GetDataByName("id").Value.ToString()), Name = x.Data.GetDataByName("name").Value.ToString() })
            .Where(x => x.Name.Equals(opponentName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

        if (target != null)
        {
            return target.Id;
        }
        else
        {
            WriteDocument doc = new WriteDocument
            {
                Template = new Template()
            };

            doc.Template.Data.Add(new Data() { Name = "name", Value = opponentName });

            doc.Template.Data.Add(new Data() { Name = "team_id", Value = teamId });

            HttpResponseMessage resp = await _httpClient.PostAsJsonAsync("/opponents", doc);

            string str2 = await resp.Content.ReadAsStringAsync();

            ReadDocument rdoc2 = JsonConvert.DeserializeObject<ReadDocument>(str2);

            resp.EnsureSuccessStatusCode();

            return rdoc2.Collection.Items.Select(x => long.Parse(x.Data.GetDataByName("id").Value.ToString())).First();
        }

    }
    public async Task<long> CreateEvent(CreateEventRequest cer)
    {
        long locationId = await FindOrCreateLocationIdByName(cer.LocationName, cer.TeamId);

        WriteDocument doc = new WriteDocument
        {
            Template = new Template()
        };

        if (cer.NotifyTeam)
        {
            doc.Template.Data.Add(new Data() { Name = "notify_team", Value = cer.NotifyTeam });
            doc.Template.Data.Add(new Data() { Name = "notify_team_as_member_id", Value = await GetTeamOwner(cer.TeamId) });
        }

        doc.Template.Data.Add(new Data() { Name = "team_id", Value = cer.TeamId });

        doc.Template.Data.Add(new Data() { Name = "location_id", Value = locationId });

        doc.Template.Data.Add(new Data() { Name = "is_game", Value = cer.IsGame });

        if (cer.DurationMinutes > 0)
        {
            doc.Template.Data.Add(new Data() { Name = "duration_in_minutes", Value = cer.DurationMinutes });
        }

        if (cer.ArriveEarlyMinutes > 0)
        {
            doc.Template.Data.Add(new Data() { Name = "minutes_to_arrive_early", Value = cer.ArriveEarlyMinutes });
        }

        if (cer.IsGame)
        {
            long opponenentId = await FindOrCreateOpponentIdByName(cer.OpponentName, cer.TeamId);

            doc.Template.Data.Add(new Data() { Name = "opponent_id", Value = opponenentId });

            if (!string.IsNullOrWhiteSpace(cer.GameType))
            {
                doc.Template.Data.Add(new Data() { Name = "game_type", Value = cer.GameType });
                doc.Template.Data.Add(new Data() { Name = "game_type_code", Value = cer.GameType == "Home" ? 1 : 2 });
            }
        }
        else
        {
            doc.Template.Data.Add(new Data() { Name = "name", Value = cer.Name });
        }

        if (!string.IsNullOrWhiteSpace(cer.Label))
        {
            doc.Template.Data.Add(new Data() { Name = "label", Value = cer.Label });
        }

        if (!string.IsNullOrWhiteSpace(cer.Notes))
        {
            doc.Template.Data.Add(new Data() { Name = "notes", Value = cer.Notes });
        }

        if (!string.IsNullOrWhiteSpace(cer.LocationDetails))
        {
            doc.Template.Data.Add(new Data() { Name = "additional_location_details", Value = cer.LocationDetails });
        }

        if (cer.IsTimeTBD)
        {
            doc.Template.Data.Add(new Data() { Name = "is_tbd", Value = true });
            doc.Template.Data.Add(new Data() { Name = "start_date", Value = cer.StartDate.Date });

        }
        else
        {
            doc.Template.Data.Add(new Data() { Name = "start_date", Value = ConvertToUtc(cer.StartDate) });
        }

        HttpResponseMessage resp = await _httpClient.PostAsJsonAsync("/events", doc);

        string str = await resp.Content.ReadAsStringAsync();

        ReadDocument rDoc = JsonConvert.DeserializeObject<ReadDocument>(str);

        if (resp.IsSuccessStatusCode)
        {
            return rDoc.UnpackEvents().First().Id;
        }
        else
        {
            throw new HttpRequestException(rDoc.Collection.Error.Message);
        }
    }
    public async Task<User> GetMe()
    {
        string str = await _httpClient.GetStringAsync(MeQuery);

        ReadDocument doc = JsonConvert.DeserializeObject<ReadDocument>(str);

        return doc.Collection.Items.Select(x => new User
        {
            Id = long.Parse(x.Data.GetDataByName("id").Value.ToString()),
            Email = x.Data.GetDataByName("email").Value.ToString(),
            FirstName = x.Data.GetDataByName("first_name").Value.ToString(),
            LastName = x.Data.GetDataByName("last_name").Value.ToString(),

        }).First();

    }
    public async Task<long> CreateAndInviteTeamMember(CreateTeamMemberRequest req)
    {

        long? existingId = await FindTeamMemberIdByEmailAddress(req.TeamId, req.EmailAddress);

        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        WriteDocument doc = new WriteDocument
        {
            Template = new Template()
        };

        doc.Template.Data.Add(new Data() { Name = "first_name", Value = req.FirstName });
        doc.Template.Data.Add(new Data() { Name = "last_name", Value = req.LastName });
        doc.Template.Data.Add(new Data() { Name = "team_id", Value = req.TeamId });

        if (req.IsManager)
        {
            doc.Template.Data.Add(new Data() { Name = "is_manager", Value = true });
        }

        if (req.IsNonPlayer)
        {
            doc.Template.Data.Add(new Data() { Name = "is_non_player", Value = true });
        }

        HttpResponseMessage resp = await _httpClient.PostAsJsonAsync("/members", doc);

        string str = await resp.Content.ReadAsStringAsync();

        ReadDocument rDoc = JsonConvert.DeserializeObject<ReadDocument>(str);


        long member_id = 0;

        if (resp.IsSuccessStatusCode)
        {

            member_id = rDoc.Collection.Items.Select(x => long.Parse(x.Data.GetDataByName("id").Value.ToString())).First();

            if (!string.IsNullOrWhiteSpace(req.EmailAddress))
            {

                WriteDocument eDoc = new WriteDocument
                {
                    Template = new Template()
                };

                eDoc.Template.Data.Add(new Data() { Name = "member_id", Value = member_id });
                eDoc.Template.Data.Add(new Data() { Name = "email", Value = req.EmailAddress });
                eDoc.Template.Data.Add(new Data() { Name = "receives_team_emails", Value = true });
                eDoc.Template.Data.Add(new Data() { Name = "is_hidden", Value = true });


                HttpResponseMessage eResp = await _httpClient.PostAsJsonAsync("/member_email_addresses", eDoc);

                string eStr = await resp.Content.ReadAsStringAsync();

                ReadDocument erDoc = JsonConvert.DeserializeObject<ReadDocument>(eStr);

                if (eResp.IsSuccessStatusCode)
                {
                    long emailId = erDoc.Collection.Items.Select(x => long.Parse(x.Data.GetDataByName("id").Value.ToString())).First();

                    long teamOwnerId = await GetTeamOwner(req.TeamId);

                    var inviteCommand = new { team_id = req.TeamId, member_id, introduction = req.InvitationMessage, notify_as_member_id = teamOwnerId };

                    HttpResponseMessage eEviteResp = await _httpClient.PostAsJsonAsync("teams/invite", inviteCommand);

                    ReadDocument evrDoc = JsonConvert.DeserializeObject<ReadDocument>(await resp.Content.ReadAsStringAsync());

                    eEviteResp.EnsureSuccessStatusCode();

                    return member_id;
                }
                else
                {
                    throw new HttpRequestException(erDoc.Collection.Error.Message);
                }
            }
            else
            {
                throw new HttpRequestException(rDoc.Collection.Error.Message);
            }
        }
        else
        {
            return member_id;
        }
    }
    public async Task ChangeTeamOwner(CreateTeamMemberRequest req)
    {
        long? newOwnerMemberId = await FindTeamMemberIdByEmailAddress(req.TeamId, req.EmailAddress);

        if (newOwnerMemberId == null)
        {
            req.IsNonPlayer = true;
            req.IsManager = true;

            newOwnerMemberId = await CreateAndInviteTeamMember(req);
        }

        var changeOwnerCommand = new { team_id = req.TeamId, member_id = newOwnerMemberId };

        HttpResponseMessage resp = await _httpClient.PostAsJsonAsync("teams/change_owner", changeOwnerCommand);

        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException(JsonConvert.DeserializeObject<ReadDocument>(await resp.Content.ReadAsStringAsync()).Collection.Error.Message);
        }
    }
    public async Task<long?> FindTeamMemberIdByEmailAddress(long teamId, string emailAddress)
    {
        if (teamId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(teamId));
        }

        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            throw new ArgumentException("must provide valid email address!", nameof(emailAddress));
        }

        string str = await _httpClient.GetStringAsync(string.Format(FindTeamMemberIdByEmailTemplate, emailAddress, teamId));

        ReadDocument doc = JsonConvert.DeserializeObject<ReadDocument>(str);

        if (doc.Collection.Items.Any())
        {
            doc.Collection.Items.Select(x => long.Parse(x.Data.GetDataByName("member_id").Value.ToString())).First();
        }
        return null;
    }
    public async Task<long> CreateTeam(CreateTeamRequest team)
    {
        WriteDocument doc = new WriteDocument
        {
            Template = new Template()
        };

        doc.Template.Data.Add(new Data() { Name = "name", Value = team.Name });
        doc.Template.Data.Add(new Data() { Name = "sport_id", Value = team.SportId });
        doc.Template.Data.Add(new Data() { Name = "location_country", Value = team.LocationCountry });
        doc.Template.Data.Add(new Data() { Name = "time_zone", Value = team.IANATimeZone });
        doc.Template.Data.Add(new Data() { Name = "location_postal_code", Value = team.LocationPostalCode });

        HttpResponseMessage resp = await _httpClient.PostAsJsonAsync("/teams", doc);

        string str = await resp.Content.ReadAsStringAsync();

        ReadDocument rDoc = JsonConvert.DeserializeObject<ReadDocument>(str);

        if (resp.IsSuccessStatusCode)
        {
            return rDoc.UnpackTeams().First().Id;
        }
        else
        {
            throw new HttpRequestException(rDoc.Collection.Error.Message);
        }
    }
    public async Task MakeTeamOwnerNonPlayer(long teamId)
    {
        long ownerMemberId = await GetTeamOwner(teamId);

        WriteDocument doc = new WriteDocument
        {
            Template = new Template()
        };

        doc.Template.Data.Add(new Data() { Name = "is_non_player", Value = true });

        HttpResponseMessage resp = await _httpClient.PutAsJsonAsync($"/members/{ownerMemberId}", doc);

        resp.EnsureSuccessStatusCode();
    }
    public class CreateTeamMemberRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public long TeamId { get; set; }
        public string InvitationMessage { get; set; }
        public bool IsNonPlayer { get; set; }
        public bool IsManager { get; set; }
    }
    public class Event
    {
        public long Id { get; set; }
        public long TeamId { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsTimeTBD { get; set; }
        public string LocationName { get; set; }
        public string Notes { get; set; }
        public bool IsGame { get; set; }
        public int DurationMinutes { get; set; }
        public int ArriveEarlyMinutes { get; set; }
        public string OpponentName { get; set; }
    }
    public class CreateEventRequest : Event
    {
        public bool NotifyTeam { get; set; } = true;
        public string Label { get; set; }
        public string LocationDetails { get; set; }
        public string GameType { get; internal set; }
    }
    public class Team
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string IsActive { get; set; }
    }
    public class CreateTeamRequest : Team
    {

        public string LocationCountry { get; set; } = "United States";
        public string IANATimeZone { get; set; } = "America/Chicago";
        public string LocationPostalCode { get; set; } = "60089";
        public int SportId { get; set; } = 16; //ice hockey
    }
    public class User
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
    public void Dispose()
    {
        if (_httpClient != null)
        {
            _httpClient.Dispose();
        }

        _httpClient = null;
    }
}

public static class TeamSnapApiExtentions
{
    public static IEnumerable<TeamSnapApi.Event> UnpackEvents(this ReadDocument doc)
    {
        return doc.Collection.Items.Select(x =>
        {

            TeamSnapApi.Event r = new TeamSnapApi.Event
            {
                Id = long.Parse(x.Data.GetDataByName("id").Value.ToString()),

                TeamId = long.Parse(x.Data.GetDataByName("team_id").Value.ToString()),

                Name = x.Data.GetDataByName("name").Value.ToString(),

                IsTimeTBD = bool.Parse(x.Data.GetDataByName("is_tbd").Value.ToString()),

                StartDate = x.Data.GetDataByName("start_date").Value.ToObject<DateTime>(),

                IsCancelled = bool.Parse(x.Data.GetDataByName("is_canceled").Value.ToString()),

                IsGame = bool.Parse(x.Data.GetDataByName("is_game").Value.ToString()),

                LocationName = x.Data.GetDataByName("location_name").Value.ToString(),

                OpponentName = x.Data.GetDataByName("opponent_name").Value.ToString(),

                Notes = x.Data.GetDataByName("notes").Value.ToString()


            };

            int.TryParse(x.Data.GetDataByName("duration_in_minutes").Value.ToString(), out int duration);

            r.DurationMinutes = duration;


            int.TryParse(x.Data.GetDataByName("minutes_to_arrive_early").Value.ToString(), out int arriveEarlyMin);

            r.ArriveEarlyMinutes = arriveEarlyMin;

            return r;
        });
    }
    public static IEnumerable<TeamSnapApi.Team> UnpackTeams(this ReadDocument doc)
    {
        return doc.Collection.Items.Select(x => new TeamSnapApi.Team
        {
            Id = int.Parse(x.Data.GetDataByName("id").Value.ToString()),

            Name = x.Data.GetDataByName("name").Value.ToString()

        });
    }
}
