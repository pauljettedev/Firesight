# ADR 010: Hosting on a subdomain behind a shared Caddy

**Status:** Accepted

## Decision

- Firesight runs at `firesight.codewheel.ca`. `codewheel.ca` is a portfolio landing page, and
  each future project gets its own subdomain.
- One DigitalOcean droplet runs everything with Docker Compose.
- Caddy (HTTPS and ports 80/443) lives in its own `codewheel` repo, with the landing page.
  Each project's containers join a shared `codewheel` Docker network so Caddy can reach them.
- Firesight's app container serves both the API and the built React app.

## Why

- **Subdomain, not `codewheel.ca/firesight`:** the app runs at `/` exactly as it does locally,
  so no URL prefixes are needed in the frontend or backend. Each project also gets its own
  browser origin (cookies and storage stay separate) and can move to another server by
  changing one DNS record.
- **Caddy in its own repo:** it serves every project, so it shouldn't belong to any one of
  them. Adding a project means adding a few lines to that repo's Caddyfile.
- **One droplet:** Firesight needs a database that's always running. One small server for
  everything costs a fixed amount each month and is simple to understand. Scale-to-zero
  platforms would mean paying for and managing compute and a database separately.
- **One app container:** the frontend and API share an origin, so there's no CORS setup.

## What this means

- Firesight's compose file has no Caddy and no public ports.
- The `codewheel` network is created once by hand (`docker network create codewheel`), and
  every stack joins it as external. No stack owns it, so any stack can be stopped or restarted
  on its own.
- A new project needs a DNS record, a Caddyfile entry, and a compose file that joins the
  `codewheel` network.
