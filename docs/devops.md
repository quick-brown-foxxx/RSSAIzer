# Specification: **Simple-VPS Deployment (Docker Compose) with Header-Based Auth**

This document tells a DevOps engineer exactly **what**, **why**, and **how** to preconfigure deployment of **RSSAIzer** in the “one-command VPS” scenario.  
It assumes no Kubernetes and no external IdP registration: authentication is handled by **Authentik + Outpost** which injects `X-Email` / `X-UserId` headers trusted by the app.

---

## 1 — Common Section (Background & Goals)

| Goal                               | Design choice                            | Rationale                                                     |
| ---------------------------------- | ---------------------------------------- | ------------------------------------------------------------- |
| One-liner install on any clean VPS | `docker compose up -d`                   | Minimises operator actions.                                   |
| End-to-end HTTPS                   | Traefik v3 + Let’s Encrypt               | Automatic certs / renewals.                                   |
| Self-contained user management     | Authentik server + Outpost (ForwardAuth) | Admin never touches OAuth client IDs; only e-mail & password. |
| Zero public ports except 80/443    | Private Docker network                   | Prevents DB/IdP leaks.                                        |
| App sees _only_ headers            | `ProxyHeaderHandler` already in code     | No changes to backend logic.                                  |

### Header contract

| Header     | Value produced by Outpost        |
| ---------- | -------------------------------- |
| `X-Email`  | Primary e-mail of logged-in user |
| `X-UserId` | Stable UUID (`sub` claim)        |

---

## 2 — File Layout

```
deploy/
└── simple-vps/
    ├── docker-compose.yml
    ├── env.sample
    ├── first-run.sh          # generates strong secrets
    └── README.md             # quick-start for future admins
```

---

## 3 — docker-compose.yml

```yaml
version: "3.9"
networks:
  net:

volumes:
  pgdata:

services:
  ########################################
  # 1. Edge Router + TLS
  ########################################
  traefik:
    image: traefik:v3.0
    container_name: traefik
    restart: unless-stopped
    command:
      - "--providers.docker=true"
      - "--providers.docker.exposedbydefault=false"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--entrypoints.web.http.redirections.entrypoint.to=websecure"
      - "--entrypoints.web.http.redirections.entrypoint.scheme=https"
      - "--certificatesresolvers.le.acme.tlschallenge=true"
      - "--certificatesresolvers.le.acme.email=${LE_EMAIL}"
      - "--certificatesresolvers.le.acme.storage=/acme.json"
      - "--api.dashboard=true"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - "/var/run/docker.sock:/var/run/docker.sock:ro"
      - "./acme.json:/acme.json"
    networks: [net]

  ########################################
  # 2. Authentik IdP (core)
  ########################################
  ak-server:
    image: ghcr.io/goauthentik/server:2025.3
    restart: unless-stopped
    environment:
      AUTHENTIK_SECRET_KEY: "${AK_SECRET}"
    networks: [net]

  ########################################
  # 3. Authentik Outpost – ForwardAuth
  ########################################
  ak-outpost:
    image: ghcr.io/goauthentik/proxy:2025.3
    restart: unless-stopped
    environment:
      AUTHENTIK_HOST: "http://ak-server:9000"
      AUTHENTIK_OUTPOST_TYPE: "proxy"
    networks: [net]
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.ak-outpost.rule=Host(`${DOMAIN}`) && PathPrefix(`/outpost.goauthentik.io`)"
      - "traefik.http.routers.ak-outpost.entrypoints=websecure"
      - "traefik.http.routers.ak-outpost.tls.certresolver=le"

  ########################################
  # 4. Application
  ########################################
  rssaiizer:
    image: ghcr.io/yourrepo/rssaiizer:${TG_VERSION:-latest}
    restart: unless-stopped
    depends_on: [postgres]
    environment:
      ASPNETCORE_URLS: "http://+:5110"
      ASPNETCORE_ENVIRONMENT: "Production"
      SINGLEUSERMODE: "false"
      PROXYHEADEREMAIL: "X-Email"
      PROXYHEADERID: "X-UserId"
      CONNECTIONSTRINGS__DEFAULT: >
        Host=postgres;Port=5432;Database=digest;
        Username=postgres;Password=${POSTGRES_PASSWORD};
    networks: [net]
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.digest.rule=Host(`${DOMAIN}`)"
      - "traefik.http.routers.digest.entrypoints=websecure"
      - "traefik.http.routers.digest.tls.certresolver=le"
      - "traefik.http.routers.digest.middlewares=ak@docker"
      # ForwardAuth
      - "traefik.http.middlewares.ak.forwardauth.address=http://ak-outpost:9000/outpost.goauthentik.io/auth/traefik"
      - "traefik.http.middlewares.ak.forwardauth.trustForwardHeader=true"
      - "traefik.http.middlewares.ak.forwardauth.authResponseHeaders=X-Email,X-UserId"

  ########################################
  # 5. Database
  ########################################
  postgres:
    image: postgres:16
    restart: unless-stopped
    environment:
      POSTGRES_PASSWORD: "${POSTGRES_PASSWORD}"
    volumes:
      - pgdata:/var/lib/postgresql/data
    networks: [net]
```

---

## 4 — env.sample

```env
# Domain & TLS
DOMAIN=demo.example.com
LE_EMAIL=admin@example.com

# Postgres
POSTGRES_PASSWORD=change-me-123

# Authentik
AK_SECRET=$(openssl rand -hex 32)   # generated by first-run.sh
```

**first-run.sh** (ships in repo):

```bash
#!/usr/bin/env bash
set -e
[[ -f .env ]] || cp env.sample .env
grep -q AK_SECRET .env || \
  echo "AK_SECRET=$(openssl rand -hex 32)" >> .env
echo "Secrets initialized. Edit .env and run: docker compose up -d"
```

---

## 5 — Operator Checklist

| Step                     | Command / action                                                                                                                   |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------- | --- |
| 1. Install Docker & Git  | `curl -fsSL https://get.docker.com                                                                                                 | sh` |
| 2. Clone repo            | `git clone https://github.com/you/rssaiizer && cd deploy/simple-vps`                                                          |
| 3. Generate secrets      | `./first-run.sh`                                                                                                                   |
| 4. Edit `.env`           | set `DOMAIN`, `LE_EMAIL`, strong `POSTGRES_PASSWORD`                                                                               |
| 5. Initialise acme store | `touch acme.json && chmod 600 acme.json`                                                                                           |
| 6. Launch stack          | `docker compose --env-file .env up -d`                                                                                             |
| 7. First-run wizard      | Visit `https://<DOMAIN>/if/flow/initial-setup/` → set admin e-mail & password, decide “Allow self-registration?”.                  |
| 8. Verify login          | Browse to `https://<DOMAIN>/` → you should be redirected to Authentik login, then land in RSSAIzer logged in as your e-mail. |

---

## 6 — Smoke-Test Matrix

| Scenario                        | Expected result                                              |
| ------------------------------- | ------------------------------------------------------------ |
| Unauthenticated request to `/`  | 302 → Authentik login page                                   |
| Valid login                     | 200 OK, user e-mail visible in app navbar                    |
| Second request (cookie present) | No redirect; headers `X-Email`, `X-UserId` arrive at backend |
| Wrong password                  | Authentik shows error; backend never hit                     |
| Traefik dashboard               | `https://<DOMAIN>/dashboard/#/` shows routers **green**      |

---

## 7 — Operational Notes

- **Back-ups** – persist `pgdata/` volume and `acme.json`. Authentik stores config in its own SQLite inside the `ak-server` container; back up `/media` if custom themes are added.
- **Upgrades** – pull newer tags and `docker compose pull && docker compose up -d`. Schema migrations for Authentik are automatic.
- **User self-service** – toggle in Authentik UI → _Flows_ → _default-authentication_ → “Enrollment” stage.

---

## 8 — Deliverable Acceptance

1. `docker compose up -d` succeeds on a fresh Ubuntu 24.04 VPS.
2. HTTPS cert issued by Let’s Encrypt within 60 seconds.
3. Login / logout flows work; headers visible via `curl -I -H "Cookie:…" https://<DOMAIN>/` for debugging.
4. No container exposes ports other than 80/443 to the host when you run `docker ps`.

---

**End of file – hand to DevOps and enjoy the one-command deploy.**

# TODO

- add logout url for reverse proxy mode + authentik outpost
