# ADR 001: Technology stack

**Status:** Accepted

## Decision

- **Frontend:** React, TypeScript, Vite, MUI, MapLibre GL JS
- **Backend:** ASP.NET Core (C#), Entity Framework Core, OpenAPI
- **Database:** PostgreSQL with PostGIS
- **AI:** Claude API (Anthropic), MCP
- **Tooling:** Docker, GitHub Actions, xUnit, Vitest

## Why

Firesight is a portfolio project. The stack should be common in real jobs, give each tool one
clear role, and avoid niche libraries unless the problem needs them.

## What this means

- The project touches many technologies, so scope has to be kept small on purpose.
- A new library needs a clear reason to be added.
