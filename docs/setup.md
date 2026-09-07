# Local Setup

## Prerequisites

Install:

- .NET 10 SDK
- Node.js LTS
- npm
- Docker Desktop
- Git
- Visual Studio or another suitable IDE

## Repository structure

```text
Firesight/
├── src/
│   ├── Firesight.Api/
│   ├── Firesight.Application/
│   ├── Firesight.Domain/
│   ├── Firesight.Infrastructure/
│   ├── Firesight.Mcp/
│   └── Firesight.Web/
├── tests/
│   ├── Firesight.UnitTests/
│   └── Firesight.IntegrationTests/
├── docs/
├── docker-compose.yml
└── Firesight.slnx
```

## Start PostgreSQL/PostGIS

From the repository root:

```powershell
docker compose up -d
docker compose ps
```

Verify PostgreSQL:

```powershell
docker exec firesight-postgres pg_isready -U firesight -d firesight
```

Expected result includes:

```text
accepting connections
```

The API applies EF Core migrations on startup. The initial migration enables the PostGIS extension and creates the wildfire table and unique external-ID index.

## Start the API

From the repository root:

```powershell
dotnet run --project src/Firesight.Api
```

Current development URL:

```text
http://localhost:5213
```

The first startup attempts to import current active fires from CWFIS. An upstream CWFIS failure is logged but does not prevent the API from starting. The API then refreshes the feed hourly while it is running.

Per-fire staleness is controlled by `WildfireFreshness:StaleAfterHours`; the current default is 48 hours.

Useful endpoints:

```text
http://localhost:5213/api/health
http://localhost:5213/api/wildfires
http://localhost:5213/api/wildfires/sync-state
```

Force an immediate CWFIS refresh:

```powershell
Invoke-RestMethod -Method Post http://localhost:5213/api/wildfires/sync
```

## Start the frontend

In a second terminal:

```powershell
cd src/Firesight.Web
npm run dev
```

Current development URL:

```text
http://localhost:5173
```

Vite proxies requests beginning with `/api` to the ASP.NET Core API.

## Frontend checks

```powershell
npm run lint
npm run build
```

## Backend build

From the repository root:

```powershell
dotnet build
```

`dotnet build` performs package restore automatically unless `--no-restore` is specified.

## Integration tests

The integration test project uses Testcontainers to start a disposable PostGIS-backed PostgreSQL instance and applies the real EF Core migrations before running repository tests. Docker Desktop must be running.

```powershell
dotnet test tests/Firesight.IntegrationTests/Firesight.IntegrationTests.csproj
```

These tests exercise the PostgreSQL/PostGIS persistence path rather than EF Core's in-memory provider.
