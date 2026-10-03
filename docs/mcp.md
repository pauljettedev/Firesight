# MCP

Firesight has an MCP (Model Context Protocol) server, so AI assistants such as Claude can look
up its wildfire data themselves. It's part of the API, served at `/mcp` over HTTP.

| | |
| --- | --- |
| Live | `https://firesight.codewheel.ca/mcp` |
| Local | `http://localhost:5213/mcp` |

There's no login and no rate limit, the same as the REST wildfire endpoints. Each request
stands on its own; the server keeps no session between calls.

## Connecting

**Claude (web and desktop apps).** Open Settings → Connectors, choose Add custom connector, and
enter `https://firesight.codewheel.ca/mcp`. The tools then work in any new chat.

**Claude Code on your own computer.** Run:

```bash
claude mcp add --transport http --scope user firesight https://firesight.codewheel.ca/mcp
```

`--scope user` makes it available in every Claude Code session. Without it, Claude Code only
adds it for the folder you ran the command in. The server is picked up when a session starts,
so start a new one afterwards; `/mcp` inside Claude Code shows whether it's connected.

Claude Code on the web runs in a cloud container whose network policy may block
firesight.codewheel.ca, so the command can fail there even though the server is fine. Use the
connector above instead.

## Tools

All three tools only read data. They never change anything.

| Tool | Inputs | Returns |
| --- | --- | --- |
| `get_active_wildfires` | none | Every fire the map shows |
| `get_wildfire_by_external_id` | `externalId`, the CWFIS `national_fire_id`, for example `2026_ON_THU_FIRE_036` | That fire, or `null` if Firesight doesn't have it |
| `find_wildfires_near_location` | `latitude`, `longitude`, `radiusKm` | Fires within the radius, nearest first, each with its `distanceKm` |

The tools return the same fires the map shows: current fires, including stale ones, and
extinguished fires for 7 days after they went out. Each fire has the same fields as the REST
API ([api.md](api.md)), including `isStale`.

Each tool's description also explains the results to the model: what the status codes mean,
that a null field means CWFIS didn't provide the value, and that a fire missing from recent
feed updates isn't necessarily out. Without that, Claude guessed that such fires had been
declared out ([data-sources.md](data-sources.md)).

`find_wildfires_near_location` checks its inputs the same way the REST API does: latitude from
-90 to 90, longitude from -180 to 180, and a radius above 0 and up to 1,000 km. Bad input comes
back as a tool error listing every problem, for example
`radiusKm: Radius must be a finite value greater than 0 and at most 1000 km.`
Any other failure comes back as a generic error, without internal details.

Ask Firesight doesn't use this server. It has its own tools, which add place-name lookup and
the feed's sync state ([ADR 007](decisions/007-ask-firesight.md)).

## How it's built

- Each tool calls the same `IWildfireService` as the REST API. There's no wildfire logic and no
  database access in `Firesight.Mcp` ([ADR 005](decisions/005-layered-backend.md)).
- Production and the integration tests register the tools through the same
  `WithFiresightTools()` call, so the tests check the same tools that are deployed.
- The integration tests connect a real MCP client to the server and call the tools, with a
  stand-in wildfire service so each test controls what comes back.

## Fields that can be null

Each tool publishes a description of its output (an output schema), so a client knows what to
expect. .NET describes a field that can be null, such as `name` or `areaHectares`, as
`"type": ["string", "null"]`. Some MCP clients reject that, so Firesight publishes those fields
as `anyOf: [{ "type": "string" }, { "type": "null" }]` instead. Both mean the same thing. This
only changes the published description; the data itself is the same.

Every field is listed as required, so a null field is always sent as `null` rather than left
out. The MCP library leaves nulls out by default, and strict clients such as Claude Code reject
a result with a required field missing.
