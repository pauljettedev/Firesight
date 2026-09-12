# MCP

## Purpose

Firesight exposes selected application capabilities through Model Context Protocol (MCP) tools so an AI model can query Firesight data through controlled application interfaces.

MCP is not the API used by the React frontend.

```text
React -------- REST --------> Firesight.Application
AI model ----- MCP tools ---> Firesight.Application
```

Both interfaces reuse the same application services.

## Implemented tools

### `get_active_wildfires`

Returns the current wildfire records available in Firesight.

### `get_wildfire_by_external_id`

Returns one current wildfire record by its CWFIS `national_fire_id`, or `null` when no current record is available.

### `find_wildfires_near_location`

Finds current wildfire records within a radius of a latitude/longitude point.

Inputs:

```text
latitude     - decimal degrees, -90 to 90
longitude    - decimal degrees, -180 to 180
radiusKm     - search radius in kilometres, greater than 0
```

Results are ordered nearest first and include `distanceKm`.

The tool delegates to `IWildfireService.FindWildfiresNearAsync`; MCP does not duplicate the spatial query or validation rules.

## Registration and schemas

Firesight uses the MCP C# SDK's attribute-based tool registration. Production and transport-level integration tests both use the same `WithFiresightTools()` registration extension so the test host cannot silently drift from the tool set registered by the API.

Tool input schemas are generated from the .NET method signatures. Structured content is enabled for the wildfire tools.

For wildfire output schemas, Firesight applies a small portability override when tools are listed. The .NET schema generator can represent nullable values as JSON Schema type arrays such as `"type": ["string", "null"]`. That syntax is valid JSON Schema, but some MCP clients reject or mishandle it. Firesight therefore advertises equivalent `anyOf` branches with one type per branch while keeping null as part of the contract.

The explicit output schemas are limited to this MCP interoperability boundary; application DTOs and business rules remain unchanged.

## Validation and errors

Application validation rules remain in `Firesight.Application`.

For example, latitude range validation is performed by `WildfireService`, not repeated in the MCP project.

When an application validation failure reaches the MCP boundary, the tool translates it to an MCP tool error with a safe validation message that an AI client can act on.

Unexpected exceptions are not rewritten with their internal exception text. The MCP SDK returns a generic tool error for those failures, avoiding accidental leakage of implementation details.

## Design rules

- MCP tools expose application capabilities, not raw database access.
- Tools delegate to `Firesight.Application`.
- Business and spatial-query logic must not be duplicated in MCP.
- Tool inputs and outputs should be narrow, typed, and easy to reason about.
- Prefer SDK-generated schemas except where an explicit interoperability constraint requires a portable MCP output schema.
- The AI should answer from tool results rather than assumed model knowledge.
- AI usage should be rate-limited in the public demo.
- The Claude API key must remain server-side.
