# Deployment

## Goal

Keep the portfolio site inexpensive while still demonstrating realistic deployment practices.

## Local development

Docker Compose currently runs PostgreSQL/PostGIS locally.

```text
Docker Compose
└── firesight-postgres
```

The API and React frontend currently run directly on the development machine.

## Planned hosted architecture

```text
GitHub
  |
  v
Container host
  |
  +--> Firesight application container
  |
  +--> managed PostgreSQL/PostGIS
```

A production deployment may serve the compiled React application from the ASP.NET Core application container, reducing the number of paid services required.

## Database persistence

Application containers should be considered disposable.

Persistent wildfire data should live in managed PostgreSQL/PostGIS or another persistent database service rather than inside the application container filesystem.

For a low-cost demo environment, the database may also be designed to be reconstructable from:
- EF Core seed/reference data
- CWFIS imports

## AI cost protection

The hosted application should include:
- server-side Claude API key
- application rate limiting
- bounded prompt/input size
- bounded output tokens
- usage logging
- project/account spending controls

The interactive map should not require Claude calls. AI usage should occur only when a user explicitly invokes an AI feature.
