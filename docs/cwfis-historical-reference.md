# CWFIS historical data

Research notes for a feature that isn't built yet: answering questions about past fires, such
as "what was the average fire size within 50 km of Ottawa over the last two years?". Nothing
here describes how Firesight works today; for that, see [data-sources.md](data-sources.md).

Everything below was checked against the live CWFIS service on 2026-09-12, unless it says
otherwise.

## Where the history is

Firesight already reads current fires from the `cwfif_national_activefires` layer. The history
lives in a sibling layer, `cwfif_national_reportedfires`, on the same server
(`https://geoserver.cwfif.nrcan.gc.ca/geoserver/`).

The two layers have the same fields. The difference is that `reportedfires` keeps every daily
report ever made for every fire, while `activefires` shows only the reports that are current
right now. In September 2026 `reportedfires` held about 352,000 rows.

NRCan is moving everything from its old server (`cwfis.cfs.nrcan.gc.ca`) to this new one.
Build against the new server only.

## Things that will catch you out

- **One fire has many rows.** A fire gets a new row every day it's reported. To count fires or
  average their size, group rows by `national_fire_id` and take one size per fire (the largest,
  or the latest report). Grouping by `id` counts reports, not fires.
- **`-1` means "not reported".** It shows up in `fire_size` (about 1.6% of 2026 rows) and in
  `percent_contained`, `fire_type_ics` and `severity_nearest_dsr`. Leave negative values out of
  any average.
- **Coordinates default to the wrong system.** Without `srsName=EPSG:4326` the server returns
  projected metres (EPSG:3978) instead of latitude and longitude. Always pass it.
- **Status codes are `OC`, `BH`, `UC` and `EX`**, the same as Firesight uses now. Some NRCan
  documentation lists different codes; the live service doesn't use them.
- **There's no fire name**, only IDs and codes.
- **Locations are approximate.** NRCan says precision varies by agency.
- **Prescribed burns are included.** Filter on `fire_was_prescribed` if they shouldn't count.

## Fields that matter

| Field | What it is |
|---|---|
| `national_fire_id` | Identifies a fire across all its reports, for example `2026_BC_2026-K22212`. A string. |
| `id` | Identifies one report, not one fire. |
| `fire_size` | Hectares. `-1` means not reported. |
| `stage_of_control_status` | `OC`, `BH`, `UC` or `EX`. |
| `record_start` | When this report became valid. Use it for date ranges. |
| `status_date` | When the status in this report was recorded. Not the same as `record_start`. |
| `agency_code` | The reporting agency, for example `BC`. |
| `geometry` | The fire's point location. Use this name in spatial filters. |

## Example query

Fires reported within 50 km of Ottawa in the last two years, with unknown sizes left out:

```text
https://geoserver.cwfif.nrcan.gc.ca/geoserver/wfs?service=WFS&version=2.0.0
  &request=GetFeature&typeName=public:cwfif_national_reportedfires
  &outputFormat=application/json&srsName=EPSG:4326
  &sortBy=national_fire_id+A,record_start+A
  &CQL_FILTER=DWithin(geometry,POINT(-75.9219 45.4341),50000,meters)
    AND record_start>='2024-09-11' AND record_start<='2026-09-11' AND fire_size>=0
```

(Shown on several lines for reading; it's one URL.) This returned 30 rows.

The server can filter but can't group, so the one-size-per-fire step happens in Firesight after
the rows come back. For paging, use `count` and `startIndex` with the sort order above; as with
current fires, there's no natural order to page by.

## Recommendation: query it live

Don't copy the history into Firesight's database. A typical question matches a few dozen rows,
there's no rate limit or API key, and the whole layer is only about 352,000 rows. Past reports
don't change, so a short in-memory cache of recent results is fine if the same question is
asked often.

## Other historical sources

The old server also has the **National Burned Area Composite (NBAC)**: one official, corrected
burned area per fire per year, back to 1972. It's more accurate than daily reports, but it's
only published after each fire season ends and is revised in later years. It also hasn't been
confirmed to exist on the new server yet. For a rolling "last two years", `reportedfires` is
the only source that includes the current season.

The data is free, needs no key, and is published under the Open Government Licence – Canada,
which requires crediting Natural Resources Canada wherever it's shown.
