# Decisions

Short records of the major decisions behind Firesight: what was decided, why, and what it
means for the code.

| ADR | Decision |
| --- | --- |
| [001](001-stack.md) | Technology stack |
| [002](002-cwfis.md) | CWFIS as the wildfire data source |
| [003](003-postgis.md) | PostgreSQL with PostGIS |
| [004](004-wildfire-freshness.md) | Feed freshness and fire freshness are tracked separately |
| [005](005-layered-backend.md) | Layered backend shared by the REST API and MCP |
| [006](006-frontend-components.md) | Every UI element is its own component |
| [007](007-ask-firesight.md) | Ask Firesight: the AI picks the query, the code gives the answer |
| [008](008-testing.md) | Test against a real database |
| [009](009-api-errors.md) | Only explicit validation errors are client errors |
