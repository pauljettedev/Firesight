# Architecture

## Overview

Firesight AI is designed as a modular monolith with separate frontend, API, application, domain, infrastructure, and MCP projects.

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
      |        |         |
      |        |         +--> OpenAI
      |        +------------> CWFIS
      +---------------------> PostgreSQL/PostGIS

Firesight.Mcp
      |
      v
Firesight.Application
```

## Project responsibilities

### Firesight.Api

ASP.NET Core HTTP layer.

Responsibilities:
- REST endpoint registration
- HTTP request/response handling
- API-specific DTOs and mapping
- API-specific middleware and rate limiting
- OpenAPI exposure

Business logic should remain outside this project.

### Firesight.Application

Application use cases and orchestration.

Examples:
- get active fires
- find fires near a location
- get fire details
- refresh wildfire data
- calculate application-level summaries

Both REST endpoints and MCP tools should reuse this layer.

### Firesight.Domain

Core domain models and domain rules.

This project should remain independent of infrastructure concerns and should not reference EF Core, HTTP, OpenAI, CWFIS, or PostgreSQL.

### Firesight.Infrastructure

External-system implementations.

Responsibilities:
- EF Core
- PostgreSQL/PostGIS
- Npgsql
- NetTopologySuite
- CWFIS integration
- OpenAI integration
- persistence implementations
- external service clients

### Firesight.Mcp

MCP server/tool definitions.

MCP tools should delegate to application services rather than duplicate business logic.

### Firesight.Web

React/TypeScript frontend.

Responsibilities:
- map
- filters
- fire details
- AI query UI
- application navigation
- demo/safety disclaimer

During development, Vite proxies `/api` requests to ASP.NET Core.

## Dependency direction

The domain remains at the bottom of the dependency graph.

```text
Api ------------> Application ------------> Domain
 |                    ^
 |                    |
 +--> Infrastructure--+

Mcp ------------> Application
 |
 +--------------> Infrastructure
```

The exact references may evolve as the implementation becomes more concrete, but business logic should remain independent of transport and persistence concerns.

## Development runtime

```text
Browser
  |
  v
Vite :5173
  |
  | /api proxy
  v
ASP.NET Core :5213
  |
  v
EF Core / Npgsql
  |
  v
PostgreSQL/PostGIS container :5432
```
