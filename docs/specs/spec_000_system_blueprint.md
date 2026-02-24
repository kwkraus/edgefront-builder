# SPEC-000 — System Blueprint (Build Ready)

## Purpose
Defines V1 architecture baseline, stack, environments, CI/CD, and operational guardrails.

## Tech Stack (V1 Locked)
- .NET Web API (latest LTS)
- EF Core
- Azure SQL
- Entra ID (single tenant)
- Azure App Service (single region)
- Background hosted services (subscription renewal, retries)
- OpenAPI enabled

## Architecture
- Monolith with modular boundaries
- Web UI → REST API → Azure SQL
- Webhook endpoint inside API
- Metrics persisted (no compute-on-read)

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
- Webhook validation (machine endpoint; Graph notification validation, not user JWT)
- Hybrid Graph permission model:
  - Delegated (OBO flow): webinar create/update/delete — requires user's Teams webinar-capable license
  - Application (client credentials): subscriptions, registration/attendance reads, background renewal
- Least-privilege Graph permissions: `VirtualEvent.ReadWrite` (delegated), `VirtualEvent.Read.All` + `VirtualEvent.Read.Chat` (application)

## Definition of Done
- Scaffolding complete
- CI pipeline functional
- Dev deploy validated
- OpenAPI published
