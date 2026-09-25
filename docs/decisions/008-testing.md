# ADR 008: Test against a real database

**Status:** Accepted

## Decision

- **Unit tests** (xUnit) cover application rules, CWFIS parsing, and Ask Firesight logic.
- **Integration tests** use Testcontainers to start a real PostGIS database in Docker and run
  the actual EF Core migrations. They cover spatial queries, the REST endpoints, and the MCP
  transport.
- **UI tests** (Vitest and Testing Library) cover components and state rules.
- CI runs all three on every push. Nothing deploys unless they pass.

## Why

EF Core's in-memory database can't run PostGIS spatial queries, so tests using it would pass
while the real queries were broken.

## What this means

- Running integration tests locally needs Docker.
- Tests are slower than pure in-memory tests, in exchange for testing what actually runs.
