# Architecture

Firesight is one .NET backend, split into layers, plus a React frontend.

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

Everything runs in one process, started by `Firesight.Api`: the REST endpoints, the MCP
endpoint, the hourly CWFIS sync, and in production the built React app as well.

## Why it's layered

The REST API, the MCP server and Ask Firesight all need the same wildfire logic. Keeping that
logic in one place, below all three, means it's written and tested once, and none of them can
drift from the others ([ADR 005](decisions/005-layered-backend.md)).

Application says *what* it needs, as interfaces: a place to store fires
(`IWildfireRepository`), a source of fires (`IWildfireSource`), a place-name lookup
(`ILocationGeocoder`) and a way to answer questions (`IAskFiresightService`). Infrastructure
provides the real versions: PostgreSQL, CWFIS, Nominatim and Claude. Application never
depends on those directly, so tests can swap in stand-ins, and a data source can be replaced
without touching the logic.

## Projects

| Project | What it does | What it must not do |
| --- | --- | --- |
| `Firesight.Domain` | The `Wildfire` model and the wildfire rules (`WildfireRules`) | Know about the database, HTTP, CWFIS or Claude |
| `Firesight.Application` | The use cases (list fires, find fires nearby, sync with CWFIS, sync state) and input checks | Know how data is stored or where it comes from |
| `Firesight.Infrastructure` | Database access, the CWFIS and Nominatim clients, Ask Firesight's Claude code, the hourly sync | Make up its own business rules; it applies the ones in Domain |
| `Firesight.Api` | REST endpoints, turning errors into HTTP responses ([ADR 009](decisions/009-api-errors.md)), rate limits | Contain business rules |
| `Firesight.Mcp` | The MCP tools | Contain business rules or touch the database. It only references Application. |
| `Firesight.Web` | The React map UI | |

## Where the rules live

Rules that decide which fires to return, such as "hide fires extinguished more than 7 days
ago", are written once in Domain as expressions (`Expression<Func<Wildfire, bool>>`, for
example `WildfireRules.IsWithinRetention`). Repositories add them to queries with `.Where(...)`
instead of repeating the condition. EF Core turns the expression into SQL, so the database
still does the filtering, and the rule can be unit tested against a plain list without a
database.

Rules about a single fire that's already loaded are methods on the model, for example
`Wildfire.IsStale` and `Wildfire.RecordStatus`.

## Frontend

Each part of the UI is its own component ([ADR 006](decisions/006-frontend-components.md)):

```text
src/Firesight.Web/src/
├── components/   UI components; the map has its own folder
├── services/     calls to the API, through one shared request helper (http.ts)
├── state/        how user actions change what the map shows
└── utils/        formatting and calculation helpers
```

The map background comes straight from OpenStreetMap's tile server. Everything else the
frontend shows comes from the Firesight API.

## Tests

- `tests/Firesight.UnitTests`: rules, services and parsing, with no database
- `tests/Firesight.IntegrationTests`: the real API, spatial queries and migrations against a
  temporary PostGIS database, and the MCP tools through a real MCP client
- UI tests sit next to the code they test (`*.test.ts` and `*.test.tsx`)

Why they're split this way: [ADR 008](decisions/008-testing.md).

## Local development

```text
Browser -> Vite :5173 --/api--> API :5213 -> PostGIS container :5432
```

Production runs differently; see [deployment.md](deployment.md).
