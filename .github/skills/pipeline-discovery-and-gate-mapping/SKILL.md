---
name: pipeline-discovery-and-gate-mapping
description: 'Discover existing CI/CD assets and map required quality gates by component and environment. Use for baseline audits before changing GitHub Actions workflows.'
argument-hint: 'Describe target components, environments, and required acceptance gates.'
---

# Pipeline Discovery and Gate Mapping

## When to Use
- Before modifying or creating any CI/CD workflow
- When pipeline ownership or current checks are unclear
- When environment-specific gates need explicit definition

## Quick Checklist
1. Inventory workflow files and trigger scopes.
2. Discover frontend/backend build and test commands from manifests.
3. Map required gates by component and environment.
4. Record missing gates and deployment blockers.

## Deep Workflow
1. Discover workflow assets (`.github/workflows`) and identify events, branches, and environments.
2. Inspect component manifests and project files to derive canonical commands:
   - Frontend scripts from `src/frontend/package.json`
   - Backend checks from `src/backend` project/solution context
3. Build a gate matrix per component and stage (lint, typecheck, build, test, artifact validation, deployment approval).
4. Mark each gate as required, optional, or not applicable with rationale.
5. Identify missing or weak gates and rank by deployment risk.
6. Produce a minimal change proposal that closes the highest-risk gaps first.

## Decision Points
- If a command is missing from manifests, fail discovery and request explicit command definition.
- If frontend/backend are independent, prefer separate jobs and parallel execution.
- If a deployment stage lacks protection, treat as high priority before rollout expansion.

## Completion Checks
- Current pipeline triggers, jobs, and gates are fully inventoried.
- Required gates are explicit per component and environment.
- Gaps are prioritized with concrete, minimal follow-up changes.
