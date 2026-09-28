# References

Official documentation for everything Firesight uses, with what it's used for.

## Backend

- [.NET](https://learn.microsoft.com/dotnet/) and
  [ASP.NET Core](https://learn.microsoft.com/aspnet/core/): the API, the hourly sync, and
  serving the React app
- [Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis): how the
  REST endpoints are written
- [Rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit): the limits
  on Ask Firesight and place search
- [OpenAPI in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/):
  the API description, available when running locally
- [Entity Framework Core](https://learn.microsoft.com/ef/core/): database access and migrations

## Database and spatial

- [PostgreSQL](https://www.postgresql.org/docs/) and
  [PostGIS](https://postgis.net/documentation/): storage and distance searches
- [Npgsql EF Core provider](https://www.npgsql.org/efcore/): connects EF Core to PostgreSQL,
  including PostGIS types
- [NetTopologySuite](https://nettopologysuite.github.io/NetTopologySuite/): the point and
  distance types used in C#

## Frontend

- [React](https://react.dev/) and [TypeScript](https://www.typescriptlang.org/docs/)
- [MUI](https://mui.com/material-ui/): UI components and theme
- [MapLibre GL JS](https://maplibre.org/maplibre-gl-js/docs/): the map
- [Vite](https://vite.dev/guide/): dev server and build
- [ESLint](https://eslint.org/docs/latest/): linting

## AI

- [Claude API](https://platform.claude.com/docs/) and the
  [Anthropic C# SDK](https://github.com/anthropics/anthropic-sdk-csharp): Ask Firesight
- [Model Context Protocol](https://modelcontextprotocol.io/) and the
  [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk): the `/mcp` server

## Testing

- [xUnit](https://xunit.net/) and [Moq](https://github.com/devlooped/moq): backend tests
- [Testcontainers for .NET](https://dotnet.testcontainers.org/): the disposable PostGIS
  database the integration tests run against
- [Vitest](https://vitest.dev/) and
  [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/): UI
  tests

## Hosting and deployment

- [Docker](https://docs.docker.com/) and [Docker Compose](https://docs.docker.com/compose/):
  local database and production containers
- [GitHub Actions](https://docs.github.com/actions): CI and automatic deploys
- [Caddy](https://caddyserver.com/docs/): HTTPS in front of the app (set up in the codewheel
  repo)
- [DigitalOcean Droplets](https://docs.digitalocean.com/products/droplets/): the server

## Data sources and their rules

- [CWFIS](https://cwfis.cfs.nrcan.gc.ca/): the wildfire data
- [Open Government Licence – Canada](https://open.canada.ca/en/open-government-licence-canada):
  the licence CWFIS data is published under
- [Nominatim search API](https://nominatim.org/release-docs/latest/api/Search/) and its
  [usage policy](https://operations.osmfoundation.org/policies/nominatim/): place search
- [OpenStreetMap tile usage policy](https://operations.osmfoundation.org/policies/tiles/): the
  map background

How Firesight uses each data source is in [data-sources.md](data-sources.md).
