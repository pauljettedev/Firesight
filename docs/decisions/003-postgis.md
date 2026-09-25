# ADR 003: PostgreSQL with PostGIS

**Status:** Accepted

## Decision

Store data in PostgreSQL with the PostGIS extension, accessed through EF Core, Npgsql, and
NetTopologySuite. Spatial questions like "fires within 50 km of here" are answered by the
database.

## Why

Wildfire data is about places. PostGIS already does distance searches, spatial indexes, and
shape intersections well. Doing that by hand with latitude/longitude columns would be slower
and easy to get wrong.

## What this means

- Every environment (local, CI, production) needs PostGIS, not plain PostgreSQL.
- Spatial logic is tested against a real PostGIS database (see ADR 008).
