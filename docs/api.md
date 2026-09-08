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

Invalid values return `400 Bad Request` using a validation-problem response with a stable parameter-specific error message. The spatial query uses the indexed PostGIS geography column rather than loading records and calculating distances in application memory.

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

## API error handling status

Radius-search validation currently maps application-level argument validation to an HTTP validation-problem response at the endpoint. A centralized API exception-handling and Problem Details policy is still planned before the HTTP surface grows significantly.
