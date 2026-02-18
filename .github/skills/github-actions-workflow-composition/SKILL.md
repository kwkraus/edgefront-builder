---
name: github-actions-workflow-composition
description: 'Design and modify GitHub Actions workflows with deterministic job boundaries, safe caching, and clear failure signals.'
argument-hint: 'Describe desired workflow behavior, triggers, job boundaries, and failure visibility requirements.'
---

# GitHub Actions Workflow Composition

## When to Use
- Creating new workflow files or major workflow edits
- Refactoring jobs for speed and diagnosability
- Standardizing naming, dependencies, and reusable patterns

## Quick Checklist
1. Define trigger and branch scope.
2. Split jobs by component and responsibility.
3. Add deterministic job ordering with `needs`.
4. Add cache and artifact strategy only where deterministic.

## Deep Workflow
1. Define trigger intent (push, pull request, workflow dispatch, release) and branch/environment scope.
2. Partition work into focused jobs (frontend checks, backend checks, package, deploy) with explicit names.
3. Use `needs` to enforce required order while preserving parallelism for independent checks.
4. Add dependency/build caching with stable keys and safe fallback behavior.
5. Keep failure output localized by step name and command scope.
6. Validate path references, action versions, and required permissions.

## Decision Points
- If a change touches one component, avoid broad edits to unrelated jobs.
- If cache invalidation risk is high, prioritize correctness over cache aggressiveness.
- If gates are required before deploy, wire deploy jobs strictly behind gate job success.

## Completion Checks
- Workflow graph is deterministic and easy to diagnose.
- Job names, triggers, and dependencies match pipeline intent.
- Caching and artifacts improve speed without compromising reliability.
