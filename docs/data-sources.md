# Data Sources

## Canadian Wildland Fire Information System (CWFIS)

Firesight uses Natural Resources Canada's Canadian Wildland Fire Information System (CWFIS) as its primary wildfire source.

Current implementation:

- Service: CWFIS 2.0 / Canadian Wildland Fire Information Framework (CWFIF) GeoServer
- Protocol: OGC Web Feature Service (WFS) 2.0
- Layer: `public:cwfif_national_activefires`
- Output: GeoJSON (`application/json`)
- Coordinate system requested: EPSG:4326
- Refresh cadence: startup plus once per hour while the API is running

The importer targets the current CWFIS 2.0 active-fire schema rather than attempting to support legacy property-name aliases. GeoJSON point geometry is used for location when available, with the layer's latitude/longitude fields available as the corresponding source coordinates.

## Current-snapshot query

Despite its name, `public:cwfif_national_activefires` is a time-versioned layer containing historical observations as well as the current state. Requesting the layer without a temporal filter returns historical records, including multiple observations for the same `national_fire_id`.

Firesight queries only the records valid at the instant the synchronization begins. The WFS request applies the same validity rule used by the CWFIS interactive map:

```text
record_start <= <snapshot UTC>
AND
record_end > <snapshot UTC>
```

`record_start` and `record_end` describe the validity window of a CWFIS layer record. They are not interpreted as wildfire start or extinguishment dates.

The CWFIS GeoServer layer does not expose a primary key that GeoServer can use for natural-order paging. A paged request using `startIndex` without an explicit sort is rejected by the server. Firesight therefore requests a deterministic manual sort on `national_fire_id` and pages through the filtered current snapshot using `count` and `startIndex`.

A single-field sort on `national_fire_id` is only safe if the filtered current snapshot never contains more than one row per fire — the unfiltered historical feed (`cwfif_national_reportedfires`) does not have this guarantee, since the same fire is legitimately reported on multiple days. Verified against live API output (2026-09-12): a full current-snapshot pull returned 480 active fires with 480 unique `national_fire_id` values — no duplicates within a single snapshot, confirming the single-field sort is safe for this specific query. See `docs/cwfis-historical-reference.md` for the historical-feed pagination requirements this does not apply to.

The snapshot timestamp is captured once per synchronization attempt and reused for every page so that a multi-page fetch cannot drift across different validity instants. If any page fails, the source fetch fails rather than treating a partial set of pages as a successful dataset refresh.

## CWFIS 2.0 active-fire schema

Firesight maps the current `public:cwfif_national_activefires` fields as follows:

- `national_fire_id` -> `ExternalId`
- `agency_code` -> `Agency`
- `fire_size` -> `AreaHectares`
- `stage_of_control_status` -> `Status`
- `status_date` -> `StatusDateUtc`
- `latitude` / `longitude` or GeoJSON point geometry -> location

`national_fire_id` is the required external identity. Firesight does not manufacture fallback wildfire identifiers; a feature without `national_fire_id` is rejected and included in the rejected-feature count for that sync attempt.

`stage_of_control_status` is stored exactly as supplied by CWFIS. Firesight does not map it to a separate application enum. Status is also treated as non-monotonic: a later CWFIS observation may move a fire to either a more-controlled or less-controlled stage, so the application must not impose one-way status-transition rules.

The active-fire schema does not expose a fire-name or fire-start-date field used by this importer. `Name` and `StartDate` are therefore left unset rather than inferred. `record_start` and `record_end` are not interpreted as wildfire lifecycle dates.

`status_date` is stored as `StatusDateUtc` and represents source-level status freshness. It is distinct from the time Firesight last saw the record in a successful feed response.

## Freshness and synchronization

Firesight tracks freshness at two different levels.

### Dataset-level sync state

Each CWFIS fetch records synchronization metadata including:

- last attempt time
- last successful fetch time
- whether the last attempt succeeded
- received feature count
- accepted feature count
- rejected feature count

A successful dataset fetch means the feed request completed and was processed. It does **not** mean that every wildfire already stored by Firesight was refreshed during that fetch.

### Per-fire freshness

Each stored wildfire tracks when Firesight last observed it in an accepted CWFIS feed feature (`LastSeenInFeedUtc`). The API derives `IsStale` from the age of that observation.

The stale threshold is configured with:

```text
WildfireFreshness:StaleAfterHours
```

The current default is 48 hours.

Staleness is a Firesight observation-quality indicator only. It does not modify or reinterpret the raw CWFIS stage-of-control status.

A wildfire not present in a later feed is not treated as having undergone a particular CWFIS status transition merely because it was absent. This prevents feed completeness and record lifecycle from being conflated.

## Local data role

The PostgreSQL/PostGIS database is a current/recent-state operational store for the demo, not a complete historical wildfire archive. Historical CWFIS data, if added to the product, should be queried from an appropriate historical source rather than inferred from repeated active-fire snapshots.

## Reliability behavior

CWFIS is an upstream public service and may be temporarily unavailable. A failed refresh is logged and does not stop Firesight from serving the last successfully stored data.

## Disclaimer

Firesight is a portfolio demonstration and is not an emergency or operational wildfire service. The UI should link users to CWFIS and direct operational decisions to official federal, provincial, territorial, and Parks Canada sources.
