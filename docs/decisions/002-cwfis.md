# ADR 002: CWFIS as the primary wildfire source

## Status

Accepted

## Decision

Use the Canadian Wildland Fire Information System (CWFIS) as the primary wildfire data source for Firesight.

## Context

Firesight requires national Canadian wildfire information suitable for mapping and later spatial analysis.

CWFIS provides national wildfire information and geospatial datasets and is operated by Natural Resources Canada.

## Consequences

- Firesight can focus initially on one national source rather than many provincial integrations.
- The application should map CWFIS data into its domain model without inventing missing source values.
- `national_fire_id` is the required external identity; features without it are rejected rather than assigned fallback IDs.
- CWFIS stage-of-control codes are retained exactly as supplied and are not converted to a Firesight-specific status enum.
- The UI must clearly state that Firesight is a demo and not an official emergency information source.
- Official CWFIS and provincial/territorial information should be linked prominently.
