# Setup

How to run and test Firesight on your own machine. Commands are PowerShell, run from the
repository root unless noted.

## 1. Install

- Git
- Docker Desktop
- .NET 10 SDK
- Node.js 24

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

This runs PostgreSQL with PostGIS in a container.

You don't need to create any tables. The API creates and updates them itself when it starts.

## 4. Add your Claude API key (optional)

Only Ask Firesight needs this. Without it, asking a question returns an error, and everything
else works normally.

```powershell
dotnet user-secrets set "Claude:ApiKey" "<your key>" --project src/Firesight.Api
```

Get a key from the [Anthropic Console](https://console.anthropic.com/). User secrets are stored
on your machine outside the repository, so the key can't be committed by accident.

## 5. Start the API

```powershell
dotnet run --project src/Firesight.Api
```

The API runs at `http://localhost:5213`. It needs the database from step 3; if the database
isn't running, the API stops with an error.

When it starts, it loads the current fires from CWFIS, then refreshes them every hour. If CWFIS
is down, the API still starts and serves whatever it already has.

Check it's working: `http://localhost:5213/api/health`

## 6. Start the web app

In a second terminal:

```powershell
cd src/Firesight.Web
npm install
npm run dev
```

Open `http://localhost:5173`. The web app sends its `/api` requests on to the API from step 5.

## Run the tests

```powershell
dotnet test                          # all backend tests
cd src/Firesight.Web
npm test                             # UI tests
npm run lint
npm run build                        # type-check and production build
```

The backend integration tests start their own temporary PostGIS database in Docker, so Docker
Desktop must be running. They don't touch the database from step 3.

CI runs the same commands on every push.

## Stop everything

Press `Ctrl+C` in the API and web app terminals, then:

```powershell
docker compose down
```

Your data is kept in a Docker volume and will still be there next time. To delete it and
start with an empty database, use `docker compose down -v` instead.
