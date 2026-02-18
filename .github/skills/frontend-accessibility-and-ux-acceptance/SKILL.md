---
name: frontend-accessibility-and-ux-acceptance
description: 'Apply accessibility and UX acceptance checks for Next.js interfaces, including semantics, keyboard flow, responsive behavior, and async states.'
argument-hint: 'Describe the user flow, UI surfaces changed, and accessibility/UX risks.'
---

# Frontend Accessibility and UX Acceptance

## When to Use
- Any user-visible UI changes in `src/frontend`
- PR reviews focused on accessibility/UX regressions
- Final acceptance pass before merge for frontend changes

## Quick Checklist
1. Verify semantic structure and accessible naming.
2. Verify keyboard navigation and focus behavior.
3. Verify responsive behavior for key viewports.
4. Verify loading/error/empty states for async flows.

## Deep Workflow
1. Map the primary user flow affected by the change and identify key interaction surfaces.
2. Validate semantic elements and labels (forms, controls, headings, landmarks).
3. Validate keyboard behavior (tab order, reachable controls, visible focus, no keyboard traps).
4. Validate contrast/readability and state clarity without relying solely on color cues.
5. Validate responsive behavior at representative breakpoints for layout and interaction continuity.
6. Validate async UX states (loading, error, empty, retry) for understandable recovery paths.

## Decision Points
- If semantics conflict with visual implementation, prioritize accessibility-safe markup.
- If responsiveness causes content clipping or interaction loss, block completion until resolved.
- If async failures are possible, require an explicit user-facing error/retry state.

## Completion Checks
- Accessibility fundamentals are preserved or improved.
- UX remains clear and consistent across devices and states.
- No critical keyboard, semantic, or async-state regressions remain.
