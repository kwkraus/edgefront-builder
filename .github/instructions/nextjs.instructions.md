---
description: Guidance for the Next.js frontend in src/frontend
applyTo: "src/frontend/**"
---

# Next.js Frontend Instructions

These instructions apply to the frontend project under `src/frontend`.

## Instruction Consistency
- After making frontend changes, review this file and any agents/skills that reference the frontend stack to ensure they still match the code.
- See the "Instruction Ecosystem Congruency" section in `copilot-instructions.md` for the full checklist.

## Agent Routing
- Testing (TDD, test strategy) → `tdd-engineer` agent
- UX design and composition → `ui-ux-nextjs` agent
- Accessibility checks → use `frontend-accessibility-and-ux-acceptance` skill via `ui-ux-nextjs`
- If requirements are unclear or missing, ask the user for clarification before inventing behavior.

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
- `lib/api/` for typed API client helpers consuming backend API DTOs.
- `public/` for static assets.
- E2E tests live in `src/frontend/e2e/`.

## Core Dependencies
- Next.js 16 with React 19.
- TypeScript is required for all new files.
- Tailwind CSS v4 is available; prefer utility-first styling.
- Primer React v38 (@primer/react) with @primer/octicons-react for UI components.
- ESLint must remain clean; use `npm run lint`.
- next-auth for Entra ID authentication.
- Playwright for E2E testing (configured in `playwright.config.ts`).

## Authentication
- Login via Entra ID redirect.
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

## UI and UX
- Favor accessible, semantic HTML.
- Use loading and error boundaries for routes.
- Desktop-first; ensure readable on tablet. Mobile not required for V1.
- Keep UI consistent with existing patterns.
- Inline error banners with retry for failed API calls.
- No optimistic updates in V1 (wait for server confirmation).

## Best Practices
- Keep components small and focused.
- Avoid `any`; use accurate types and shared interfaces matching backend API DTOs.
- Use `async` server actions where appropriate.
- Prefer composition over prop drilling.

## Build and Tooling
- `npm run build` should pass for new features.
- `npm run lint` should remain clean.
- Keep formatting consistent with existing lint rules.
