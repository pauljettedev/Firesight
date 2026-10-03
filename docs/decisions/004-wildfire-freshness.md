# ADR 004: Feed freshness and fire freshness are tracked separately

**Status:** Accepted

## Decision

Track two separate things:

- **Feed sync state:** when Firesight last tried and last succeeded in fetching the CWFIS feed,
  and how many records were received, accepted, and rejected.
- **Per-fire freshness:** when each fire was last seen in the feed (`LastSeenInFeedUtc`). A fire
  is marked stale after 48 hours by default (`WildfireFreshness:StaleAfterHours`).

## Why

A successful fetch doesn't mean every stored fire was in it. Using the fetch time as every
fire's update time would make old data look current. A fire missing from the feed also doesn't
tell us its status changed.

## What this means

- The UI can show "the feed was checked recently" separately from "this fire was seen recently".
- Stale is a Firesight data-quality flag only. It never changes the CWFIS status.

## Update, October 2026: hide fires missing for 5 days

We first kept missing fires on the map until CWFIS reported them as extinguished (`EX`). Live
data showed the active-fires layer rarely does that. On October 3, 2026, none of the 421 fires
Firesight showed were `EX`, and 138 hadn't been in the feed for over a week, most still listed
as out of control. CWFIS was dropping fires instead of marking them out, so the map filled up
with fires that were probably out.

Fires missing from the feed for 5 days (`WildfireRetention:MissingFromFeedDays`) are now
hidden, counted from the last successful sync so a CWFIS outage doesn't hide everything. Their
status still isn't changed, and they reappear if CWFIS reports them again. `EX` is still handled
the same way as before ([data-sources.md](../data-sources.md)).
