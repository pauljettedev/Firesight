# API

The REST API behind the Firesight web app. All endpoints return JSON, and none need a login.

| | |
| --- | --- |
| Live | `https://firesight.codewheel.ca` |
| Local | `http://localhost:5213` |

| Method | Path | What it does |
| --- | --- | --- |
| GET | `/api/wildfires` | Every fire the map shows |
| GET | `/api/wildfires/near` | Fires within a distance of a point |
| GET | `/api/wildfires/sync-state` | How the last CWFIS sync went |
| GET | `/api/locations/geocode` | Finds a place in Canada by name |
| POST | `/api/ask` | Asks Firesight a question in plain English |
| GET | `/api/health` | Whether the API is up and can reach its database |

AI assistants can use the same data through the MCP server at `/mcp` ([mcp.md](mcp.md)).

## GET /api/wildfires

Returns every fire the map shows, largest first. That's the current fires, including stale
ones, and extinguished fires for 7 days after Firesight first saw them as extinguished
([data-sources.md](data-sources.md)).

Each fire has:

| Field | Meaning |
| --- | --- |
| `id` | Firesight's ID for the fire |
| `externalId` | The CWFIS `national_fire_id`, for example `2026_BC_2026-K22212` |
| `agency` | The reporting province or agency, for example `BC` |
| `latitude`, `longitude` | Location |
| `areaHectares` | Size in hectares, or `null` if CWFIS didn't report it |
| `status` | Stage of control, exactly as CWFIS sent it: `OC` (out of control), `BH` (being held), `UC` (under control) or `EX` (extinguished) |
| `statusDateUtc` | When CWFIS last updated the status |
| `lastSeenInFeedUtc` | When Firesight last saw this fire in the CWFIS feed |
| `isStale` | `true` if the fire hasn't been in the feed for 48 hours |
| `name`, `startDate` | Always `null`. CWFIS doesn't provide them. |

## GET /api/wildfires/near

```http
GET /api/wildfires/near?latitude=45.42&longitude=-75.70&radiusKm=25
```

| Parameter | Allowed values |
| --- | --- |
| `latitude` | -90 to 90 |
| `longitude` | -180 to 180 |
| `radiusKm` | Above 0, up to 1,000 |

Returns the fires within the radius, nearest first. Each result is
`{ wildfire, distanceKm }`, where `wildfire` has the same fields as above.

## GET /api/wildfires/sync-state

How the last sync with CWFIS went:

| Field | Meaning |
| --- | --- |
| `lastAttemptUtc` | When the last sync started |
| `lastSuccessfulFetchUtc` | When the feed was last fetched successfully, or `null` if never |
| `lastAttemptSucceeded` | Whether the last sync worked |
| `received`, `accepted`, `rejected` | How many fires the last successful fetch returned, kept and rejected |

Returns `204 No Content` if no sync has run yet.

A successful sync doesn't mean every stored fire was in it. Each fire's `lastSeenInFeedUtc`
shows when that fire was last in the feed.

## GET /api/locations/geocode

```http
GET /api/locations/geocode?query=Kamloops
```

Returns `{ displayName, latitude, longitude }` for the best match in Canada, or `404` if
nothing matches. `query` is required. The search uses OpenStreetMap's Nominatim service.

## POST /api/ask

```json
{ "question": "Are there any out-of-control fires near Kamloops?" }
```

`question` is required and can be up to 500 characters. The response has:

| Field | Meaning |
| --- | --- |
| `answer` | The answer text |
| `toolsUsed` | Which of Firesight's data lookups were used to answer it |
| `mapContext` | `{ latitude, longitude, radiusKm, label }` for the area the answer searched, or `null` |
| `wildfireExternalIds` | The CWFIS IDs of the fires the answer is about, in display order. Only fires that Firesight's own lookups returned are included. |

How Ask Firesight works: [ADR 007](decisions/007-ask-firesight.md).

## GET /api/health

Returns `{ "status": "ok", "database": "connected" }`. `database` is `unavailable` if the API
can't reach PostgreSQL.

## Rate limits

Only the two endpoints that call an outside service are limited, per IP address. Going over
the limit returns `429 Too Many Requests`.

| Endpoint | Limit | Why |
| --- | --- | --- |
| `/api/ask` | 10 per minute | Each question is sent to Claude, and every call costs money |
| `/api/locations/geocode` | 20 per minute | Each search is sent to Nominatim, a free service that blocks apps sending too many requests. If it blocked Firesight, place search would stop working for everyone. |

The other endpoints, and the MCP server, aren't limited. They only read Firesight's own
database, which costs nothing per request and doesn't depend on anyone else's rules.

## Errors

Errors use the standard Problem Details format, with a `traceId` that matches the server logs.

| Status | When |
| --- | --- |
| `400` | Invalid input. The response lists the problem with each field. |
| `404` | Not found |
| `429` | Rate limit exceeded |
| `500` | Something went wrong on the server. The response has no internal details. |

Why only some errors become `400`: [ADR 009](decisions/009-api-errors.md).
