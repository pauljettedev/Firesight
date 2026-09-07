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
