# Firesight.Web

React + TypeScript frontend for Firesight.

The web application uses:

- Vite for local development and builds
- Material UI for application components
- MapLibre GL JS for wildfire mapping
- the Firesight ASP.NET Core API for wildfire, health, and synchronization-state data

During local development, Vite proxies `/api` requests to the backend so the frontend can call the API without hardcoding deployment-specific URLs.

## Run locally

From this directory:

```powershell
npm install
npm run dev
```

The development server runs at:

```text
http://localhost:5173
```

The Firesight API should also be running locally.

For complete project setup instructions, see [../../docs/quickstart.md](../../docs/quickstart.md).

## Checks

```powershell
npm run lint
npm run build
```

For project architecture and backend details, see the repository-level [README](../../README.md) and [architecture documentation](../../docs/architecture.md).
