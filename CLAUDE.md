# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Firesight is a portfolio project (not an operational wildfire service) demonstrating full-stack architecture: a React/TypeScript map frontend, ASP.NET Core REST API, layered Application/Domain/Infrastructure backend, PostgreSQL/PostGIS spatial storage, CWFIS wildfire data ingestion, and MCP tools that reuse the same application services as the REST API.

## Commands

### Backend (.NET 10)

```powershell
docker compose up -d                       # start PostgreSQL/PostGIS (required for API startup/migrations)
dotnet build Firesight.slnx                # build everything
dotnet run --project src/Firesight.Api     # run API at http://localhost:5213
dotnet test Firesight.slnx                 # run all unit + integration tests
dotnet test tests/Firesight.UnitTests/Firesight.UnitTests.csproj
dotnet test tests/Firesight.IntegrationTests/Firesight.IntegrationTests.csproj
dotnet test --filter "FullyQualifiedName~WildfireServiceTests.MethodName"  # single test
```

Integration tests use Testcontainers to spin up a real disposable PostGIS-backed Postgres and apply actual EF Core migrations — Docker must be running. They exercise the real PostGIS spatial path, not the EF Core in-memory provider.

### Frontend (`src/Firesight.Web`)

```powershell
npm run dev      # Vite dev server at http://localhost:5173, proxies /api to :5213
npm run lint
npm run build    # tsc -b && vite build
```

### CI (`.github/workflows/ci.yml`)

Mirrors the above: `dotnet restore/build/test` on `Firesight.slnx`, and `npm ci && npm run lint && npm run build` in `src/Firesight.Web`.

## Architecture

Layered dependency direction (domain at the bottom, both API and MCP sit on top of Application):

```text
Api ------------> Application ------------> Domain
 |                    ^
 |                    |
 +--> Infrastructure--+

Mcp ------------> Application
 |
 +--------------> Infrastructure
```

- **Firesight.Domain** — core models only (e.g. `Wildfire`). No EF Core, Npgsql, HTTP, Claude/Anthropic, or CWFIS references. Uses NetTopologySuite geometry types for spatial data.
- **Firesight.Application** — use cases/orchestration (`WildfireService`, `IAskFiresightService` etc.), DTOs, validation. Both the REST API and MCP tools must reuse this layer rather than duplicating business or spatial-query logic.
- **Firesight.Infrastructure** — EF Core, Npgsql, PostGIS, CWFIS client, Nominatim geocoding, Claude (Anthropic) client for Ask Firesight, hosted sync background service, persistence implementations.
- **Firesight.Api** — ASP.NET Core minimal API endpoints, DTO mapping, centralized error handling. Business logic must not live here.
- **Firesight.Mcp** — MCP tool definitions that delegate to Application services; must not duplicate business/spatial logic or talk to the database directly.
- **Firesight.Web** — React/TypeScript/MUI/MapLibre frontend; Vite proxies `/api` to the backend in dev.

### API error handling

`Firesight.Api` owns translating failures to HTTP responses but not application validation rules. `Firesight.Application` throws `ApplicationValidationException` (field-level errors) for expected validation failures, which the API's central `IExceptionHandler` maps to `400` Problem Details. Unexpected exceptions become generic `500` Problem Details (no internal exception text/stack traces), correlated via the ASP.NET Core trace id. Broad exception types like `ArgumentOutOfRangeException` are deliberately *not* globally treated as client errors — only the explicit `ApplicationValidationException` maps to 400. Client-aborted requests are handled separately (`499`) so cancellation doesn't pollute error logs as 500s.

### CWFIS data ingestion (see `docs/data-sources.md` for full detail)

- Source: CWFIS 2.0 WFS layer `public:cwfif_national_activefires`, GeoJSON, EPSG:4326. This layer is time-versioned, so Firesight requests only records valid at the sync's captured snapshot instant (`record_start <= snapshot < record_end`), reusing the same snapshot timestamp across all pages of one sync attempt.
- The layer has no natural-order primary key for paging, so requests use a deterministic manual sort on `national_fire_id` with `count`/`startIndex`. If any page fails, the whole sync attempt fails rather than partially applying.
- `national_fire_id` is the required external identity; a feature without it is rejected (counted, not defaulted to a generated id).
- `stage_of_control_status` is persisted verbatim from CWFIS and treated as non-monotonic — never impose one-way status-transition rules on it, and never reinterpret it based on staleness or feed absence.
- The active-fire schema has no fire-name/start-date field; `Name`/`StartDate` stay unset rather than inferred.

### Freshness model — two independent concepts, do not conflate

- **Dataset-level sync state** (`IWildfireSyncStateRepository`): last attempt/success time, received/accepted/rejected counts for a CWFIS fetch. A successful fetch does *not* imply every stored wildfire was refreshed in it.
- **Per-fire freshness** (`Wildfire.LastSeenInFeedUtc`): when a specific fire was last seen in an *accepted* feed feature. `IsStale` is derived from this age against `WildfireFreshness:StaleAfterHours` (default 48h). Staleness is a Firesight observation-quality signal only — it never modifies the raw CWFIS status, and a fire's absence from a feed is never treated as an implied status transition.

### MCP (see `docs/mcp.md`)

MCP is a separate interface from the REST API but both call the same `Firesight.Application` services — never duplicate business/spatial logic in `Firesight.Mcp`. Production and the transport-level integration tests both register tools via the same `WithFiresightTools()` extension so the test host can't drift from production registration. Nullable output schema fields use explicit `anyOf` branches (not JSON Schema `["string","null"]` type arrays) for MCP client interoperability — this override is limited to the schema boundary and doesn't change application DTOs.

### Ask Firesight (natural-language query feature)

`ClaudeAskFiresightService` (`Firesight.Infrastructure/Claude`) implements `IAskFiresightService` using the Claude API (Messages API, tool use) — configured via the `Claude:Model`/`Claude:ApiKey` options (`ClaudeOptions`). It runs an agentic tool-call loop (max 4 rounds) over the same five Firesight tools MCP exposes (`geocode_location`, `get_active_wildfires`, `get_wildfire_by_external_id`, `find_wildfires_near_location`, `get_feed_sync_state`), calling straight through to `IWildfireService`/`ILocationGeocoder` — never the database directly. For count/exists/status questions, the *application code* computes the deterministic answer from tool output rather than trusting the model to count or state status itself; the model's role is limited to picking the right tool and `responseMode`. `AnthropicClient` is registered as a singleton in DI (it's a stateless, thread-safe API client — do not switch it back to scoped/per-request).

## Documentation map

- `docs/architecture.md` — layering/dependency rules in full
- `docs/api.md` — REST endpoint contracts
- `docs/data-sources.md` — CWFIS ingestion, schema mapping, freshness semantics
- `docs/mcp.md` — MCP tool contracts and design rules
- `docs/decisions/*.md` — ADRs (stack choice, CWFIS, PostGIS, freshness model)
