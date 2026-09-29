# Server setup walkthrough

Setting up the production server from nothing, step by step, with what each step does and why.
It's written for someone who hasn't set up a server, domain or DNS recently.
[deployment.md](deployment.md) is the short reference for day-to-day deploys.

The finished setup:

- one DigitalOcean droplet running Ubuntu
- **Caddy**, from the [codewheel repo](https://github.com/pauljettedev/codewheel), handling
  HTTPS for every codewheel.ca site
- **Firesight**, from this repo, running as two containers (the app and its database)

Why it's built this way: [ADR 010](decisions/010-hosting.md).

## 1. Create the droplet

A droplet is DigitalOcean's name for a virtual server: a machine with a fixed IP address that
you log into over SSH and can install anything on.

In DigitalOcean, first create a **Project** (a folder for grouping resources; it doesn't affect
how anything runs), then create the droplet inside it with these settings:

| Setting | Choice | Why |
| --- | --- | --- |
| Region | Toronto | Closest to the owner |
| Image | Ubuntu 24.04 LTS | Stable and supported for years. Docker is installed separately in step 4. |
| Plan | Basic, Regular CPU, 2 GB RAM, 50 GB disk ($12/month) | The server runs three containers: Caddy, the app and PostgreSQL. 1 GB is too tight for all three. |
| Backups | Off | The wildfire data is rebuilt from CWFIS on the next sync, so there's nothing worth paying to back up. |
| Networking | IPv4 only | IPv6 would need extra DNS records for no real benefit. |
| Monitoring | On (free) | CPU, memory and disk graphs, with optional alerts. |
| Managed database | Off | PostgreSQL runs in a container on the droplet instead, so hosting is one droplet. |
| Authentication | SSH key | See step 2. Safer than a password. |
| Hostname | `firesight-prod` | |

Note the droplet's IP address. The steps below call it `<droplet-ip>`.

## 2. Add your SSH key

An SSH key pair replaces a password. The **private** key stays on your own computer and is never
shared. The **public** key goes on the server. When you connect, the server checks that you
hold the matching private key, and no password is ever sent.

On your own computer (PowerShell works):

```bash
ssh-keygen -t ed25519            # accept the default location
cat ~/.ssh/id_ed25519.pub        # prints the public key
```

Paste the public key into DigitalOcean's **Add a public SSH key** box while creating the
droplet. DigitalOcean installs it on the server for you.

This key is for you to log in. Automatic deploys use a separate key, set up in step 8.

## 3. Point the domain at the droplet

The domain `codewheel.ca` is registered at Namecheap. DNS is what turns a name like
`firesight.codewheel.ca` into the droplet's IP address.

In Namecheap, open the domain's **Advanced DNS** tab:

1. Delete Namecheap's parking records: the `www` CNAME pointing at `parkingpage.namecheap.com`,
   and the redirect to `http://www.codewheel.ca/`. They'd conflict with the records below.
2. Add these A records (an A record means "this name is at this IP address"):

   | Type | Host | Value | TTL |
   | --- | --- | --- | --- |
   | A | `@` | `<droplet-ip>` | Automatic |
   | A | `www` | `<droplet-ip>` | Automatic |
   | A | `firesight` | `<droplet-ip>` | Automatic |

   `@` is `codewheel.ca` itself. `firesight` is `firesight.codewheel.ca`.

Leave the mail TXT record alone; it's for email, not the website. DNSSEC and Dynamic DNS stay
off.

DNS changes take anywhere from a few minutes to a few hours to reach everyone.

## 4. Log in and install Docker

```bash
ssh root@<droplet-ip>
```

The first time you connect, SSH asks whether to trust the server. Answer `yes`. It only asks
once per server. If it ever asks again for a server you already use, stop: the server's
identity has changed.

Install updates:

```bash
sudo apt update && sudo apt upgrade -y
```

If it asks about a changed `/etc/ssh/sshd_config`, **keep the local version** (the default).
DigitalOcean customises that file to allow your SSH key, and replacing it can lock you out.

Install Docker using Docker's official method for Ubuntu:

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

In short: the first five commands add Docker's own software source to Ubuntu, with Docker's
signing key so Ubuntu can check the downloads are genuine. The last two install Docker and the
`docker compose` command.

Check it works:

```bash
sudo docker run hello-world
```

## 5. Start Caddy

Caddy sits in front of every site on the server. It gets and renews free HTTPS certificates
from Let's Encrypt, and forwards each request to the right app. It lives in the codewheel repo
so that no single project owns it.

```bash
docker network create codewheel      # once per server; apps join this to reach Caddy
git clone https://github.com/pauljettedev/codewheel.git
cd codewheel
docker compose up -d
cd ~
```

The codewheel repo's `Caddyfile` already lists `codewheel.ca`, `www.codewheel.ca` (redirected
to `codewheel.ca`) and `firesight.codewheel.ca`. Its README covers adding more sites.

## 6. Start Firesight

```bash
git clone https://github.com/pauljettedev/Firesight.git
cd Firesight
cp .env.example .env
nano .env
```

`.env` holds the two secrets, which are never stored in git:

- `POSTGRES_PASSWORD`: make up a new, strong password. Nobody types it; the app and the
  database both read it from this file.
- `CLAUDE_API_KEY`: create one in the Anthropic Console. Ask Firesight uses it.

Then build and start:

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

- `-f docker-compose.prod.yml`: use the production setup, not the local one
- `--build`: build the app from the code you just cloned
- `-d`: keep running after you log out

This starts two containers: `firesight-app` (the API and the website) and `firesight-postgres`
(the database). The first build takes several minutes. The app creates its database tables
itself on first start.

To check on it:

```bash
docker compose -f docker-compose.prod.yml ps           # both containers running?
docker compose -f docker-compose.prod.yml logs -f app  # watch the app's log
```

## 7. Check the site

Open `https://firesight.codewheel.ca`. The first visit can take a few seconds while Caddy gets
the HTTPS certificate.

Check that:

- the address bar shows a padlock
- the map shows wildfires (the first sync runs when the app starts)
- Ask Firesight answers a question

## 8. Turn on automatic deploys

So that every push to `main` deploys itself, follow **Setting up automatic deploys** in
[deployment.md](deployment.md#setting-up-automatic-deploys). It covers the deploy key, the
GitHub secrets and the server fingerprint.
