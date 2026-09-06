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

CWFIS describes active-fire attributes as including the reporting agency, fire name, coordinates, start date, fire size in hectares, and stage of control. Stage-of-control codes include `OC` (Out of Control), `BH` (Being Held), `UC` (Under Control), and `EX` (Out).

The infrastructure adapter intentionally accepts several likely property-name variants because CWFIS is migrating services from its legacy GeoServer into the CWFIF GeoServer. Geometry coordinates from the GeoJSON feature are preferred over duplicate latitude/longitude attributes.

## Reliability behavior

CWFIS is an upstream public service and may be temporarily unavailable. A failed refresh is logged and does not stop Firesight from serving the last successfully stored dataset.

## Disclaimer

Firesight is a portfolio demonstration and is not an emergency or operational wildfire service. The UI links users to CWFIS and should direct operational decisions to official federal, provincial, territorial, and Parks Canada sources.

## CWFIS 2.0 active-fire schema

Firesight maps the current `public:cwfif_national_activefires` WFS fields directly:

- `national_fire_id` -> `ExternalId` (required; records without it are rejected)
- `agency_code` -> `Agency`
- `fire_size` -> `AreaHectares`
- `stage_of_control_status` -> `Status`
- `status_date` -> `StatusDateUtc`
- `latitude` / `longitude` or GeoJSON point geometry -> location

Firesight does not manufacture fallback wildfire identifiers. The active-fire schema does not expose a fire-name or fire-start-date field, so this importer leaves `Name` and `StartDate` unset. `record_start` and `record_end` are not interpreted as wildfire lifecycle dates.
