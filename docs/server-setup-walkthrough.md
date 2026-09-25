# Server Setup Walkthrough

A plain-language, click-by-click record of standing up the Firesight production server from
scratch. `deployment.md` is the terse technical reference (what to do); this document is the
fuller walkthrough (what each step actually means and why), for anyone who hasn't set up a
server/DNS/domain in a while, or is doing it for the first time.

This file is written incrementally as the server is built — see the bottom for what's done so
far and what's still to come.

## 1. DigitalOcean: create a Project

DigitalOcean "Projects" are just organizational folders for grouping your cloud resources
(droplets, databases, etc.) — they don't affect how anything runs. Created a project named
`Firesight`, environment type `Production`, purpose `Web Application`, so this droplet doesn't
get lost among unrelated old projects in the same DigitalOcean account.

## 2. DigitalOcean: create the Droplet

A "Droplet" is DigitalOcean's name for a basic virtual server — a slice of a physical machine
you get root/SSH access to, with a fixed IP address, that you can install anything on.

Choices made and why:

- **Region: Toronto** — closest datacenter to the owner, lowest latency.
- **Image: Ubuntu 24.04 LTS** — a stable, long-supported Linux distribution; Docker gets
  installed separately (see step 5), the OS image itself doesn't need to include it.
- **Plan: Basic, Regular (shared) CPU, $12/month — 2 GB RAM, 50 GB SSD.** The app runs three
  containers on one box (Postgres/PostGIS, the API, and Caddy as a reverse proxy). 1 GB was
  judged too tight for all three to share comfortably (real risk of running out of memory
  during something like a big data sync); "Premium" CPU tiers were skipped since this is a
  low-traffic demo, not a performance-sensitive workload.
- **Volumes (extra block storage): skipped** — not needed, the included 50 GB is far more
  than this app's data will ever need.
- **Backups: skipped, deliberately.** This isn't just a cost call — the whole architecture in
  `deployment.md` is built around the wildfire dataset being disposable and fully
  reconstructable from the live CWFIS feed on the next sync. Paying extra to back up data
  that rebuilds itself for free doesn't add value here.
- **Networking: Public IPv4 only.** IPv6 was left off — enabling it would mean also managing
  an AAAA DNS record for no real benefit on a small project.
- **Monitoring: "Improved Metrics and monitoring" enabled (it's free).** Gives CPU/RAM/disk
  graphs and the ability to set alerts — worth having given three containers are sharing one
  box's resources.
- **Additional options: both skipped.** "Startup scripts" automate first-boot setup, but
  since this is a one-time manual setup anyway (see step 5), scripting it added no value.
  "Add a worry-free Managed Database" ($15/month) was skipped on purpose — the architecture
  intentionally runs Postgres/PostGIS in its own container instead of paying for a managed
  database service, to keep the whole app's hosting cost to one droplet.
- **Authentication: SSH Key**, not password — see step 3.
- **Hostname:** `firesight-prod`.
- **Tags:** skipped — the Project already provides enough organization for a single droplet.

## 3. Generate and add an SSH key (for personal login access)

An SSH key pair is a more secure alternative to a password: a *private* key stays only on your
own machine and is never shared; a matching *public* key gets uploaded to the server (or in
this case, to DigitalOcean, which installs it on the droplet automatically). The server can
then verify "does whoever's connecting have the matching private key?" without a password ever
being transmitted.

Generated with (Windows PowerShell):

```
ssh-keygen
```

Accepted the default save location, then read the public key back out with:

```
cat ~/.ssh/id_rsa.pub
```

and pasted that into DigitalOcean's "Add a public SSH key" dialog (comment/label suffix
— the trailing `user@hostname` text on the key — was left off; it's cosmetic only and doesn't
affect the key's function).

Note: this key is for the *owner's own personal login access* to the droplet. It's separate
from the dedicated deploy key that continuous deployment will use later (see `deployment.md`'s
"Continuous deployment" section) — that one gets generated directly on the droplet, so it can
be revoked independently of personal access.

## 4. DNS: point the domain at the droplet

Domain: `codewheel.ca`, registered at Namecheap. Droplet's public IP: `134.122.40.150`.

At the registrar's **Advanced DNS** tab:

1. **Deleted the default parking CNAME record** (`CNAME | www | parkingpage.namecheap.com`) —
   this was Namecheap's placeholder for a freshly registered, unconfigured domain, and a host
   can't have both a CNAME and an A record pointing different places, so it had to go before
   adding the real A record for `www`.
2. **Deleted the default parking redirect** (`codewheel.ca → http://www.codewheel.ca/`), for
   the same reason — it would conflict with pointing the domain directly at the droplet.
3. **Added two A records:**

   | Type | Host | Value | TTL |
   |---|---|---|---|
   | A | `@` | `134.122.40.150` | Automatic |
   | A | `www` | `134.122.40.150` | Automatic |

   An A record is the basic building block of DNS: "this name = this IP address." Once this
   propagates, visiting `codewheel.ca` or `www.codewheel.ca` sends the browser straight to the
   droplet.

Left untouched (unrelated to the web server):
- The **TXT record** under Mail Settings (an SPF record — tells other mail servers which
  servers are allowed to send email as `@codewheel.ca`; about email deliverability, not the
  website).
- **DNSSEC** (off) and **Dynamic DNS** (off) — neither is needed here; DNSSEC cryptographically
  signs DNS records against tampering (a nice-to-have, not required for this project), and
  Dynamic DNS is for servers whose IP address changes automatically, which doesn't apply to a
  droplet with a fixed IP.

DNS changes can take anywhere from a few minutes to a few hours to fully propagate.

## 5. SSH in and prepare the server

```
ssh root@134.122.40.150
```

First connection to a new server always shows a host key confirmation prompt (SSH saying "I've
never seen this server before, are you sure this is who you think it is?") — answered `yes`.
This only appears once per server; if it ever reappears unexpectedly on a *known* server, that
would be a red flag (it would mean the server's identity changed).

Applied pending OS updates before installing anything else:

```
sudo apt update && sudo apt upgrade -y
```

**Important:** partway through, this prompted with a config-file conflict for
`/etc/ssh/sshd_config`, asking whether to install the package maintainer's default version or
keep the locally modified one. **Kept the local version** (the prompt's default option) —
DigitalOcean customizes this file on droplet creation (SSH key access, cloud-specific settings),
and overwriting it with the plain Ubuntu default risks undoing those customizations, potentially
breaking the SSH access that's currently working. If this prompt reappears on a future update,
keep the local version again unless there's a specific, understood reason to change it.

### Installing Docker + the Compose plugin

Docker's official install method for Ubuntu, step by step:

```bash
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

What each part does:

- `ca-certificates` / `curl` — trusted security certificates and a download tool, needed to
  fetch Docker's install files securely.
- `install -m 0755 -d /etc/apt/keyrings` — creates a folder to hold trusted signing keys.
- `curl ... -o /etc/apt/keyrings/docker.asc` — downloads Docker's official signing key, so the
  system can verify that software claiming to be from Docker really is.
- `chmod a+r` — makes that key file readable by the package manager.
- The `echo ... | sudo tee /etc/apt/sources.list.d/docker.list` line — registers Docker's own
  package server as a new trusted software source (like adding a new app store), tied to the
  key from the previous step.
- `apt-get update` — refreshes the list of available software, now including Docker's.
- The final `apt-get install` — installs Docker Engine (`docker-ce`, the part that actually
  runs containers), the `docker` command-line tool (`docker-ce-cli`), a lower-level container
  runtime Docker depends on (`containerd.io`), modern image-building support
  (`docker-buildx-plugin`), and the `docker compose` command
  (`docker-compose-plugin`) — used to run `docker-compose.prod.yml`.

Verified the install:

```
sudo docker run hello-world
```

A short test image download-and-run — it just proves Docker actually works end-to-end
before trusting it with the real app.

## 6. Clone the repository

```
git clone https://github.com/pauljettedev/Firesight.git
cd Firesight
```

This downloads a copy of the codebase onto the droplet itself — the droplet needs its own copy
on disk because that's where the `Dockerfile`, `docker-compose.prod.yml`, and `Caddyfile` it's
about to build and run actually live. This clones whatever is currently on `main`, so `main`
needs to already contain the production deployment files (Dockerfile, compose file, Caddyfile,
`.env.example`) before this step will produce a working setup.

## 7. Create the `.env` file

The app needs a few private settings (a database password, the Claude API key, the domain
name) that must never be stored in Git. They live in a file called `.env` on the droplet only.

```bash
cp .env.example .env
nano .env
```

`.env.example` is a template with placeholder values. Copy it, then replace each placeholder:

- `DOMAIN` — the site's domain (`codewheel.ca`). Caddy uses this to request the HTTPS
  certificate.
- `POSTGRES_PASSWORD` — a new, strong password made up for this server. Nobody types it by
  hand; the app and database both read it from this file.
- `CLAUDE_API_KEY` — an API key created in the Anthropic Console, used by Ask Firesight.

`.env` is listed in `.gitignore`, so Git ignores it. It stays on the droplet and is never
committed or pushed.

## 8. Build and start the app

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

- `-f docker-compose.prod.yml` — use the production setup, not the local-development one.
- `--build` — build the Firesight image from the code just cloned.
- `-d` — run in the background, so the app keeps running after you log out.

This starts three containers: Caddy (handles HTTPS), the Firesight app (API plus the
website), and Postgres/PostGIS (the database). The first build takes several minutes. The app
creates its own database tables on first start, so there's no separate setup step.

Useful checks:

```bash
docker compose -f docker-compose.prod.yml ps          # are all three containers running?
docker compose -f docker-compose.prod.yml logs -f app # watch the app's log output
```

## 9. Confirm the site works

Opened `https://codewheel.ca` in a browser. The first visit can take a few extra seconds while
Caddy gets a free HTTPS certificate from Let's Encrypt; after that it renews automatically.

Check that the page loads over HTTPS (padlock in the address bar), the map appears with
wildfire points on it, and Ask Firesight answers a question.

### Problem found at launch: the map didn't load

The site loaded, but the map failed. The browser's developer tools (Network tab) show two
MapLibre JavaScript files returning "404 Not Found":

- `maplibre-gl-worker.mjs` — a helper script MapLibre runs in the background to draw the map.
- `maplibre-gl-shared.mjs` — a file the worker script loads for itself.

**Why it only broke on the live site:** during local development (`npm run dev`), Vite serves
files straight out of `node_modules`, so everything is found. For production (`npm run
build`), Vite only includes files it knows the app uses. MapLibre finds these two files
itself at runtime, so Vite never saw them and left them out of the build.

**Fix:** tell Vite about both files so it includes them.

- The worker: `WildfireMap.tsx` imports it with `?url`, which makes Vite include the file and
  return its real address. That address is then passed to MapLibre with `setWorkerUrl()`.
- The shared file: the worker asks for it by a fixed name, `./maplibre-gl-shared.mjs`, so it
  has to exist under exactly that name. `vite.config.ts` uses `vite-plugin-static-copy` to
  copy it into the build unchanged.

**Lesson:** a working `npm run dev` doesn't prove the production build works. Running `npm
run build` then `npm run preview` locally is a quick way to catch this kind of problem before
deploying.

### Problem found after launch: `www.codewheel.ca` failed

`https://codewheel.ca` worked, but `https://www.codewheel.ca` showed "Secure Connection
Failed" (`SSL_ERROR_INTERNAL_ERROR_ALERT`).

**Why:** DNS sent `www` to the droplet correctly, but the `Caddyfile` only listed the main
domain. Caddy only gets HTTPS certificates for sites listed in the `Caddyfile`, so it had no
certificate for `www` and refused the connection.

**Fix:** added a `www.{$DOMAIN}` block to the `Caddyfile`. Caddy now gets a certificate for
`www` as well, and permanently redirects it to `https://codewheel.ca`, so the site has one
main address.

**Applying it on the droplet:**

```bash
cd ~/Firesight
git pull
docker compose -f docker-compose.prod.yml restart caddy
```

This needs a `restart`, not just `up -d`. The `Caddyfile` is mounted into the Caddy container
as a single file, and `git pull` replaces that file with a new one. A running container keeps
reading the old copy until it restarts, and `up -d` doesn't restart a container whose
settings haven't changed.

---

## Progress so far

- [x] DigitalOcean Project created
- [x] Droplet created (`firesight-prod`, Toronto, 2GB/50GB, Ubuntu 24.04)
- [x] Personal SSH key generated and added
- [x] DNS A records pointing `codewheel.ca` / `www.codewheel.ca` at the droplet
- [x] Logged into the droplet, OS updated
- [x] Docker + Compose plugin installed and verified
- [x] Repository cloned onto the droplet
- [x] Set up `.env` with real secrets
- [x] `docker compose -f docker-compose.prod.yml up -d --build`
- [x] Confirm the site loads over HTTPS (after fixing the MapLibre build issue)
- [ ] Set up the dedicated deploy key + GitHub Actions secrets for continuous deployment
