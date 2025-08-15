# LoLSmith

A League of Legends match tracking and analysis application built with .NET, React, and SQLite. Uses the Riot Games API to fetch and store match history data with efficient many-to-many relationship handling.

## Features

- **Match History Tracking**: Fetch and store player match history from Riot API
- **Efficient Data Storage**: Deduplicates matches across multiple users
- **Many-to-Many Relationships**: Links users to matches with proper database design
- **RESTful API**: Clean endpoints for summoner lookup and match data
- **Rate Limiting Ready**: Architecture supports Riot API rate limiting

## Structure

```
├── api/LoLSmith.Api/          # ASP.NET Core Web API
│   ├── Controllers/           # API endpoints (Summoner, Match)
│   ├── Services/Riot/         # External API clients
│   ├── Options/               # Configuration classes
│   └── Utils/                 # Helper utilities
├── Db/                        # Database layer
│   ├── objects/               # EF Core entities (User, Match, UserMatches)
│   └── LoLSmith.db           # SQLite database
├── web/                       # React app (planned)
├── infra/                     # Docker, K8s (planned)
└── docs/                      # Documentation
```

## Technology Stack

- **Backend**: ASP.NET Core 9, Entity Framework Core
- **Database**: SQLite with EF Core migrations
- **External API**: Riot Games API v5
- **Frontend**: React (planned)
- **DevOps**: Docker, Kubernetes (planned)

## API Endpoints

### Summoner Lookup
```
GET /api/summoners/{platform}/{name}/{tag}
```
- Fetches PUUID for a Riot ID
- Creates/updates user record in database
- Example: `/api/summoners/americas/Faker/T1`

### Match History
```
GET /api/matches/{platform}/{puuid}
```
- Fetches match history for a PUUID
- Stores unique matches and user-match relationships
- Automatically deduplicates shared matches between users

### Debug Endpoints
```
GET /api/matches/debug/tables    # Database table counts
```

## Database Schema

- **Users**: Stores player PUUIDs and basic info
- **Matches**: Stores unique match data (game mode, duration, etc.)
- **UserMatches**: Join table linking users to matches (many-to-many)

## Quick Start

### Prerequisites
- .NET 9 SDK
- Riot Games API key (from https://developer.riotgames.com/)

### Setup
1. **Clone and build:**
   ```bash
   git clone <repo-url>
   cd LoLApp
   dotnet build
   ```

2. **Set up API key:**
   ```bash
   cd api/LoLSmith.Api
   dotnet user-secrets set "RiotOptions:ApiKey" "RGAPI-your-key-here"
   ```

3. **Run database migrations:**
   ```bash
   dotnet ef database update
   ```

4. **Start the API:**
   ```bash
   dotnet run --project api/LoLSmith.Api
   ```

5. **Test the API:**
   - Visit: `http://localhost:5033/api/summoners/americas/Faker/T1`
   - Check database: `http://localhost:5033/api/matches/debug/tables`

## Configuration

### User Secrets (Development)
```bash
dotnet user-secrets set "RiotOptions:ApiKey" "RGAPI-your-key"
```

### appsettings.Development.json
```json
{
  "RiotOptions": {
    "ApiKey": "",
    "Region": "na1",
    "RateLimit": {
      "Enabled": true,
      "MaxRequestsPerMinute": 20
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=../db/LoLSmith.db"
  }
}
```

## Development Notes

- **API Keys**: Development keys expire every 24 hours
- **Database**: SQLite for development, easily switchable to PostgreSQL/SQL Server
- **Many-to-Many**: Efficient storage - shared matches between users aren't duplicated
- **Error Handling**: 401 errors usually indicate expired API key

## Current Status

✅ **Completed:**
- User and match data persistence
- Many-to-many relationship handling
- Basic match history fetching
- RESTful API endpoints
- Database migrations

🔄 **In Progress:**
- Match detail population (champion info, KDA, etc.)
- Rate limiting implementation

📋 **Planned:**
- React frontend
- Caching layer
- Authentication system
- Docker containerization
- Kubernetes deployment

## Contributing

This is a learning project for .NET, React, and cloud technologies. Feel free to explore the code and suggest improvements!

## Security

- Never commit API keys to version control
- Use .NET User Secrets for development
- Use proper secret management in production (e.g., Azure Key Vault, AWS Secrets Manager)
