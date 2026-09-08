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
- expose feed synchronization state
- calculate per-fire freshness/staleness
- calculate application-level summaries

Both REST endpoints and MCP tools should reuse this layer.

### Firesight.Domain

Core domain models and domain rules.

This project remains independent of transport, persistence, and external-system concerns. It does not reference EF Core, Npgsql, PostgreSQL, HTTP, OpenAI, or CWFIS. It does use NetTopologySuite geometry types for spatial domain data.

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

## API error boundary

`Firesight.Api` owns translation from application failures to HTTP responses, but it does not own application validation rules.

The Application layer reports expected validation failures through an explicit `ApplicationValidationException` containing field-level errors. The API's centralized `IExceptionHandler` maps that known failure to HTTP 400 validation Problem Details.

Unexpected exceptions are treated as server failures, logged with the ASP.NET Core trace identifier, and returned as generic HTTP 500 Problem Details without internal exception text or stack traces.

Broad runtime exceptions such as `ArgumentOutOfRangeException` are deliberately not classified as client errors globally. This prevents internal programming defects from being misreported as bad requests.

Client-aborted requests are handled separately from server failures so normal cancellation does not pollute error logs as an unhandled HTTP 500.
