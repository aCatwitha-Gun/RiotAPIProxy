using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add COrS so frontend is allowed to call Riot API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
    policy =>
    {
        policy.AllowAnyOrigin() // Adjust later to lock down domain
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient();

var app = builder.Build();
app.UseCors("AllowFrontend");

// Define endpoint frontend will call
app.MapGet("/api/playerstats", async (IConfiguration config, HttpClient http) =>
{
    var apiKey = config["RIOT_API_KEY"];

    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Problem("API Key is missing from the environment.");
    }
    
    // Target Riot account
    string gameName = "A Cat with a Gun";
    string tagLine = "NA1";

    // Add API key to the request headers for all calls
    http.DefaultRequestHeaders.Clear();
    http.DefaultRequestHeaders.Add("X-Riot-Token", apiKey);

    try
    {
        // Account-V1 to get PUUID
        var accountResponse = await http.GetAsync($"https://americas.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{gameName}/{tagLine}");
        accountResponse.EnsureSuccessStatusCode();
        var accountData = await accountResponse.Content.ReadFromJsonAsync<JsonElement>();
        var puuid = accountData.GetProperty("puuid").GetString();

        // Summoner-V4 to get Summoner ID
        var summonerResponse = await http.GetAsync($"https://na1.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{puuid}");
        summonerResponse.EnsureSuccessStatusCode();
        var summonerData = await summonerResponse.Content.ReadFromJsonAsync<JsonElement>();
        
        var profileIconId = summonerData.GetProperty("profileIconId").GetInt32();
        var summonerLevel = summonerData.GetProperty("summonerLevel").GetInt64();

        // League-V4 to get rank and win/loss
        var leagueResponse = await http.GetAsync($"https://na1.api.riotgames.com/lol/league/v4/entries/by-puuid/{puuid}");
        leagueResponse.EnsureSuccessStatusCode();
        var leagueData = await leagueResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Find ranked solo/duo queue data
        JsonElement? rankedData = null;
        foreach (var entry in leagueData.EnumerateArray())
        {
            if (entry.GetProperty("queueType").GetString() == "RANKED_SOLO_5x5")
            {
                rankedData = entry;
                break;
            }
        }

        // Champion-Mastery-V4 to get top 5 champions
        var masteryResponse = await http.GetAsync($"https://na1.api.riotgames.com/lol/champion-mastery/v4/champion-masteries/by-puuid/{puuid}/top?count=5");
        masteryResponse.EnsureSuccessStatusCode();
        var masteryData = await masteryResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Normalize and construct final JSON payload
        var finalPayload = new
        {
            Player = $"{gameName}#{tagLine}",
            ProfileIconId = profileIconId,
            SummonerLevel = summonerLevel,
            Rank = rankedData.HasValue 
                ? $"{rankedData.Value.GetProperty("tier").GetString()} {rankedData.Value.GetProperty("rank").GetString()}" 
                : "Unranked",
            Wins = rankedData.HasValue ? rankedData.Value.GetProperty("wins").GetInt32() : 0,
            Losses = rankedData.HasValue ? rankedData.Value.GetProperty("losses").GetInt32() : 0,
            LeaguePoints = rankedData.HasValue ? rankedData.Value.GetProperty("leaguePoints").GetInt32() : 0,
            TopChampions = masteryData.EnumerateArray().Select(c => new 
            {
                ChampionId = c.GetProperty("championId").GetInt32(),
                MasteryLevel = c.GetProperty("championLevel").GetInt32(),
                MasteryPoints = c.GetProperty("championPoints").GetInt32()
            })
        };
        
        return Results.Ok(finalPayload);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Error communicating with Riot API: {ex.Message}");
    }
});

app.Run();
