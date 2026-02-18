---
name: nextjs-ui-composition-patterns
description: 'Compose Next.js App Router UI with thin routes, reusable components, and clear server/client boundaries.'
argument-hint: 'Describe the UI change, route scope, and client behavior needs.'
---

# Next.js UI Composition Patterns

## When to Use
- Building or refactoring frontend routes/pages in `src/frontend`
- Splitting large page logic into reusable components/helpers
- Clarifying server vs client component boundaries

## Quick Checklist
1. Keep route files orchestration-only.
2. Extract reusable UI into `components/` and helpers into `lib/`.
3. Default to server components; add client components only when needed.
4. Preserve existing visual and interaction patterns.

## Deep Workflow
1. Identify whether the requested behavior is route composition, reusable component work, or helper logic.
2. Keep `app/` route files thin by delegating UI blocks to `components/` and non-UI helpers to `lib/`.
3. Choose rendering boundary deliberately:
   - Server components by default
   - Client components only for browser-only interactivity/state
4. Ensure loading/error states exist for async experiences where users wait or recover.
5. Reuse existing style/util patterns; avoid introducing new design systems.
6. Validate final structure for maintainability and minimal coupling.

## Decision Points
- If behavior does not require browser APIs or local interactive state, keep it server-side.
- If component reuse is likely across routes, extract it immediately to `components/`.
- If a route grows beyond orchestration scope, split into focused presentation and helper units.

## Completion Checks
- Route files remain thin and focused on composition.
- Server/client boundaries are intentional and minimal.
- Reusable UI and helper logic are placed in canonical folders.
