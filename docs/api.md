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

## Active wildfires

```http
GET /api/wildfires
```

Returns the current/recent wildfire records stored in PostgreSQL/PostGIS. Coordinates are exposed as latitude/longitude for the web client while persistence uses a PostGIS geography point. Each record includes `lastSeenInFeedUtc` and a derived `isStale` flag. The stale flag is based on Firesight observation age only and does not alter the CWFIS stage-of-control code.

## Refresh CWFIS data

```http
POST /api/wildfires/sync
```

Triggers an immediate pull from the CWFIS 2.0 WFS active-fire layer and upserts records by stable external identifier.

The API also performs a refresh at startup and then hourly. Refresh failures are non-fatal; existing stored data remains available.

## CWFIS feed sync state

```http
GET /api/wildfires/sync-state
```

Returns dataset-level CWFIS fetch metadata, including the last attempt, the last successful fetch, whether the last fetch attempt succeeded, and received/accepted/rejected feature counts. These timestamps describe the feed fetch and do not imply that every stored wildfire was updated at that time.
