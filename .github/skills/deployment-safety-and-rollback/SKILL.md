---
name: deployment-safety-and-rollback
description: 'Apply deployment safety controls, environment protection, and rollback planning for CI/CD releases.'
argument-hint: 'Describe target environment, release strategy, risk tolerance, and rollback expectations.'
---

# Deployment Safety and Rollback

## When to Use
- Adding or updating deployment jobs
- Defining production release safeguards
- Reviewing rollback readiness before rollout

## Quick Checklist
1. Confirm required pre-deploy gates.
2. Add environment protections and manual approval for production by default.
3. Define rollback trigger and execution path.
4. Document assumptions and blast radius.

## Deep Workflow
1. Classify deployment context by environment and criticality (dev, staging, production).
2. Ensure all required quality gates block deployment by default.
3. Configure environment protection (required reviewers, branch restrictions, concurrency controls), with manual approval as the default production release gate.
4. Define rollout strategy (full, phased, canary) appropriate to risk tolerance.
5. Define rollback path with explicit trigger conditions and recovery actions.
6. Capture operational assumptions, runbook links, and post-deploy verification checks.

## Decision Points
- For production, default to manual approval before deployment unless the prompt explicitly authorizes automated promotion.
- If rollback cannot be executed quickly, reduce rollout scope and add manual approval gates.
- If environment protections are absent for production, treat as a release blocker.
- If deployment mode is `deploy-only`, avoid provisioning changes and focus on safe promotion.

## Completion Checks
- Deployment jobs are gated and protected by environment controls.
- Rollback criteria and actions are concrete and executable.
- Release risk, assumptions, and recovery plan are clearly documented.
