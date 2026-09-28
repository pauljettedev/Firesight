# Deployment

Firesight runs at **https://firesight.codewheel.ca** on a DigitalOcean droplet that it shares
with other codewheel.ca projects. Why it's set up this way: [ADR 010](decisions/010-hosting.md).
To run it on your own machine instead, see [setup.md](setup.md).

```text
Internet
   |
   v
Caddy (codewheel repo): ports 80/443, handles HTTPS
   |   shared "codewheel" Docker network
   v
firesight-app :8080 (API + React app)
   |   Firesight's own private network
   v
firesight-postgres
```

Only Caddy can be reached from the internet. The app and the database have no public ports.

## What runs

`docker-compose.prod.yml` starts two containers:

- **app**: one image, built by the root `Dockerfile`, containing the .NET API and the built
  React app. The API serves the React files itself.
- **postgres**: PostgreSQL with PostGIS. Its data lives in the `firesight-postgres-data`
  volume, so it survives rebuilds.

The app creates and updates its own database tables when it starts, so there's no separate
migration step.

## First deploy on a new server

Before this, the server needs:

- the shared Docker network, created once with `docker network create codewheel`
- the codewheel Caddy running (see the [codewheel repo](https://github.com/pauljettedev/codewheel))
- a DNS A record for `firesight.codewheel.ca` pointing at the server

Then:

```bash
git clone https://github.com/pauljettedev/Firesight.git
cd Firesight
cp .env.example .env     # then fill in the two values below
docker compose -f docker-compose.prod.yml up -d --build
```

`.env` holds the two secrets:

- `POSTGRES_PASSWORD`: a strong password, not the one used for local development
- `CLAUDE_API_KEY`: from the Anthropic Console

`.env` is ignored by git. Never commit it.

The full first-time setup, including creating the droplet, is in the
[server setup walkthrough](server-setup-walkthrough.md).

## Automatic deploys

Every push to `main` runs CI. If the tests pass, the `deploy` job in
`.github/workflows/ci.yml` connects to the server over SSH and runs:

```bash
cd Firesight
git fetch origin main
git reset --hard origin/main      # the server's copy always matches main exactly
docker compose -f docker-compose.prod.yml up -d --build --remove-orphans
docker image prune -f             # delete old builds so they don't fill the disk
```

- `git reset --hard` throws away any edits made directly on the server. Change files in the
  repo, not on the server.
- If any step fails, the deploy fails and shows red in GitHub Actions.
- Only one deploy runs at a time. A second push waits for the first to finish.
- A deploy can run for up to 20 minutes.

To deploy by hand instead, run the same commands on the server from the `Firesight` folder.

### Setting up automatic deploys

This is done once.

1. On the server, create an SSH key used only for deploys:
   ```bash
   ssh-keygen -t ed25519 -f ~/.ssh/firesight-deploy -N ""
   cat ~/.ssh/firesight-deploy.pub >> ~/.ssh/authorized_keys
   ```
2. On GitHub, open the repository's **Settings** (not your account settings), then
   **Secrets and variables → Actions**, and add these secrets:
   - `DEPLOY_HOST`: the server's IP address
   - `DEPLOY_USER`: the SSH user that owns `~/Firesight`
   - `DEPLOY_SSH_KEY`: the contents of the private key `~/.ssh/firesight-deploy` (not the
     `.pub` file)
   - `DEPLOY_HOST_FINGERPRINT`: the server's fingerprint, so the deploy refuses to connect to
     anything else. Get it on the server with
     `ssh-keygen -lf /etc/ssh/ssh_host_ecdsa_key.pub | cut -d ' ' -f2`. It starts with
     `SHA256:`. Use the ECDSA key as shown: the deploy action checks the server's ECDSA key
     first, so a fingerprint from another key type won't match. Only if the server has no
     ECDSA key, use the RSA one (`ssh_host_rsa_key.pub`).
3. Delete the private key from the server: `rm ~/.ssh/firesight-deploy`. GitHub now holds the
   only copy.
4. On the **Variables** tab, add `DEPLOY_ENABLED` with the value `true`. Until this is set, the
   deploy job is skipped.

Because the deploy key is separate from your own, it can be revoked without affecting your SSH
access.

The deploy action is pinned to a specific commit rather than a version tag, because it's given
the server's SSH key.

## Limiting Claude costs

- The Claude API key stays in `.env` on the server and never reaches the browser.
- Only Ask Firesight calls Claude. The map and the other tabs never do.
- Each IP address can ask 10 questions a minute ([api.md](api.md#rate-limits)).
- Questions are limited to 500 characters, each answer to 8,000 tokens, and each question to
  4 rounds of data lookups.
