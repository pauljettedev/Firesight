# Firesight

A map of current Canadian wildfires, built as a portfolio project. It pulls live data from
the Canadian Wildland Fire Information System (CWFIS), stores it in PostgreSQL/PostGIS, and
serves it through a React map, a REST API, and MCP tools.

It is a demo, not an official wildfire service. See the [disclaimer](#disclaimer).

## Features

- Map of current wildfires with status, size, and how recently each fire was reported
- Find fires within a distance of a town or city
- **Ask Firesight:** ask questions in plain English ("any fires near Kamloops?"), answered by
  Claude using Firesight's own data
- MCP tools, so AI assistants can query the same data
- Hourly sync from CWFIS

## Stack

React, TypeScript, MUI, MapLibre · ASP.NET Core (.NET 10), EF Core · PostgreSQL/PostGIS ·
Claude API, MCP · Docker, GitHub Actions

## Run locally

Needs the .NET 10 SDK, Node.js LTS, and Docker.

```powershell
docker compose up -d                                   # database
dotnet run --project src/Firesight.Api                 # API on :5213
cd src/Firesight.Web; npm install; npm run dev         # web app on :5173
```

Full steps, including the Claude API key for Ask Firesight: [Setup](docs/setup.md).

## Tests

```powershell
dotnet test                                    # backend (needs Docker running)
cd src/Firesight.Web; npm test; npm run lint   # frontend
```

## Documentation

- [Setup](docs/setup.md): run and test locally
- [Architecture](docs/architecture.md): how the projects fit together
- [API](docs/api.md): REST endpoints
- [MCP](docs/mcp.md): MCP tools
- [Data sources](docs/data-sources.md): how CWFIS data is loaded and stored
- [Decisions](docs/decisions/README.md): why things are built the way they are
- [Deployment](docs/deployment.md) and [server setup walkthrough](docs/server-setup-walkthrough.md)
- [CWFIS historical data](docs/cwfis-historical-reference.md): research notes for a future feature
- [References](docs/references.md): official docs for the tools used

## Disclaimer

Firesight is a demo. Do not use it for emergency, evacuation, or safety decisions. For
official wildfire information, use [CWFIS](https://cwfis.cfs.nrcan.gc.ca/) and your provincial
or territorial wildfire agency.
