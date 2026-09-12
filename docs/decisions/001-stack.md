# ADR 001: Core technology stack

## Status

Accepted

## Decision

Firesight will use a broad, industry-recognizable stack:

- React
- TypeScript
- Vite
- MUI
- MapLibre GL JS
- ASP.NET Core / C#
- Entity Framework Core
- PostgreSQL / PostGIS
- Docker / Docker Compose
- GitHub / GitHub Actions
- Serilog
- OpenAPI
- Claude API (Anthropic)
- MCP

## Context

Firesight is a portfolio project intended to demonstrate practical, transferable engineering skills to employers.

The stack should therefore favor:
- common technologies
- clear architectural roles
- realistic production practices
- minimal use of niche libraries unless the problem genuinely requires them

## Consequences

Positive:
- broad relevance to full-stack .NET roles
- modern React/TypeScript experience
- geospatial database experience
- containerization and CI/CD exposure
- practical AI/MCP integration

Tradeoff:
- the project spans several technologies and therefore requires deliberate scope control
