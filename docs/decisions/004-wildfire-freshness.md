# ADR 004: Separate feed synchronization from wildfire freshness

## Status

Accepted

## Decision

Firesight will represent dataset synchronization freshness separately from the freshness of an individual wildfire record.

Dataset-level state records when the CWFIS feed was attempted and successfully fetched. Each wildfire independently records when Firesight last observed that fire in an accepted feed feature.

The application derives a stale indicator from per-fire observation age. The current default stale threshold is 48 hours and is configurable through `WildfireFreshness:StaleAfterHours`.

CWFIS `stage_of_control_status` is persisted exactly as supplied and is not changed when a Firesight record becomes stale.

## Context

A successful CWFIS request does not guarantee that every previously known fire appears in that response. Treating the feed timestamp as the update time for every stored fire would therefore overstate data freshness.

Likewise, absence from a feed is not sufficient evidence that a fire has moved to a particular lifecycle or stage-of-control state.

## Consequences

- The UI can distinguish "the feed was fetched recently" from "this individual fire was observed recently."
- Older stored records can be marked stale without inventing a CWFIS status.
- Feed completeness problems do not silently rewrite wildfire lifecycle state.
- Synchronization and status semantics remain independently testable.
