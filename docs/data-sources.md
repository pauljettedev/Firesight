# Data sources

## CWFIS

All wildfire data comes from the Canadian Wildland Fire Information System (CWFIS), run by
Natural Resources Canada ([ADR 002](decisions/002-cwfis.md)).

| | |
| --- | --- |
| Service | CWFIF GeoServer, WFS 2.0 |
| Layer | `public:cwfif_national_activefires` |
| Format | GeoJSON, lat/long (EPSG:4326) |
| Schedule | On API startup, then every hour |
| Settings | `Cwfis` section of `appsettings.json` |

### Getting only current fires

Despite its name, the active-fires layer also holds old records: each row is valid for a time
window (`record_start` to `record_end`). Firesight asks only for rows valid at the moment the
sync starts:

```text
record_start <= sync time  AND  record_end > sync time
```

These two fields are the row's validity window, not when the fire started or ended.

### Paging

The layer has no key GeoServer can page on by default, so Firesight sorts by
`national_fire_id` and fetches pages with `count` and `startIndex`. This is safe because the
current snapshot has one row per fire. (Checked on 2026-09-12: 480 fires, 480 unique IDs.)

The same sync time is used for every page, so all pages describe the same moment. If any page
fails, the whole sync fails and nothing is saved.

### Field mapping

| CWFIS field | Firesight field | Notes |
| --- | --- | --- |
| `national_fire_id` | `ExternalId` | Required. Features without it are rejected and counted. |
| `agency_code` | `Agency` | |
| `fire_size` | `AreaHectares` | CWFIS sends `-1` for "not reported". Firesight stores it as `null`. |
| `stage_of_control_status` | `Status` | Stored exactly as sent. |
| `status_date` | `StatusDateUtc` | When CWFIS last updated the status |
| point geometry | `Location` | |

CWFIS has no fire name or start date in this layer, so `Name` and `StartDate` stay empty.

Status can move in either direction, for example from "under control" back to "out of
control". Firesight never assumes a status only moves one way.

## Freshness

Firesight tracks two separate things ([ADR 004](decisions/004-wildfire-freshness.md)):

- **Sync state:** when the feed was last fetched, and how many fires were received, accepted,
  and rejected.
- **Per-fire freshness:** when each fire was last seen in the feed (`LastSeenInFeedUtc`). A fire
  not seen for `WildfireFreshness:StaleAfterHours` (default 48) is marked stale.

Stale only means "Firesight hasn't seen this fire lately". It never changes the fire's status,
and a fire missing from the feed isn't assumed to be out.

## Extinguished fires

When a fire's status becomes `EX` (extinguished), Firesight records when it first saw that.
The fire stays visible for `WildfireRetention:ExtinguishedDays` (default 7) and is then hidden
from the API, the map, and MCP. It is not deleted.

## What the database is for

The database holds current and recent fires for the app. It is not a historical archive.
Historical data should be queried live from CWFIS instead. See
[cwfis-historical-reference.md](cwfis-historical-reference.md).

## When CWFIS is down

A failed sync is logged, and Firesight keeps serving the data it already has.

## Nominatim

Place-name search ("fires near Kamloops") uses OpenStreetMap's
[Nominatim](https://nominatim.org/) service, limited to Canada. Requests are rate-limited to
stay within Nominatim's usage policy. See [api.md](api.md#rate-limits).
