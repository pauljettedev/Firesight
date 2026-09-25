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
