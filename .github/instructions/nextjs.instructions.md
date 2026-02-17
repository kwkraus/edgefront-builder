---
description: Guidance for the Next.js frontend in src/frontend
applyTo: "src/frontend/**"
---

# Next.js Frontend Instructions

These instructions apply to the frontend project under `src/frontend`.

## Instruction Consistency
- After making frontend changes, review this file and update it if guidance is no longer accurate.
- Keep the instruction set aligned with the current architecture and dependencies.

## Architecture
- Use the App Router (`app/`) with React Server Components by default.
- Keep pages thin: route handlers and server components orchestrate; UI components render.
- Organize by feature using `app/<feature>/` and colocate components and styles.
- Extract shared UI into `components/` and shared utilities into `lib/`.

## Project Structure
- `app/` for routes, layouts, pages, loading and error states.
- `components/` for reusable UI; keep them pure and prop-driven.
- `lib/` for data access, API clients, and helpers.
- `public/` for static assets.
- Tests live in `tests/frontend/` mirroring `app/` features.

## Core Dependencies
- Next.js 16 with React 19.
- TypeScript is required for all new files.
- Tailwind CSS is available; prefer utility-first styling.
- ESLint must remain clean; use `npm run lint`.

## Data Fetching and State
- Prefer server components for data fetching.
- Use `fetch` with caching strategies (`no-store`, `force-cache`, revalidate).
- Keep client state local; use context only when needed.
- Avoid global stores unless the feature truly requires it.

## UI and UX
- Favor accessible, semantic HTML.
- Use loading and error boundaries for routes.
- Ensure responsive layouts for mobile and desktop.
- Keep UI consistent with existing patterns.

## Test-Driven Development (TDD)
- Write tests first: red -> green -> refactor.
- Use React Testing Library and Jest or Vitest as configured.
- Test user-visible behavior, not implementation details.
- Add integration tests for critical flows.

## Best Practices
- Keep components small and focused.
- Avoid `any`; use accurate types and shared interfaces.
- Use `async` server actions where appropriate.
- Prefer composition over prop drilling.

## Build and Tooling
- `npm run build` should pass for new features.
- `npm run lint` should remain clean.
- Keep formatting consistent with existing lint rules.
