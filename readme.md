# EdgeFront Builder

EdgeFront Builder is a **local-first webinar planning and analytics platform**. It helps organizations organize series and sessions, upload session-scoped CSV data, and review aggregated engagement metrics without requiring an active Teams or Microsoft Graph integration.

![About EdgeFront Builder](https://github.com/user-attachments/assets/674c414a-8e81-47f5-90e1-6695f224319a)

---

## Key Capabilities

- **Series & session management** — Create webinar series containing multiple sessions and manage their local titles and schedules.
- **Session-scoped imports** — Upload registration, attendance, and Q&A CSV files for each session and replace imported data deterministically.
- **Local analytics** — Persist aggregated engagement metrics per session and across a series for fast reads and reporting.
- **Metrics & analytics** — Aggregated engagement metrics per session and across a series: total registrations, attendees, unique account domains, and warm-account influence tracking.
- **Entra ID authentication** — Single-tenant login via Entra ID for authenticated series/session ownership and API access.

---

## Screenshots

> Some screenshots predate the local-only refactor and will be refreshed in a future pass.

### Sign In

![Login](docs/screenshots/login.png)

### Series List

![Series List](docs/screenshots/series-list.png)

### Create a Series

![Create Series](docs/screenshots/series-create.png)

### Series Detail with Metrics

![Series Detail](docs/screenshots/series-detail.png)

### Series Publish View (legacy screenshot)

![Publish Series](docs/screenshots/series-publish.png)

### Create a Session

![Create Session](docs/screenshots/session-create.png)

### Session Detail — Draft

![Session Detail (Draft)](docs/screenshots/session-detail-draft.png)

### Session Detail — Published with Sync & Metrics

![Session Detail (Published)](docs/screenshots/session-detail-published.png)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS v4, Primer React v38 |
| Backend | ASP.NET Core Minimal API, .NET 10, EF Core |
| Database | Azure SQL |
| Auth | Microsoft Entra ID (next-auth, single-tenant) |
| Data ingestion | Session-scoped CSV imports for registrations, attendance, and Q&A |
| Hosting | Azure App Service |

---

## Project Structure

```text
docs/           # Setup guides, screenshots
src/
  backend/      # ASP.NET Core Minimal API
  frontend/     # Next.js App Router app
tests/
  backend/      # xUnit tests for backend
tools/          # PowerShell scripts (e.g., Entra app registration)
```

---

## Architecture Overview

- **Monolith with modular boundaries** — vertical-slice feature organization in the backend, App Router feature directories in the frontend.
- **Local-first data model** — series, sessions, imported source data, and computed metrics are stored locally in Azure SQL.
- **Manual import workflow** — registrations, attendance, and Q&A enter the system through authenticated CSV uploads; there are no background jobs or webhooks in the active product flow.
- **Metrics persisted on import** — all metric aggregations are computed and stored on write; no compute-on-read.
- **Legacy Graph code retained** — dormant Teams/Microsoft Graph services remain in the backend codebase for compatibility work, but they are not part of the active local-only architecture.

---

## Authentication Setup

See [`docs/setup-entra-permissions.md`](docs/setup-entra-permissions.md) for the current local-only authentication requirements and notes about archived Graph scopes.
