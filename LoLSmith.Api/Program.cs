var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();

public class RiotService
{
    private readonly string _apiKey;

    public RiotService(IConfiguration configuration)
    {
        _apiKey = configuration["RiotApi:ApiKey"] ?? throw new ArgumentNullException("RiotApi:ApiKey configuration is missing.");
    }

    // Example method to use the API key
    public void UseApi()
    {
        // Use _apiKey for API calls
    }
}

