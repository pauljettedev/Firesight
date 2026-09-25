# ADR 002: CWFIS as the wildfire data source

**Status:** Accepted

## Decision

Use the Canadian Wildland Fire Information System (CWFIS), run by Natural Resources Canada, as
the only wildfire data source. Store its data as-is and never fill in values it doesn't provide.

## Why

CWFIS covers all of Canada in one feed, so Firesight doesn't need a separate integration for
each province.

## What this means

- `national_fire_id` is required. A fire without one is rejected and counted, not given a
  made-up ID.
- The CWFIS stage-of-control status is stored exactly as sent. Firesight has no status enum of
  its own and never assumes a status can only move one way.
- CWFIS has no fire name or start date in this feed, so those stay empty.
- A sync is all or nothing: if any page of the feed fails, nothing from that sync is saved.
- The UI says clearly that Firesight is a demo, not an official emergency source, and links to
  official sources.

Details: [data-sources.md](../data-sources.md).
