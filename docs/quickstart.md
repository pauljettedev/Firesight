# Quick Start

Want to run Firesight without reading all of the documentation first? Start here.

This is the shortest path to getting the application running on a Windows development machine.

> **Note:** Docker currently runs the PostgreSQL/PostGIS database. The Firesight API and web application still run locally using .NET and Node.js.

## 1. Install the required software

You need:

- Git
- Docker Desktop
- .NET 10 SDK
- Node.js LTS (includes npm)

If you already have these installed, continue to step 2.

## 2. Get the Firesight source code

Clone the repository and open a terminal in the repository root.

```powershell
git clone <firesight-repository-url>
cd Firesight
```

If you downloaded the repository as a ZIP instead, extract it and open PowerShell in the extracted `Firesight` folder.

## 3. Start the database

Make sure Docker Desktop is running, then run:

```powershell
docker compose up -d
```

Check that the database container is running:

```powershell
docker compose ps
```

You should see the Firesight PostgreSQL container running.

You do **not** need to manually create the database tables. The API applies the Entity Framework Core migrations when it starts.

## 4. Start the Firesight API

Open a second terminal in the repository root and run:

```powershell
dotnet run --project src/Firesight.Api
```

The API should start at:

```text
http://localhost:5213
```

On startup, Firesight attempts to download the current active-fire data from the Canadian Wildland Fire Information System (CWFIS).

To confirm the API is working, open:

```text
http://localhost:5213/api/health
```

## 5. Start the web application

Open a third terminal and run:

```powershell
cd src/Firesight.Web
npm install
npm run dev
```

Then open:

```text
http://localhost:5173
```

Firesight should now be running.

## That's it

For normal local use, the three things that need to be running are:

```text
Docker Desktop / PostgreSQL
        +
Firesight.Api
        +
Firesight.Web
```

After the first setup, starting Firesight is normally just:

**Terminal 1 — database**

```powershell
docker compose up -d
```

**Terminal 2 — API**

```powershell
dotnet run --project src/Firesight.Api
```

**Terminal 3 — web UI**

```powershell
cd src/Firesight.Web
npm run dev
```

Then browse to `http://localhost:5173`.

## Stop Firesight

Stop the API and web development server with `Ctrl+C` in their terminals.

Stop the Docker services with:

```powershell
docker compose down
```

The PostgreSQL data volume is retained unless you explicitly remove it.

## If something does not start

Use the full [Local Setup](setup.md) guide for database checks, build commands, test instructions, ports, and troubleshooting context.

For an explanation of how the projects fit together, see [Architecture](architecture.md).
