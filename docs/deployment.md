# Deployment

Firesight runs at **https://firesight.codewheel.ca** on a DigitalOcean droplet shared with
other codewheel.ca projects. Why it's set up this way:
[ADR 010](decisions/010-hosting.md). For running locally, see [setup.md](setup.md).

```text
Internet
   |
   v
Caddy (codewheel repo): ports 80/443, HTTPS
   |   shared "codewheel" Docker network
   v
firesight-app :8080 (API + built React app)
   |   Firesight's private network
   v
firesight-postgres
```

Only Caddy is reachable from the internet. The app and the database have no public ports.

## What this repo deploys

`docker-compose.prod.yml` runs two containers:

- **app**: built by the root `Dockerfile`, which builds the React app and the .NET API into one
  image. The API serves the React files itself.
- **postgres**: PostGIS, with data kept in the `firesight-postgres-data` volume, so it
  survives rebuilds.

The app creates and updates its own database tables on startup, so there's no separate
migration step.

## First deploy on a server

This assumes the server already has the shared network (`docker network create codewheel`,
run once), the codewheel Caddy is running (see the
[codewheel repo](https://github.com/pauljettedev/codewheel)), and a DNS A record for
`firesight.codewheel.ca` points at the server.

```bash
git clone https://github.com/pauljettedev/Firesight.git
cd Firesight
cp .env.example .env     # then fill in the values
docker compose -f docker-compose.prod.yml up -d --build
```

`.env` needs:

- `POSTGRES_PASSWORD`: a strong password, not the local dev one
- `CLAUDE_API_KEY`: from the Anthropic Console

`.env` is gitignored and must never be committed.

## Updating

Manually, on the server:

```bash
cd ~/Firesight
git pull
docker compose -f docker-compose.prod.yml up -d --build --remove-orphans
```

## Continuous deployment

When `DEPLOY_ENABLED` is on, every push to `main` that passes CI runs the update commands
above over SSH (the `deploy` job in `.github/workflows/ci.yml`), then removes unused Docker
images so old builds don't fill the disk. Deploys run one at a time.

One-time setup:

1. On the server, create a key just for deploys:
   ```bash
   ssh-keygen -t ed25519 -f ~/.ssh/firesight-deploy -N ""
   cat ~/.ssh/firesight-deploy.pub >> ~/.ssh/authorized_keys
   ```
2. In the repository's **Settings** tab on GitHub (not your account settings), go to
   **Secrets and variables → Actions** and add these secrets:
   - `DEPLOY_HOST`: the server's IP address
   - `DEPLOY_USER`: the SSH user that owns `~/Firesight`
   - `DEPLOY_SSH_KEY`: the contents of the private key `~/.ssh/firesight-deploy` (not `.pub`)
3. Delete the private key from the server: `rm ~/.ssh/firesight-deploy`. GitHub keeps the only
   copy it needs.
4. On the **Variables** tab, add `DEPLOY_ENABLED` = `true`.

A separate deploy key can be revoked without affecting your own SSH access.

## Protecting the Claude bill

- The API key stays on the server (`.env`) and never reaches the browser.
- Ask Firesight is rate-limited per IP ([api.md](api.md#rate-limits)), questions are capped at
  500 characters, and answers have a maximum length.
- Only Ask Firesight calls Claude. The map never does.

Worth adding if traffic grows: logging of Ask Firesight usage, and a spending cap on the
Anthropic account.
