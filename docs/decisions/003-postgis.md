# ADR 003: PostgreSQL with PostGIS

## Status

Accepted

## Decision

Use PostgreSQL with PostGIS for persistence and spatial queries.

Use:
- Entity Framework Core
- Npgsql
- NetTopologySuite

## Context

Wildfire data is inherently geographic.

Likely operations include:
- storing fire locations
- storing fire perimeters
- distance queries
- nearby-fire searches
- region intersection queries
- spatial indexing

Implementing this manually with latitude/longitude columns would duplicate mature GIS functionality.

## Consequences

Positive:
- industry-standard spatial database capabilities
- natural support for MapLibre-backed geographic features
- efficient spatial querying and indexing
- useful experience with PostgreSQL/PostGIS

Tradeoff:
- local and hosted environments must support the PostGIS extension
