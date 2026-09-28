# Data sources

Firesight uses three outside services:

- **CWFIS** for the wildfires themselves
- **Nominatim** to turn a place name like "Kamloops" into a location
- **OpenStreetMap** for the background map

Ask Firesight's answers come from these same sources, through Firesight's own services. Claude
doesn't supply any wildfire data itself ([ADR 007](decisions/007-ask-firesight.md)).

## Wildfires: CWFIS

All wildfire data comes from the Canadian Wildland Fire Information System (CWFIS), run by
Natural Resources Canada ([ADR 002](decisions/002-cwfis.md)). It's free and needs no API key.

| | |
| --- | --- |
| Server | `https://geoserver.cwfif.nrcan.gc.ca/geoserver/ows` (WFS 2.0) |
| Layer | `public:cwfif_national_activefires` |
| Format | GeoJSON, latitude/longitude (EPSG:4326) |
| When | When the API starts, then every hour |
| Settings | `Cwfis` section of `appsettings.json` |

### How a sync works

1. **Pick a moment.** Firesight notes the current time. Every request in this sync asks about
   that same moment, so all the pages describe the same snapshot.
2. **Ask for the fires that are current at that moment.** Despite its name, the active-fires
   layer also holds old rows. Each row is valid for a time window, so Firesight asks only for
   rows where `record_start <= snapshot` and `record_end > snapshot`. These two fields are the
   row's validity window, not when the fire started or ended.
3. **Fetch in pages of 1,000,** sorted by `national_fire_id`, because the layer has no natural
   order to page by. If the pages stop before the total CWFIS said to expect, the sync fails.
4. **Check each fire.** A fire is rejected, logged and counted if it has no `national_fire_id`,
   no usable location, or no properties at all.
5. **Save everything in one go.** New fires are added and existing ones updated. If any request
   failed along the way, nothing is saved and the existing data stays as it was.

Only one sync runs at a time. Each request to CWFIS times out after 30 seconds.

### What's stored for each fire

| CWFIS field | Firesight field | Notes |
| --- | --- | --- |
| `national_fire_id` | `ExternalId` | Required. Identifies the fire across syncs. |
| `agency_code` | `Agency` | The reporting province or agency. `Unknown` if missing. |
| `fire_size` | `AreaHectares` | CWFIS sends `-1` for "not reported". Any negative size is stored as unknown. |
| `stage_of_control_status` | `Status` | Stored exactly as CWFIS sends it. |
| `status_date` | `StatusDateUtc` | When CWFIS last updated the status. If a sync doesn't include one, the last known date is kept. |
| point geometry | `Location` | Falls back to the `latitude`/`longitude` fields if the geometry is missing. |

This layer has no fire name or start date, so `Name` and `StartDate` stay empty rather than
being guessed.

The status codes are `OC` (out of control), `BH` (being held), `UC` (under control) and `EX`
(extinguished). A status can move in either direction, for example from under control back
to out of control, so Firesight never assumes it only moves one way.

### Freshness

Firesight tracks two separate things ([ADR 004](decisions/004-wildfire-freshness.md)):

- **The feed:** when it was last fetched, whether that worked, and how many fires were
  received, accepted and rejected. This is at `/api/wildfires/sync-state`.
- **Each fire:** when it was last seen in the feed (`LastSeenInFeedUtc`). A fire not seen for
  48 hours (`WildfireFreshness:StaleAfterHours`) is marked stale.

A successful fetch doesn't mean every stored fire was in it. Stale only means "Firesight
hasn't seen this fire lately". It never changes the fire's status.

### When a fire drops out of the feed

Firesight doesn't guess why a fire is missing. It keeps the fire with its last known status,
and after 48 hours marks it stale. It stays on the map, marked stale, until CWFIS reports it
again or reports it as extinguished.

### Extinguished fires

When a fire's status becomes `EX`, Firesight records when it first saw that. The fire stays
visible for 7 days (`WildfireRetention:ExtinguishedDays`) from that moment, then is hidden from
the map, the API and MCP. It isn't deleted. If CWFIS later reports it as active again, it
reappears and the 7-day clock starts over.

### When CWFIS is down

A failed sync is logged and recorded in the sync state, and Firesight keeps serving the data
it already has. The next attempt is an hour later.

### What the database is for

The database holds current and recent fires for the app. It isn't a historical archive. Past
fires should be queried live from CWFIS instead; see
[cwfis-historical-reference.md](cwfis-historical-reference.md).

### Licence

CWFIS data is published under the Open Government Licence – Canada, which requires crediting
Natural Resources Canada where the data is shown.

## Place search: Nominatim

Searching for fires near a place ("fires near Kamloops") uses OpenStreetMap's
[Nominatim](https://nominatim.org/) service to find the place's location. Searches are limited
to Canada and take the single best match.

Nominatim's usage policy asks for light use and an identifying app name, so Firesight:

- sends its own name with every request
- limits place searches to 20 per minute per IP address ([api.md](api.md#rate-limits))
- gives up on a search after 10 seconds

## Map background: OpenStreetMap

The map uses OpenStreetMap's public tile server (`tile.openstreetmap.org`), with the required
"© OpenStreetMap contributors" credit shown on the map. That server is meant for light use,
which suits a demo; a site with real traffic would need its own tile provider.
