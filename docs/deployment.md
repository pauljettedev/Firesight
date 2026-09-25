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

## Hosted architecture

A single DigitalOcean droplet runs three containers via `docker-compose.prod.yml`:

```text
Internet
  |
  v
Caddy (ports 80/443, automatic HTTPS)
  |
  v
Firesight app container (API + built React app, port 8080, not exposed to the internet)
  |
  v
PostgreSQL/PostGIS container (not exposed to the internet)
```

Caddy is the only container reachable from outside the droplet. It terminates HTTPS (a free
certificate from Let's Encrypt, renewed automatically) and forwards everything to the app
container over Compose's private internal network. The app reaches Postgres the same way.
Neither the app nor the database ever has a port published to the host.

The `Dockerfile` at the repo root builds the React app and the .NET API in one multi-stage
build, and the API serves the built React files itself (see `Program.cs`) — one container is
the whole app, frontend and backend share one origin, and there is no CORS configuration to
maintain.

A single always-on droplet was chosen over a scale-to-zero container platform (Fly.io, Cloud
Run) specifically because the database needs to persist data across restarts, and cheap
serverless Postgres and cheap serverless compute are two separate things to provision and pay
for. One small droplet running both containers is simpler to reason about and to explain, for
a small amount of guaranteed monthly cost instead of a theoretical $0 minimum.

## Deploying to a new droplet

1. Create a droplet (a $6-12/month "Basic" droplet with 1-2GB RAM is enough for this app's
   traffic). Pick any Linux image; Docker gets installed in the next step.
2. Point your domain's DNS at the droplet: add an A record for your domain (and `www` if you
   want it) to the droplet's IP address. This can take a few minutes to a few hours to
   propagate.
3. SSH into the droplet and install Docker and the Compose plugin (see
   [Docker's official install instructions](https://docs.docker.com/engine/install/) for the
   droplet's Linux distribution).
4. Clone the repository onto the droplet:
   ```bash
   git clone https://github.com/pauljettedev/Firesight.git
   cd Firesight
   ```
5. Copy `.env.example` to `.env` and fill in real values:
   ```bash
   cp .env.example .env
   ```
   - `DOMAIN` — the domain name from step 2.
   - `POSTGRES_PASSWORD` — a strong, unique password. Not the local dev password.
   - `CLAUDE_API_KEY` — an API key from the Anthropic Console.

   `.env` is gitignored and must never be committed.
6. Start everything:
   ```bash
   docker compose -f docker-compose.prod.yml up -d --build
   ```
   The app applies its own database migrations on startup (see `DatabaseInitializer`), so
   there's no separate migration step to run by hand.
7. Open `https://<your-domain>` in a browser. The first request to a fresh certificate can
   take a few extra seconds while Caddy provisions it.

To deploy an update later, `git pull` on the droplet and re-run the step 6 command — Compose
rebuilds only what changed. Once continuous deployment (below) is set up, this happens
automatically instead.

Changes to the `Caddyfile` are the exception: it's bind-mounted as a single file, and a
running container keeps seeing the old copy after `git pull` replaces it. Apply them with
`docker compose -f docker-compose.prod.yml restart caddy`.

## Continuous deployment

Pushing to `main` runs the existing CI checks (`.github/workflows/ci.yml`), and only if those
pass, a `deploy` job SSHes into the droplet and re-runs the same commands from step 6 above
(`git fetch` + `git reset --hard origin/main`, then `docker compose up -d --build`). Nothing
deploys unless the backend and frontend jobs both succeed first.

This is a one-time setup per droplet, done outside of Git:

1. On the droplet, create a dedicated deploy key rather than reusing your own SSH key:
   ```bash
   ssh-keygen -t ed25519 -f ~/.ssh/firesight-deploy -N ""
   cat ~/.ssh/firesight-deploy.pub >> ~/.ssh/authorized_keys
   ```
2. In the GitHub repository's **Settings → Secrets and variables → Actions**, add three
   repository secrets:
   - `DEPLOY_HOST` — the droplet's IP address or domain.
   - `DEPLOY_USER` — the SSH username used to connect (whichever user owns `~/Firesight` on
     the droplet).
   - `DEPLOY_SSH_KEY` — the *private* key content from `~/.ssh/firesight-deploy` (not the
     `.pub` file).
3. Delete the private key from the droplet once it's saved in the secret
   (`rm ~/.ssh/firesight-deploy`) — GitHub Actions only needs the copy it holds, and there's
   no reason for the private half to also sit on disk on the server it's used to access.
4. In the same **Settings → Secrets and variables → Actions** area, switch to the
   **Variables** tab and add a repository variable `DEPLOY_ENABLED` set to `true`. The
   `deploy` job in `ci.yml` checks this before running, so pushes to `main` before the
   droplet exists build and test normally without a failing deploy step.

Using a key generated just for this, rather than a personal SSH key, means it can be revoked
or rotated at any time from GitHub's secrets settings without touching how you personally
access the droplet.

## Database persistence

Application containers should be considered disposable — the `docker compose up` command above
can be re-run at any time without losing data, because `docker-compose.prod.yml` stores
Postgres's data in a named Docker volume (`firesight-postgres-data`) rather than inside the
container filesystem. The volume survives container restarts and rebuilds; it's only lost if
someone explicitly deletes it (`docker volume rm`) or destroys the droplet itself.

The dataset is also fully reconstructable from scratch if needed, since it's a mirror of a live
public feed: the next scheduled CWFIS sync repopulates current wildfire data from nothing.

## AI cost protection

Implemented:
- the Claude API key is a server-side secret (`.env`, never in source control or shipped to
  the browser)
- per-IP rate limiting on the `/api/ask` endpoint (see `Program.cs`)
- a bounded input length on the question field and a bounded max output token count (see
  `AskFiresightEndpoints.cs` / `ClaudeAskFiresightService.cs`)
- the interactive map never calls Claude — AI usage only happens when a user explicitly asks
  Ask Firesight a question

Not yet implemented, worth adding if this ever saw real traffic:
- usage logging (how many Ask Firesight calls happened, from where)
- an account-level spending cap on the Claude API key itself, as a hard backstop below the
  rate limiter
