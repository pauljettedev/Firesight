# ADR 005: Layered backend shared by the REST API and MCP

**Status:** Accepted

## Decision

Split the backend into layers, each allowed to depend only on the ones below it:

- **Domain:** core models such as `Wildfire`. No database, HTTP, or AI code.
- **Application:** the use cases (get fires, find fires nearby, sync state) and validation.
- **Infrastructure:** the database, CWFIS client, geocoding, and Claude client.
- **Api** and **Mcp:** thin entry points. Both call the same Application services.

## Why

The REST API and MCP answer the same questions. If each had its own logic, the two would
drift apart. Keeping the logic in one layer means it's written and tested once.

## What this means

- Endpoints and MCP tools only translate input and output. No business or spatial logic lives
  there, and they never talk to the database directly.
- Production and the MCP tests register tools through the same `WithFiresightTools()` call, so
  the tests can't drift from what's deployed.

Details: [architecture.md](../architecture.md).
