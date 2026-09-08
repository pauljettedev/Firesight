# Firesight

Firesight is a portfolio project for exploring Canadian wildfire data through an interactive map, REST API, and MCP tools.

It is intended to demonstrate practical full-stack architecture and integration work rather than act as an operational wildfire service.

## What it demonstrates

- React + TypeScript frontend
- MapLibre GL JS mapping
- ASP.NET Core API
- layered Application / Domain / Infrastructure projects
- PostgreSQL + PostGIS spatial storage and queries
- Entity Framework Core
- CWFIS wildfire data ingestion
- MCP tools backed by the same application services as the REST API
- synchronization and per-fire freshness tracking
- integration testing with PostgreSQL/PostGIS containers

## Architecture

```text
React / TypeScript / MUI / MapLibre
                |
                | HTTP / REST
                v
          Firesight.Api
                |
                v
       Firesight.Application
                |
                v
          Firesight.Domain
                ^
                |
     Firesight.Infrastructure
      |                  |
      |                  +--> CWFIS
      +----------------------> PostgreSQL/PostGIS

Firesight.Mcp
      |
      v
Firesight.Application
```

The API and MCP surfaces reuse the Application layer rather than duplicating wildfire rules.

More detail: [Architecture](docs/architecture.md)

## Current capabilities

- load the current CWFIS wildfire snapshot
- retain wildfire state in PostgreSQL/PostGIS
- track the last successful dataset synchronization
- distinguish dataset freshness from individual wildfire observation freshness
- display wildfire status, size, freshness, and source timestamps on the map
- find wildfires within a radius using PostGIS spatial queries
- expose wildfire queries through REST and MCP
- handle source validation, paging, and failed synchronization without treating partial data as a successful refresh

## Data source

Wildfire data comes from the Canadian Wildland Fire Information System (CWFIS).

Firesight uses the CWFIS active-fire WFS source as an upstream dataset and stores a current/recent operational view locally. CWFIS identifiers and stage-of-control values are preserved rather than replaced with Firesight-specific equivalents.

See [Data Sources](docs/data-sources.md) for implementation details.

## Run locally

Requirements:

- .NET 10 SDK
- Node.js LTS
- Docker Desktop
- Git

Start PostgreSQL/PostGIS:

```powershell
docker compose up -d
```

Start the API:

```powershell
dotnet run --project src/Firesight.Api
```

Start the web application in another terminal:

```powershell
cd src/Firesight.Web
npm install
npm run dev
```

Then open:

```text
http://localhost:5173
```

The full setup guide is in [Quick Start](docs/quickstart.md) and [Local Setup](docs/setup.md).

## Tests

Run the .NET test suite from the repository root:

```powershell
dotnet test
```

The integration suite uses Testcontainers and requires Docker to be running.

Frontend checks:

```powershell
cd src/Firesight.Web
npm run lint
npm run build
```

## Documentation

- [Architecture](docs/architecture.md)
- [API](docs/api.md)
- [Data Sources](docs/data-sources.md)
- [MCP](docs/mcp.md)
- [Quick Start](docs/quickstart.md)
- [Local Setup](docs/setup.md)
- [Deployment](docs/deployment.md)
- [References](docs/references.md)

## Project status

Firesight is under active development as a software-development portfolio project. The emphasis is on clear architecture, realistic integration work, spatial data handling, and maintainable code rather than production-scale traffic or operational wildfire response.

## Disclaimer

Firesight is a demonstration project only. It must not be used for emergency, evacuation, public-safety, or operational wildfire decisions.

For authoritative wildfire information, use the official Canadian Wildland Fire Information System and the responsible provincial or territorial wildfire agency.
