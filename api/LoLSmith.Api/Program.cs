using Options.RiotOptions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Services.Riot;
using Services.Riot.Dtos;
using LoLSmith.Db;

var builder = WebApplication.CreateBuilder(args);

// register RateLimitHandler
builder.Services.AddTransient<RateLimitHandler>();

// typed Riot clients with the rate-limit handler in the pipeline
builder.Services.AddHttpClient<IRiotMatchClient, RiotClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<RateLimitHandler>();

builder.Services.AddHttpClient<IRiotAccountClient, RiotClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<RateLimitHandler>();

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
builder.Services.AddHttpClient<IRiotMatchClient, RiotClient>();

builder.Services.AddTransient<RateLimitHandler>();

// Resolve absolute path to solution root (parent of the api project) and place DB in /db/LoLSmith.db
// Go up two directories: from .../LoLSmith.Api -> ../api -> ../ (solution root)
var solutionRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var dbFolder = Path.Combine(solutionRoot, "db");
Directory.CreateDirectory(dbFolder); // ensure folder exists
var dbPath = Path.Combine(dbFolder, "LoLSmith.db");

builder.Services.AddDbContext<LoLSmithDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


// Simple root endpoint to verify the API is running
app.MapGet("/", (EndpointDataSource endpointData) =>
{
    var routes = endpointData.Endpoints
        .OfType<RouteEndpoint>()
        .Select(e => e.RoutePattern?.RawText ?? e.DisplayName ?? string.Empty)
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Distinct()
        .OrderBy(s => s)
        .ToList();
    return Results.Ok(new
    {
        name = "LoLSmith.Api",
        status = "ok",
        endpoints = routes
    });
});

app.MapGet("/config-check", (IOptions<RiotOptions> o) => string.IsNullOrWhiteSpace(o.Value.ApiKey) ?
    Results.Problem("Missing") : Results.Ok(new { status = "ok" }));

// Valid regional hosts for Account-V1 (by-riot-id)
string[] allowedPlatforms = ["americas", "europe", "asia"];

// Lookup PUUID by Riot ID (gameName + tagLine) using regional routing


app.MapControllers();

app.Run();

