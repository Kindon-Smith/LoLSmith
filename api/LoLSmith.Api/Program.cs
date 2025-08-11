using Options.RiotOptions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

using Services.Riot;
using Services.Riot.Dtos;

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
builder.Services.AddHttpClient<IRiotClient, RiotClient>();
// Also use the same typed client for Account-V1 calls (PUUID via Riot ID)
builder.Services.AddHttpClient<IRiotAccountClient, RiotClient>();

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
        "/api/summoners/{platform}/{name}"
    }
}));

app.MapGet("/config-check", (IOptions<RiotOptions> o) => string.IsNullOrWhiteSpace(o.Value.ApiKey) ?
    Results.Problem("Missing") : Results.Ok(new { status = "ok" }));

// Valid regional hosts for Account-V1 (by-riot-id)
string[] allowedPlatforms = ["americas", "europe", "asia"];

// Lookup PUUID by Riot ID (gameName + tagLine) using regional routing
app.MapGet("/api/summoners/{platform}/{name}/{tag}",
    async (string platform, string name, string tag, [FromServices] IRiotAccountClient accounts, CancellationToken ct) =>
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

        // return only the PUUID for now
        return Results.Ok(new { dto.Puuid });
    })
    .WithName("GetPuuidByRiotId")
    .WithOpenApi();
app.Run();

