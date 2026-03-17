# SPEC-000 — System Blueprint (Build Ready)

> Historical note: this blueprint predates the local-only refactor. The current shipped architecture uses local CSV imports for registrations, attendance, and Q&A; Teams/Graph sections below should be treated as legacy design context unless explicitly reactivated.

## Purpose
Defines V1 architecture baseline, stack, environments, CI/CD, and operational guardrails.

## Tech Stack (V1 Locked)
- .NET Web API (latest LTS)
- EF Core
- Azure SQL
- Entra ID (single tenant)
- Azure App Service (single region)
- OpenAPI enabled

## Architecture
- Monolith with modular boundaries
- Web UI → REST API → Azure SQL
- Metrics persisted (no compute-on-read)
- Data sync (registrations, attendance) is user-initiated via delegated token on page load

## Repo Structure
/src/frontend
/src/backend
/libs/contracts
/tests
/infra

## Environments
Local, Dev, Prod

## CI/CD
- Build
- Unit tests
- Integration tests
- OpenAPI validation
- Deploy Dev
- Manual approval → Prod

## Security
- JWT validation for all user/business endpoints
- Delegated-only Graph permission model (no application permissions):
  - All Graph API calls use OBO flow — user must be authenticated
  - Single delegated permission: `VirtualEvent.ReadWrite`
  - User must have Teams account with webinar-capable license

## App Registration Requirements
- **Type:** Single-tenant
- **Platform:** Web (redirect URI for frontend auth)
- **API permissions (Delegated only):**
  - `openid`, `profile`, `email`, `offline_access` — standard OIDC
  - `VirtualEvent.ReadWrite` — Teams virtual events (create, read, update, delete webinars, registrations, attendance)
- **No application permissions required**
- **Expose an API:** `api://{ClientId}/access_as_user` scope for frontend → backend token exchange
- **Client secret:** Required for OBO token exchange

## Definition of Done
- Scaffolding complete
- CI pipeline functional
- Dev deploy validated
- OpenAPI published
