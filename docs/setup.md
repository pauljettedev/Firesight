# Setup

How to run and test Firesight on your own machine. Commands are PowerShell, run from the
repository root unless noted.

## 1. Install

- Git
- Docker Desktop
- .NET 10 SDK
- Node.js LTS

## 2. Get the code

```powershell
git clone https://github.com/pauljettedev/Firesight.git
cd Firesight
```

## 3. Start the database

With Docker Desktop running:

```powershell
docker compose up -d
```

This runs PostgreSQL with PostGIS in a container. You don't need to create any tables: the
API does that itself when it starts.

## 4. Add your Claude API key (optional)

Only Ask Firesight needs this. Everything else works without it.

```powershell
dotnet user-secrets set "Claude:ApiKey" "<your key>" --project src/Firesight.Api
```

Get a key from the [Anthropic Console](https://console.anthropic.com/). User secrets are stored
outside the repository, so the key can't be committed by accident.

## 5. Start the API

```powershell
dotnet run --project src/Firesight.Api
```

The API runs at `http://localhost:5213`. On startup it loads current fires from CWFIS, then
refreshes every hour. If CWFIS is down, the API still starts and serves whatever it already has.

Check it's working: `http://localhost:5213/api/health`

## 6. Start the web app

In a second terminal:

```powershell
cd src/Firesight.Web
npm install
npm run dev
```

Open `http://localhost:5173`. The dev server forwards `/api` requests to the API.

## Run the tests

```powershell
dotnet test                          # all backend tests
cd src/Firesight.Web
npm test                             # UI tests
npm run lint
npm run build                        # type-check and production build
```

The backend integration tests start their own throwaway PostGIS database in Docker, so Docker
must be running.

## Stop everything

Press `Ctrl+C` in the API and web app terminals, then:

```powershell
docker compose down
```

Your data is kept in a Docker volume and will still be there next time.
