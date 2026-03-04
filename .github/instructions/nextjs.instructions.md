---
description: Guidance for the Next.js frontend in src/frontend
applyTo: "src/frontend/**"
---

# Next.js Frontend Instructions

These instructions apply to the frontend project under `src/frontend`.

## Instruction Consistency
- After making frontend changes, review this file and update it if guidance is no longer accurate.
- Keep the instruction set aligned with the current architecture and dependencies.

## Spec Authority
- All implementation must reference and conform to the authoritative specs in `docs/specs/`.
- SPEC-100 defines screens, field lists, UX contracts, loading/error states, and auth flow.
- SPEC-110 defines API endpoints and response DTOs the frontend consumes.
- If a required UX rule is missing in a spec, add `TODO-SPEC` comment and stop.

## Architecture
- Use the App Router (`app/`) with React Server Components by default.
- Keep pages thin: route handlers and server components orchestrate; UI components render.
- Organize by feature using `app/<feature>/` and colocate components and styles.
- Extract shared UI into `components/` and shared utilities into `lib/`.

## Project Structure
- `app/` for routes, layouts, pages, loading and error states.
- `app/series/` for series list, detail, create/edit flows.
- `app/sessions/` for session detail, edit flows.
- `components/` for reusable UI; keep them pure and prop-driven.
- `lib/` for data access, API clients, and helpers.
- `lib/api/` for typed API client helpers consuming SPEC-110 DTOs.
- `public/` for static assets.
- E2E tests live in `src/frontend/e2e/`.

## Core Dependencies
- Next.js 16 with React 19.
- TypeScript is required for all new files.
- Tailwind CSS is available; prefer utility-first styling.
- ESLint must remain clean; use `npm run lint`.
- MSAL.js or next-auth for Entra ID authentication.

## Authentication
- Login via Entra ID redirect per SPEC-100.
- Unauthenticated access → redirect to Entra login.
- Token refresh handled silently; on failure → redirect to login.
- Logout clears session and redirects to login.
- Pass user JWT to backend API for all authenticated requests.

## Data Fetching and State
- Prefer server components for data fetching.
- Use `fetch` with caching strategies (`no-store`, `force-cache`, revalidate).
- Keep client state local; use context only when needed.
- Avoid global stores unless the feature truly requires it.
- Metrics are read-only from API (no compute-on-read).
- No pagination in V1 — list endpoints return all results.

## Screens and UX Contracts (per SPEC-100)
- Series List: title, status, session count, metrics summary.
- Series Detail: header with SeriesMetrics, sessions table with per-session metrics/status and "Last Synced" column.
- Session Create/Edit (Draft): title, startsAt, endsAt fields; save locally only.
- Session Edit (Published): "Save & Publish to Teams" atomic action.
- Publish Series: confirmation dialog, progress indicator, failure with retry.
- Drift: badges and comparison values per SPEC-100 display rules.
- Data Sync: auto-sync on page load for published sessions; "Syncing from Teams…" indicator; "Last Synced" timestamps.
- Delete: confirmation dialogs for both session and series deletion.
- Teams licensing error: "webinar license required" message, no retry.
- Empty states: defined per screen in SPEC-100.

## UI and UX
- Favor accessible, semantic HTML.
- Use loading and error boundaries for routes.
- Desktop-first; ensure readable on tablet. Mobile not required for V1.
- Keep UI consistent with existing patterns.
- Inline error banners with retry for failed API calls.
- No optimistic updates in V1 (wait for server confirmation).

## Test-Driven Development (TDD)
- Write tests first: red -> green -> refactor.
- Use React Testing Library and Jest or Vitest as configured.
- Test user-visible behavior, not implementation details.
- Add integration tests for critical flows: publish, atomic edit, delete.

## Best Practices
- Keep components small and focused.
- Avoid `any`; use accurate types and shared interfaces matching SPEC-110 DTOs.
- Use `async` server actions where appropriate.
- Prefer composition over prop drilling.

## Build and Tooling
- `npm run build` should pass for new features.
- `npm run lint` should remain clean.
- Keep formatting consistent with existing lint rules.
