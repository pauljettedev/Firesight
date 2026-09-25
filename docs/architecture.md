# Architecture

Firesight is one backend application split into layers, plus a React frontend.

```text
 Browser (React)          AI assistant
       |                       |
       | REST /api             | MCP /mcp
       v                       v
  Firesight.Api          Firesight.Mcp
       \                     /
        v                   v
       Firesight.Application  ------>  Firesight.Domain
                 ^
                 | implemented by
       Firesight.Infrastructure
          |       |        |        |
          v       v        v        v
     PostGIS   CWFIS   Nominatim  Claude
```

`Firesight.Api` hosts everything in one process: the REST endpoints, the MCP endpoint, the
hourly CWFIS sync, and (in production) the built React app.

## Projects

| Project | What it does | What it must not do |
| --- | --- | --- |
| `Firesight.Domain` | Core models, such as `Wildfire` | Reference the database, HTTP, CWFIS, or Claude |
| `Firesight.Application` | Use cases (list fires, find fires nearby, sync state) and input validation | Know how data is stored or fetched |
| `Firesight.Infrastructure` | Database access, CWFIS client, Nominatim geocoding, Claude client, the hourly sync | |
| `Firesight.Api` | REST endpoints, error handling, rate limits | Contain business rules |
| `Firesight.Mcp` | MCP tool definitions | Contain business rules or query the database |
| `Firesight.Web` | The React map UI | |

The REST API and MCP both call the same Application services, so rules are written once
([ADR 005](decisions/005-layered-backend.md)). Error handling is in
[ADR 009](decisions/009-api-errors.md).

## Frontend

Each UI element is its own component ([ADR 006](decisions/006-frontend-components.md)):

```text
src/Firesight.Web/src/
├── components/   UI components (the map has its own folder)
├── services/     calls to the API
├── state/        rules for how user actions change the map
└── utils/        formatting and calculation helpers
```

## Local development

```text
Browser -> Vite :5173 --/api--> API :5213 -> PostGIS container :5432
```

Production runs differently. See [deployment.md](deployment.md).
