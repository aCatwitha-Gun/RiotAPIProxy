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

// Register HttpClient so requests can be made
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseCors("AllowFrontend");

// Define endpoint frontend will call
app.MapGet("/api/playerstats", async (IConfiguration config, HttpClient http) =>
{
    // look for environment variable named "RIOT_API_KEY"
    var apiKey = config["RIOT_API_KEY"];

    if (string.IsNullOrEmpty(apiKey))
    {
        return Results.Problem("API Key is missing from the environment.");
    }
    
    // Insert Riot API fetching logic here

    return Results.Ok(new { message = "Proxy is working", keyStatus = "Loaded" });
});

app.Run();
