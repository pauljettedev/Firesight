# CWFIS Historical Data Reference

**Purpose:** reference for extending Firesight to query CWFIS/CWFIF live for historical wildfire data (no local historical copy).

**Provenance key** — every fact below is tagged so you know how much to trust it:
- **[confirmed-live]** — pulled directly from a live GeoServer response (either a `GetCapabilities` pull, or `DescribeFeatureType`/`GetFeature` results run on 2026-09-12).
- **[confirmed-doc]** — from NRCan's own current documentation (the "CWFIS 2.0 — How to Access CWFIS Data Services" PDF, dated **May 6, 2026**, and the CWFIF data catalogue page).
- **[confirmed-app]** — from Firesight's own existing, working code.
- **[unverified]** — plausible/likely based on the above but not independently confirmed against a live response. Treat as "verify before shipping."

The field schema and code lists below (sections 3 and part of 7) are **[confirmed-live]** — `DescribeFeatureType` and several `GetFeature` calls were run directly against `cwfif_national_reportedfires`. Everything else keeps its original provenance tag.

---

## 1. Overview

- **CWFIS is mid-migration to a new platform called CWFIF** (Canadian Wildland Fire Information Framework). **[confirmed-doc]** NRCan currently runs *two* GeoServer instances side by side:
  - Legacy: `https://cwfis.cfs.nrcan.gc.ca/geoserver/`
  - New (CWFIF): `https://geoserver.cwfif.nrcan.gc.ca/geoserver/` — **this is the one Firesight already targets.**
  NRCan's stated plan is to migrate everything onto the CWFIF instance and retire the legacy one, so build against CWFIF only; don't add the legacy host as a fallback in Firesight.
- **Protocol:** OGC WFS, GeoServer's reference implementation. Advertises WFS **1.0.0, 1.1.0, 2.0.0** (Firesight's existing code already uses 2.0.0, which is right). **[confirmed-live/doc]**
- **Endpoints:** both `/geoserver/wfs?` and `/geoserver/ows?service=WFS&...` work as the WFS entry point; NRCan's own examples use both interchangeably. **[confirmed-doc]**
- **Output formats for WFS:** GeoJSON (`outputFormat=application/json`), CSV (`csv`/`text/csv`), GML 2/3/3.2, KML, `SHAPE-ZIP`. **[confirmed-live]**
- **Default output CRS is EPSG:3978** (NAD83 / Canada Atlas Lambert) if you don't specify `srsName` — confirmed live: a plain `GetFeature` call with no CRS parameter returned `"crs":{"properties":{"name":"urn:ogc:def:crs:EPSG::3978"}}` and coordinates like `[-1667181.53, 626432.70]` (projected meters, not lat/long). **[confirmed-live]** Firesight's existing GeoJSON output is EPSG:4326, so the current code must be passing `srsName=EPSG:4326` (or reprojecting client-side) — carry that same parameter into any new historical query, or you'll get Lambert coordinates instead of lat/long.
- **Auth:** none. **Fees:** `NONE`. **AccessConstraints:** `NONE`. Data is published under the **Open Government Licence – Canada**. **[confirmed-live/doc]**
- **Attribution requirement:** not a technical control, but a licence condition — when you display this data you must credit Natural Resources Canada, e.g.: *"Canadian Forest Service. [year]. Canadian Wildland Fire Information System (CWFIS), Natural Resources Canada, Canadian Forest Service, Northern Forestry Centre, Edmonton, Alberta."* **[confirmed-doc]**
- **Rate limits:** none published anywhere found. GeoServer's own default `CountDefault` is 1,000,000. No API key/token scheme. **[confirmed-live/doc]**
- **Standing disclaimer NRCan attaches to fire data:** it's "estimates based on available data and may not reflect the real-time fire situation." **[confirmed-doc]**

## 2. Available layers

| Layer (`public:` workspace) | Title | Geometry | Temporal coverage | Description |
|---|---|---|---|---|
| `cwfif_national_activefires` | Active Wildland Fires (current) | Point | Rolling "now" snapshot | What Firesight already queries. One row per *currently active* fire, filtered by `record_start <= now < record_end`. **[confirmed-app]** |
| `cwfif_national_reportedfires` | Reported Fires (time series) | Point | Full history of daily agency reports, **352,458 total rows as of 2026-09-12** **[confirmed-live]** | **This is the historical source.** Same underlying feed/schema as `activefires`, but unfiltered — every daily report ever ingested for every fire, active or long since out. `activefires` is `reportedfires` with a `record_start<=now<record_end` filter applied. **[confirmed-doc + confirmed-live]** |
| `nbac` *(legacy server only, as of writing)* | National Burned Area Composite (1972–2024) | MultiPolygon | Annual, finalized, back to 1972 | The authoritative, satellite/agency-reconciled *final* burned-area perimeter and size per fire per year, produced once each fire season ends. Not the live/day-to-day feed — a retrospective, higher-quality dataset updated once a year (see caveats). **[confirmed-live, from legacy `GetCapabilities`; not yet confirmed to exist under this name on the CWFIF instance — check `GetCapabilities` there before relying on it]** |
| `NFDB_point` *(legacy server only, as of writing)* | National Fire Database points — fires ≥ 200 ha (1970–2024) | Point | 1970–2024 | Older, coarser historical point archive (one point per fire, size ≥ 200 ha only). Largely superseded by NBAC after ~1986; useful mainly for very old fires or as a cross-check. **[confirmed-live]** |
| `hotspots` / `hotspots_24h` / `hotspots_last24hrs` | Satellite hotspots | Point | Rolling / near-real-time | Raw satellite thermal detections, not fire records — not useful for "fire size." **[confirmed-live]** |
| `m3polygons` / `m3_polygons_current` | Fire Perimeter Estimate (Fire M3) | Polygon | Current season | Same-day, hotspot-derived perimeter *estimate*, noisier than NBAC, useful only for in-season situational awareness. **[confirmed-live]** |

**For "average fire size within X km, last 2 years"**, `cwfif_national_reportedfires` is the layer to query live. NBAC would be the more authoritative source for *finalized* size once a season is over, but as of writing only appears confirmed to exist on the **legacy** server — see the Recommendation section for why that matters for a rolling 2-year window.

## 3. Field schema

### `cwfif_national_activefires` / `cwfif_national_reportedfires` (same schema) — full field list confirmed live via `DescribeFeatureType`, values confirmed via live `GetFeature` samples

| Field | Type | Nullable? | Description | Known quirks |
|---|---|---|---|---|
| `id` | `long`, nillable | Yes | Internal CWFIF row ID — unique **per report**, not per fire. **[confirmed-live]** | Don't group or dedupe on this — it identifies a single daily report, not a fire. |
| `agency_code` | `string`, nillable | Yes | Two-letter reporting agency code, e.g. `BC`. **[confirmed-live]** | — |
| `region_code` | `string`, nillable | Yes | Sub-agency region/district, e.g. `K2`. **[confirmed-live]** | Not all agencies populate this — expect frequent nulls. |
| `national_fire_id` | **`string`**, nillable | Yes | The key that ties multiple reports together as the *same* fire over its lifetime — e.g. `"2026_BC_2026-K22212"` (looks like `{fire_year}_{agency_code}_{agency_fire_id}`). **[confirmed-live — this is a string, not numeric]** | **This is the field to `GROUP BY`/dedupe on** when computing per-fire statistics like average size — a single fire generates one row per day it's reported, so grouping by `id` instead will silently inflate your fire count. |
| `agency_fire_id` | `string`, nillable | Yes | The fire number as assigned by the reporting agency itself, e.g. `"2026-K22212"`. **[confirmed-live]** | Embedded inside `national_fire_id` (see above) — don't treat them as independent. |
| `national_fire_cause` | `string`, nillable | Yes | Cause code, e.g. `"N"` seen live (Natural?). **[confirmed-live, field exists]** | Enumeration not confirmed — pull distinct values before building a lookup table/label map. |
| `fire_type_ics` | `int`, nillable | Yes | ICS (Incident Command System) fire type code. Saw `-1` live. **[confirmed-live]** | `-1` looks like the same "not reported" sentinel pattern as `fire_size`/`percent_contained` below — treat negative values as missing, not a real code, until confirmed otherwise. |
| `severity_nearest_dsr` | `decimal`, nillable | Yes | Nearest Daily Severity Rating (a CFFDRS fire-weather index) at the fire's location/date. Saw `-1` live. **[confirmed-live]** | Same `-1`-as-sentinel pattern — filter negatives before averaging. |
| `fire_was_prescribed` | `int`, nillable | Yes | Flag for prescribed burns. Saw `0` live (boolean-style int, not `-1`/sentinel). **[confirmed-live]** | Decide up front whether "wildfire" should include prescribed burns for your averages — filter on this if not. |
| `percent_contained` | `decimal`, nillable | Yes | Percent of the fire's perimeter contained. Saw `-1` live. **[confirmed-live]** | Same sentinel pattern again — `-1` = not reported. |
| `fire_size` | `decimal`, nillable | Yes | Size in hectares. **[confirmed-live]** | **`-1` confirmed live as a real, common sentinel** for "not reported/error" — **81 of 5,000 sampled 2026 rows (≈1.6%) had `fire_size < 0`.** Filter these out of any average/sum. |
| `response_type` | `string`, nillable | Yes | Agency's response category, e.g. `"MON"` (monitor) seen live. **[confirmed-live]** | Pull distinct values before using as a filter/label. |
| `stage_of_control_status` | `string`, nillable | Yes | Fire status. **Confirmed live distinct values: `BH`, `EX`, `OC`, `UC`** — this matches the 4-code list Firesight already uses (Being Held / EXtinguished / Out of Control / Under Control), **not** a 3-code `HC/C/M` list that appears in NRCan's French catalogue text. **[confirmed-live]** | The `HC/C/M` text elsewhere in NRCan's own docs does not match what the live service actually returns. |
| `situation_report_date` | `dateTime`, nillable | Yes | Timestamp of the underlying situation report this row was built from. **[confirmed-live]** | Distinct from `status_date` below — saw them differ by several hours in the sample row (`09:35` vs `18:02` same day), so don't assume they're interchangeable. |
| `status_date` | `dateTime`, nillable | Yes | Date/time this row's status was recorded. **[confirmed-live]** | Per-report timestamp, not the fire's overall start — use the earliest `record_start` for a given `national_fire_id` for "when did this fire start." |
| `latitude` / `longitude` | `decimal`, nillable | Yes | Approximate fire location as given by the agency. **[confirmed-live]** | Precision is agency-dependent; NRCan's own text warns it "varies in time and space" — treat as approximate. |
| `geometry` | `gml:GeometryPropertyType`, nillable | Yes | Point geometry. **Property name confirmed as `geometry`** (also echoed as `"geometry_name":"geometry"` in the GeoJSON response). **[confirmed-live]** | Use `geometry` (not `the_geom`) in any `DWithin`/spatial `CQL_FILTER`. |
| `fire_year` | `int`, nillable | Yes | Calendar year of the fire. **[confirmed-live]** | Derived, not always `YEAR(record_start)` per NRCan's docs — don't assume redundancy with a date field. |
| `status_year` | `int`, nillable | Yes | Year the status was last updated. **[confirmed-live]** | Distinct from `fire_year` for fires straddling a year boundary. |
| `record_start` / `record_end` | `dateTime`, nillable | Yes | Validity window for this specific report row. **[confirmed-live]** | Firesight's current `activefires` code filters `record_start<=now<record_end`; for history, filter `record_start` into the target range and **ignore `record_end`**. |

### `nbac` (National Burned Area Composite) — legacy server, field names confirmed live from NRCan's published shapefile metadata

| Field | Type | Description | Quirks |
|---|---|---|---|
| `YEAR` | int | Fire year | — |
| `NFIREID` | int | ID unique per fire *per year* | Not stable across years; a re-ignition next year gets a new ID |
| `GID` | string | `YEAR` + `NFIREID` concatenated — the actually-unique key per record | Use this, not `NFIREID` alone, if joining across years |
| `ADMIN_AREA` | string | Province/territory/Parks Canada code where mapped | — |
| `POLY_HA` | decimal | Raw calculated polygon area (hectares) | This is the *unadjusted* figure |
| `ADJ_HA` | decimal | Area-burned-adjusted hectares | **Use this over `POLY_HA` for "official" size**; `ADJ_FLAG` tells you whether an adjustment was applied |
| `ADJ_FLAG` | bool | Whether the adjustment model was applied | — |
| `BASRC` | string | Data source: `MAFiMS`, `C2C`, `TFMS`, `Agency` | Reliability varies materially by source — see caveats |
| `FIREMAPS` / `FIREMAPM` | string | Detection platform / delineation method | Long enumerated value lists — pull them live if needed |
| `FIRECAUS` | string | `Undetermined` / `Natural` / `Human` | — |
| `HS_SDATE` / `HS_EDATE` | date | First/last satellite hotspot date over the fire's footprint | Null wherever no hotspots were detected |
| `AG_SDATE` / `AG_EDATE` | date | Agency-reported start/end date | Null wherever the agency didn't supply dates |
| `CAPDATE` | date | Acquisition date of the source imagery/survey | — |
| `PRESCRIBED` | bool | Whether the agency reported this as a prescribed burn | Filter explicitly depending on whether prescribed burns should count |
| `NATPARK` | string | National park code, where applicable | — |
| `VERSION` | string | Annual dataset version stamp | NBAC gets revised retroactively (see caveats) |

## 4. Historical/date-range query mechanics

Confirmed-working `CQL_FILTER` syntax against `cwfif_national_reportedfires` (run live on 2026-09-12):

```
https://geoserver.cwfif.nrcan.gc.ca/geoserver/wfs?service=WFS&version=2.0.0&request=GetFeature&typeName=public:cwfif_national_reportedfires&outputFormat=application/json&srsName=EPSG:4326&sortBy=national_fire_id+A,record_start+A&CQL_FILTER=record_start>='2024-09-11' AND record_start<='2026-09-11' AND fire_size>=0
```
(`fire_size>=0` excludes the `-1` "not reported" sentinel at the source instead of client-side. `srsName=EPSG:4326` added so coordinates come back as lat/long, matching the existing `activefires` output, rather than the EPSG:3978 default.)

## 5. Spatial + temporal combined query example

Real, working GET URL for "fires within N km of a lat/long, between date A and date B" — `geometry` and `DWithin` both confirmed against the live service:
```
https://geoserver.cwfif.nrcan.gc.ca/geoserver/wfs?service=WFS&version=2.0.0&request=GetFeature&typeName=public:cwfif_national_reportedfires&outputFormat=application/json&srsName=EPSG:4326&sortBy=national_fire_id+A,record_start+A&CQL_FILTER=DWithin(geometry,POINT(-75.9219 45.4341),50000,meters) AND record_start>='2024-09-11' AND record_start<='2026-09-11' AND fire_size>=0
```
This centers on Ottawa with a 50 km radius; a `resultType=hits` version of this exact filter (no date range, just the last 2 years) returned **30 matching report-rows** live — genuinely small.

Because `fire_size` lives on every daily report row rather than once per fire, computing an *average fire size* requires a client-side group-by: fetch matching rows, group by `national_fire_id`, take `MAX(fire_size)` (or the row with the latest `status_date`) per group, then average across groups. GeoServer's WFS has no server-side `GROUP BY` — only row filtering — so this step happens in Firesight after the fetch.

## 6. Pagination behavior

- WFS 2.0.0 supports `startIndex` + `count` (`ImplementsResultPaging` = `TRUE`). **[confirmed-live]**
- `CountDefault` is 1,000,000 — but specify `count` explicitly rather than relying on the default.
- **No natural sort key exists on `reportedfires`**, same as `activefires`. Use a compound `sortBy=national_fire_id+A,record_start+A` (add `id` as a third tiebreaker if you ever see duplicate `(national_fire_id, record_start)` pairs). Don't paginate on `record_start` alone.

## 7. Data quality caveats

- **`fire_size = -1` is a real, common sentinel** for "not reported/error" — **confirmed live: ~1.6% of 2026 rows so far** (`81 of 5000` sampled). Several other fields (`fire_type_ics`, `severity_nearest_dsr`, `percent_contained`) show the same `-1`-as-sentinel pattern live — treat negative values on any of these as missing data, not real values, wherever they're used.
- **One fire → many rows.** `reportedfires` is a report time series, not a fire registry — a long-lived fire generates a new row every day it's updated. Any per-fire statistic needs an explicit dedupe/group step on `national_fire_id` (confirmed to be a composite string key, not a simple integer).
- **`stage_of_control_status` — resolved.** Live distinct values are `BH`, `EX`, `OC`, `UC`. Ignore the `HC/C/M` list that appears in NRCan's own French catalogue text — it doesn't match what the service actually returns.
- **NBAC is retroactively revised.** NRCan's own changelog documents corrections made as recently as this year's release — polygons, sizes, even fire IDs get corrected, merged, or split in later versions. Don't treat any NBAC-derived number as permanently final.
- **NBAC lags the current season.** It's "updated to current year at the end of each wildfire season" — for a rolling "last 2 years" window computed today, the most recent ~6–12 months (the current season) won't yet be in NBAC. `reportedfires` is the only live source for that recent slice.
- **No fire-name field** exists in the confirmed schema above (only IDs and codes) — matches Firesight's existing `docs/data-sources.md` note that `Name` is left unset rather than inferred.
- **Location precision is agency-dependent and explicitly disclaimed** by NRCan as varying "in time and space" — treat `latitude`/`longitude`/`geometry` as approximate.
- **Snapshot vs. lifecycle semantics differ by layer**: `activefires` = "true right now"; `reportedfires` = every historical report ever made (lifecycle); `nbac` = one finalized record per fire per year (retrospective).
- **Default output CRS is EPSG:3978, not EPSG:4326** — omitting `srsName` will silently give Lambert-projected meters instead of lat/long. Always pass `srsName=EPSG:4326` explicitly for historical queries, matching what the existing `activefires` code must already be doing.

## 8. Recommendation

**Query live, per request — do not build a local historical cache.** This is backed by real numbers, not estimates:

- The full `reportedfires` table is **352,458 rows** nationally, across all agencies and all years in the feed. **[confirmed-live]**
- A realistic single-region query — 2 years, 50 km radius around a city — matched **only 30 rows**. **[confirmed-live]** That's trivial for a live per-request call; there's no volume or latency case for caching this data locally.
- There is no published rate limit, and GeoServer's own default cap is 1,000,000 records — nowhere close to a concern at this scale.
- The data is genuinely historical and immutable at the `reportedfires` level (a report made in 2024 doesn't change), so if repeat network calls for a location/date range a user revisits often should be avoided, an **in-memory or short-TTL cache keyed on `(bbox or center+radius, date range)` inside Firesight** is reasonable — but a persistent local copy of the dataset is unnecessary given how small real query results actually are.
- The one case for caching more seriously: if "average fire size" queries end up running the client-side group-by in section 5 over large result sets repeatedly (e.g., a dashboard recomputing on every page load) — cache the *post-aggregation* result (one row per fire), not the raw report time series.
