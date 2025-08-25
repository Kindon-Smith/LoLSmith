using Options.RiotOptions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

using Services.Riot;
using Services.Riot.Dtos;
using LoLSmith.Db;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen();

// Add API key from secrets to builder
builder.Services.AddOptions<RiotOptions>()
                .Bind(builder.Configuration.GetSection("Riot"))
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Riot:ApiKey is missing")
                .ValidateOnStart();

// register handler and HttpClient via DI
builder.Services.AddTransient<RateLimitHandler>();
builder.Services.AddHttpClient<IRiotAccountClient, RiotClient>()
       .AddHttpMessageHandler<RateLimitHandler>();
builder.Services.AddHttpClient<IRiotMatchClient, RiotClient>()
       .AddHttpMessageHandler<RateLimitHandler>();

// Resolve absolute path to solution root (parent of the api project) and place DB in /db/LoLSmith.db
// Go up two directories: from .../LoLSmith.Api -> ../api -> ../ (solution root)
var solutionRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var dbFolder = Path.Combine(solutionRoot, "db");
Directory.CreateDirectory(dbFolder); // ensure folder exists
var dbPath = Path.Combine(dbFolder, "LoLSmith.db");

builder.Services.AddDbContext<LoLSmithDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// memory cache for RiotClient caching
builder.Services.AddMemoryCache();

// CORS for local React dev
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", p =>
        p.WithOrigins("http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// JWT auth (dev-friendly, symmetric key from user-secrets)
var jwtKey = builder.Configuration["Auth:JwtKey"];
if (!string.IsNullOrWhiteSpace(jwtKey))
{
    var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuerSigningKey = true
        };
    });
}

builder.Services.AddControllers();

var app = builder.Build();

// simple API-key middleware for trusted frontend (set Frontend:ApiKey via user-secrets)
var frontendKey = builder.Configuration["Frontend:ApiKey"];

app.Use(async (ctx, next) =>
{
    // allow auth endpoints to run without client API key
    if (ctx.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
        var isDev = app.Environment.IsDevelopment();
        var jwtConfigured = !string.IsNullOrWhiteSpace(config["Auth:JwtKey"]);

        if (string.IsNullOrWhiteSpace(frontendKey))
        {
            await next();
            return;
        }

        if (ctx.Request.Headers.TryGetValue("X-Client-ApiKey", out var k))
        {
            if (k == frontendKey)
            {
                // create a simple authenticated identity for API-key clients
                var claims = new[] { new System.Security.Claims.Claim("client_id", "frontend") };
                var identity = new System.Security.Claims.ClaimsIdentity(claims, "ApiKey");
                ctx.User = new System.Security.Claims.ClaimsPrincipal(identity);

                await next();
                return;
            }
            else
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                var payload = new { error = "Invalid API key", hint = isDev ? "Ensure Frontend:ApiKey matches X-Client-ApiKey header" : null };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
                return;
            }
        }

        if (ctx.Request.Headers.ContainsKey("Authorization"))
        {
            if (!jwtConfigured)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                var payload = new { error = "Authorization header present but server JWT is not configured", hint = isDev ? "Set Auth:JwtKey in user-secrets or use X-Client-ApiKey for dev" : null };
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
                return;
            }

            await next(); // let JwtBearer middleware validate the token
            return;
        }

        // no credentials provided
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.ContentType = "application/json";
        var missingPayload = new
        {
            error = "Missing credentials",
            detail = "Provide X-Client-ApiKey header or Authorization: Bearer <token>",
            hint = isDev ? "You can set Frontend:ApiKey via dotnet user-secrets for dev" : null
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(missingPayload));
        return;
    }

    await next();
});

app.UseCors("DevCors");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
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

