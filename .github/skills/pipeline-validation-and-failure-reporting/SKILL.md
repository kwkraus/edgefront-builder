---
name: pipeline-validation-and-failure-reporting
description: 'Run targeted CI/CD validation, report pass/fail clearly, and provide actionable next steps with minimal noise.'
argument-hint: 'Describe changed workflow scope and the confidence level needed before merge/deploy.'
---

# Pipeline Validation and Failure Reporting

## When to Use
- After any workflow or gate change
- During CI/CD incident response for failed runs
- Before handoff or merge of pipeline updates

## Quick Checklist
1. Run narrow validation for changed workflow scope.
2. Capture pass/fail by gate and component.
3. Separate blockers from follow-up improvements.
4. Publish concise next actions.

## Deep Workflow
1. Select minimal validation commands for touched assets first, then broaden only if risk remains.
2. Validate workflow syntax and command path correctness before full pipeline execution.
3. Run component-specific checks independently where possible to isolate failures quickly.
4. Report outcomes with structure: what ran, what passed, what failed, and impact.
5. Classify failures into deployment-blocking vs non-blocking with rationale.
6. Provide concrete remediation and rerun scope for each blocking failure.

## Decision Points
- If failures are unrelated to workflow edits, record as pre-existing and avoid unrelated fixes.
- If a required gate fails, treat release as blocked until resolved.
- If confidence is low after narrow checks, expand verification scope incrementally.

## Completion Checks
- Validation scope matches changed pipeline surface area.
- Results are explicit, reproducible, and actionable.
- Blocking vs non-blocking outcomes are clearly identified.
