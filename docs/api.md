# API

Development base URL:

```text
http://localhost:5213
```

## Health

```http
GET /api/health
```

Returns API and PostgreSQL connectivity status.

## Wildfires

```http
GET /api/wildfires
```

Returns the current/recent wildfire records stored in PostgreSQL/PostGIS.

Coordinates are exposed as latitude/longitude for the web client while persistence uses a PostGIS geography point.

Relevant freshness fields include:

- `statusDateUtc` - source-level status timestamp from CWFIS `status_date`
- `lastSeenInFeedUtc` - when Firesight last observed the wildfire in an accepted feed feature
- `isStale` - derived from `lastSeenInFeedUtc` and the configured stale threshold

`isStale` describes Firesight's observation freshness only. It does not alter the raw CWFIS stage-of-control status.

## Wildfires within a radius

```http
GET /api/wildfires/near?latitude=45.4215&longitude=-75.6972&radiusKm=25
```

Returns wildfire records whose PostGIS geography point falls within the requested radius, ordered nearest first.

Each result contains:

- `wildfire` - the normal wildfire DTO
- `distanceKm` - distance from the supplied point in kilometres

Query constraints:

- `latitude`: finite value from `-90` to `90`
- `longitude`: finite value from `-180` to `180`
- `radiusKm`: finite value greater than `0`

Invalid values return `400 Bad Request` using a standard validation Problem Details response. Validation rules and messages originate in the Application layer; the API only translates the application validation failure into HTTP. The spatial query uses the indexed PostGIS geography column rather than loading records and calculating distances in application memory.

## Refresh CWFIS data

```http
POST /api/wildfires/sync
```

Triggers an immediate pull from the CWFIS 2.0 WFS active-fire layer and upserts accepted records by `national_fire_id` / `ExternalId`.

Features without `national_fire_id` are rejected rather than assigned a generated identifier.

The API also performs a refresh at startup and then hourly. Refresh failures are non-fatal; existing stored data remains available.

## CWFIS feed sync state

```http
GET /api/wildfires/sync-state
```

Returns dataset-level CWFIS fetch metadata, including:

- last attempt time
- last successful fetch time
- whether the last attempt succeeded
- received feature count
- accepted feature count
- rejected feature count

These values describe the dataset fetch. They do not imply that every stored wildfire was present or individually refreshed during that fetch.

Detailed sync failure messages may be retained internally for diagnostics but are not included in the public sync-state response.

## API error handling

API exceptions are handled centrally through ASP.NET Core `IExceptionHandler` and Problem Details.

Current mappings:

- Application validation failures -> `400 Bad Request` with validation Problem Details
- unexpected exceptions -> `500 Internal Server Error` with a generic Problem Details body
- client-aborted requests -> `499 Client Closed Request` for server-side logging/status purposes

Unexpected exception messages and stack traces are not returned to clients. Problem Details responses include the ASP.NET Core request `traceId` so a client-visible error can be correlated with server logs. Empty framework-generated error responses, such as a missing required query parameter or an unmatched route, are also filled by status-code middleware using Problem Details.

The API does not infer client errors from broad framework exception types such as `ArgumentOutOfRangeException`; only the explicit Application validation exception is mapped to HTTP 400.
