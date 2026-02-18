---
name: incident-triage-and-diagnostics
description: 'Run a focused triage workflow for reliability incidents, isolate likely fault domains, and define reproducible remediation steps.'
argument-hint: 'Describe symptoms, timeframe, impacted flows, and available telemetry or logs.'
---

# Incident Triage and Diagnostics

## When to Use
- Active reliability incident or recurring degradation
- Failed deployment or sudden change in error/latency patterns
- Need fast isolation of likely fault domains

## Quick Checklist
1. Define impact, blast radius, and timeframe.
2. Correlate symptoms across logs, metrics, and traces.
3. Isolate probable fault domain and confidence level.
4. Recommend immediate mitigation and verification steps.

## Deep Workflow
1. Capture incident scope: impacted users/flows, first-seen time, severity, and current status.
2. Establish a timeline using key telemetry markers (error spikes, latency shifts, deploy/config events).
3. Segment by likely domains (frontend, backend API, dependency, configuration, infrastructure).
4. Test the top hypothesis with focused evidence queries and minimal reproducible checks.
5. Define short-term mitigation (rollback, feature flag, throttling, config correction) with tradeoffs.
6. Define validation checks to confirm recovery and prevent false positives.

## Decision Points
- If evidence is mixed, prioritize mitigations with lowest blast radius first.
- If root cause confidence is low, separate mitigation from root-cause claims.
- If incident ties to recent release, evaluate rollback path immediately.

## Completion Checks
- Impact and suspected fault domain are explicit.
- Mitigation steps are concrete and reversible.
- Recovery validation criteria are clear and measurable.
- Next root-cause actions are documented for follow-up.
