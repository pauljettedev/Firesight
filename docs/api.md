# API

All endpoints return JSON. Local base URL: `http://localhost:5213`.

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/api/health` | API and database status |
| GET | `/api/wildfires` | All current fires |
| GET | `/api/wildfires/near` | Fires within a distance of a point |
| GET | `/api/wildfires/sync-state` | When CWFIS was last synced |
| GET | `/api/locations/geocode` | Look up a Canadian place name |
| POST | `/api/ask` | Ask Firesight a question |

The MCP endpoint is at `/mcp`. See [mcp.md](mcp.md).

## GET /api/wildfires

Returns every current fire. Each fire has:

| Field | Meaning |
| --- | --- |
| `id` | Firesight's ID |
| `externalId` | CWFIS `national_fire_id` |
| `agency` | Reporting agency code, e.g. `BC` |
| `latitude`, `longitude` | Location |
| `areaHectares` | Size, or `null` if CWFIS didn't report it |
| `status` | CWFIS stage of control, exactly as sent (`OC`, `BH`, `UC`, `EX`) |
| `statusDateUtc` | When CWFIS last updated the status |
| `lastSeenInFeedUtc` | When Firesight last saw this fire in the feed |
| `isStale` | `true` if not seen in the feed for 48 hours (configurable) |
| `name`, `startDate` | Always `null`: CWFIS doesn't provide them |

Extinguished fires are hidden 7 days after Firesight first sees them as extinguished. See
[data-sources.md](data-sources.md).

## GET /api/wildfires/near

```http
GET /api/wildfires/near?latitude=45.42&longitude=-75.70&radiusKm=25
```

| Parameter | Allowed values |
| --- | --- |
| `latitude` | -90 to 90 |
| `longitude` | -180 to 180 |
| `radiusKm` | more than 0, up to 1000 |

Returns fires nearest first, each as `{ wildfire, distanceKm }`.

## GET /api/wildfires/sync-state

The last CWFIS sync: when it was attempted, when it last succeeded, whether it succeeded, and
how many fires were received, accepted, and rejected. A successful sync doesn't mean every
stored fire was in it. That's what each fire's `lastSeenInFeedUtc` is for.

## GET /api/locations/geocode

```http
GET /api/locations/geocode?query=Kamloops
```

Returns `{ displayName, latitude, longitude }` for the best match in Canada, or `404` if
nothing matches. Uses OpenStreetMap's Nominatim service.

## POST /api/ask

```json
{ "question": "Are there any out-of-control fires near Kamloops?" }
```

`question` is required, up to 500 characters. Returns:

- `answer`: the answer text
- `toolsUsed`: which Firesight tools were called to answer it
- `mapContext`: a point and radius to show on the map, or `null`
- `wildfireExternalIds`: CWFIS IDs of the fires the answer names, in the order it names them.
  Only fires that Firesight's own tools returned are included.

How it works: [ADR 007](decisions/007-ask-firesight.md).

## Rate limits

Limits are per IP address. Going over returns `429 Too Many Requests`.

| Endpoint | Limit | Why |
| --- | --- | --- |
| `/api/ask` | 10 per minute | Each question costs money in Claude API usage |
| `/api/locations/geocode` | 20 per minute | Keeps Firesight within Nominatim's usage policy |

## Errors

Errors use the standard Problem Details format, with a `traceId` to match server logs.

| Status | When |
| --- | --- |
| `400` | Invalid input. The response lists the errors by field. |
| `404` | Not found |
| `429` | Rate limit exceeded |
| `500` | Server error. No internal details are included. |

Why only some errors become `400`: [ADR 009](decisions/009-api-errors.md).
