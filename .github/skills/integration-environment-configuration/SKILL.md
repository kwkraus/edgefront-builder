---
name: integration-environment-configuration
description: 'Validate cross-stack environment configuration for local/dev/prod without hardcoded service endpoints. Use for config source mapping and runtime verification.'
argument-hint: 'Describe target environments, required variables, and suspected configuration gaps.'
---

# Integration Environment Configuration

## When to Use
- Frontend-backend URL wiring changes
- New deployment environment setup
- Runtime failures likely caused by misconfiguration

## Quick Checklist
1. Enumerate required configuration keys per component.
2. Verify source of truth per environment.
3. Confirm no hardcoded service URLs in runtime code.
4. Validate startup/runtime behavior with effective config.

## Deep Workflow
1. Build a configuration matrix by environment for frontend and backend.
2. Map each key to source of truth (local `.env`, app settings, secrets, environment variables).
3. Verify configuration naming consistency across components and deployment assets.
4. Check frontend API base URL resolution and backend binding expectations.
5. Validate failure behavior for missing/invalid configuration.
6. Provide minimal remediation steps for any gap found.

## Decision Points
- If production value source is unclear, treat as a deployment blocker.
- If config exists but naming diverges across stacks, align names before release.
- If local and deployed behavior differ, document environment-specific expectations explicitly.

## Completion Checks
- Required configuration is explicit for each environment.
- URL/service wiring is environment-driven and non-hardcoded.
- Misconfiguration risks are identified with concrete fixes.
