# MCP

## Purpose

Firesight will expose selected application capabilities through Model Context Protocol (MCP) tools so an AI model can query Firesight data through controlled application interfaces.

MCP is not the API used by the React frontend.

```text
React -------- REST --------> Firesight.Application
AI model ----- MCP tools ---> Firesight.Application
```

Both interfaces should reuse the same application services.

## Planned tool examples

Initial concepts include:

```text
get_active_fires
get_fire_details
find_fires_near_location
get_largest_active_fires
```

These names are provisional and should be finalized only after the application model and CWFIS data shape are understood.

## Design rules

- MCP tools should expose application capabilities, not raw database access.
- Tools should delegate to `Firesight.Application`.
- The AI should answer from tool results rather than assumed model knowledge.
- Tool inputs and outputs should be narrow, typed, and easy to reason about.
- AI usage should be rate-limited in the public demo.
- The OpenAI API key must remain server-side.
