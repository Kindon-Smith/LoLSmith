using Options.RiotOptions;
using Microsoft.Extensions.Options;

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
        "/config-check"
    }
}));

app.MapGet("/config-check", (IOptions<RiotOptions> o) => string.IsNullOrWhiteSpace(o.Value.ApiKey) ?
    Results.Problem("Missing") : Results.Ok(new { status = "ok" }));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
