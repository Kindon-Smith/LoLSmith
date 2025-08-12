using Options.RiotOptions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Services.Riot;
using Services.Riot.Dtos;
using LoLSmith.Db;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// Add API key from secrets to builder
builder.Services.AddOptions<RiotOptions>()
                .Bind(builder.Configuration.GetSection("Riot"))
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Riot:ApiKey is missing")
                .ValidateOnStart();
// Also use the same typed client for Account-V1 calls (PUUID via Riot ID)
builder.Services.AddHttpClient<IRiotAccountClient, RiotClient>();

// Resolve absolute path to solution root (parent of the api project) and place DB in /db/LoLSmith.db
// Go up two directories: from .../LoLSmith.Api -> ../api -> ../ (solution root)
var solutionRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var dbFolder = Path.Combine(solutionRoot, "db");
Directory.CreateDirectory(dbFolder); // ensure folder exists
var dbPath = Path.Combine(dbFolder, "LoLSmith.db");

builder.Services.AddDbContext<LoLSmithDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


// Simple root endpoint to verify the API is running
app.MapGet("/", () => Results.Ok(new
{
    name = "LoLSmith.Api",
    status = "ok",
    endpoints = new[]
    {
        "/",
        "/config-check",
        "/api/summoners/{region}/{name}/{tag}"
    }
}));

app.MapGet("/config-check", (IOptions<RiotOptions> o) => string.IsNullOrWhiteSpace(o.Value.ApiKey) ?
    Results.Problem("Missing") : Results.Ok(new { status = "ok" }));

// Valid regional hosts for Account-V1 (by-riot-id)
string[] allowedPlatforms = ["americas", "europe", "asia"];

// Lookup PUUID by Riot ID (gameName + tagLine) using regional routing
app.MapGet("/api/summoners/{platform}/{name}/{tag}",
    async (string platform, string name, string tag,
           [FromServices] IRiotAccountClient accounts,
           [FromServices] LoLSmithDbContext db,
           CancellationToken ct) =>
    {
        // validate platform
        if (!allowedPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Invalid platform code." });
        }

        // call to client (regional host e.g., americas/europe/asia)
        var dto = await accounts.GetPuuidByRiotIdAsync(platform, name, tag, ct);

        // 404 mapping
        if (dto is null) return Results.NotFound();

        // Upsert lightweight user record based on PUUID
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Puuid == dto.Puuid, ct);
        if (existing is null)
        {
            db.Users.Add(new User
            {
                Puuid = dto.Puuid,
                SummonerName = dto.GameName,
                TagLine = dto.TagLine,
                LastUpdated = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        else if (existing.SummonerName != dto.GameName || existing.TagLine != dto.TagLine)
        {
            existing.SummonerName = dto.GameName;
            existing.TagLine = dto.TagLine;
            existing.LastUpdated = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new { dto.Puuid, dto.GameName, dto.TagLine });
    })
    .WithName("GetPuuidByRiotId")
    .WithOpenApi();
app.Run();

