# MCP

Firesight has an MCP (Model Context Protocol) server, so AI assistants can query its wildfire
data directly. It runs inside the API at `/mcp` over HTTP, with no login and no session
state.

Local URL: `http://localhost:5213/mcp`

## Tools

| Tool | Inputs | Returns |
| --- | --- | --- |
| `get_active_wildfires` | none | All current fires |
| `get_wildfire_by_external_id` | `externalId` (CWFIS `national_fire_id`) | One fire, or `null` |
| `find_wildfires_near_location` | `latitude`, `longitude`, `radiusKm` | Fires nearest first, with `distanceKm` |

Fire fields match the REST API. See [api.md](api.md).

Ask Firesight doesn't use this MCP server. It has its own tool list, which adds place-name
lookup and sync state. See [ADR 007](decisions/007-ask-firesight.md).

## Rules

- Tools call the same Application services as the REST API. No logic is copied into
  `Firesight.Mcp`, and tools never query the database ([ADR 005](decisions/005-layered-backend.md)).
- Bad input (for example, latitude 200) returns a tool error with the validation message.
  Any other failure returns a generic error with no internal details.
- Production and the MCP tests register tools through the same `WithFiresightTools()` call,
  so the tests always check what's actually deployed.

## Nullable fields in output schemas

.NET describes a field that can be null as `"type": ["string", "null"]`. That's valid JSON
Schema, but some MCP clients reject it. Firesight rewrites these fields as
`anyOf: [{ "type": "string" }, { "type": "null" }]`, which means the same thing and works
everywhere. The rewrite only affects the schemas MCP publishes. The application's data types
are unchanged.
