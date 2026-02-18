---
name: telemetry-signal-design
description: 'Define logs, metrics, and traces for critical user/API flows with clear operator actionability and low noise.'
argument-hint: 'Describe target flow, required diagnostics, and current observability gaps.'
---

# Telemetry Signal Design

## When to Use
- Adding or revising observability for a feature or endpoint
- Improving signal-to-noise for production diagnostics
- Aligning logs/metrics/traces across frontend-backend flows

## Quick Checklist
1. Identify critical flow milestones and failure points.
2. Define minimal, actionable logs/metrics/traces.
3. Add correlation context and stable event naming.
4. Validate noise level and operator usefulness.

## Deep Workflow
1. Identify the user/API flow boundaries and top operational questions to answer.
2. Define key events and outcomes for each stage (start, success, degraded, failure).
3. Choose telemetry types by question:
   - Logs for event context
   - Metrics for trend/threshold visibility
   - Traces for latency and dependency breakdown
4. Define stable dimensions (operation name, endpoint, status/result category, environment, correlation id).
5. Apply sampling and level guidance to keep default noise low.
6. Verify each signal maps to an actionable operator response.

## Decision Points
- If an event has no operational action, remove or downgrade it.
- If flow spans services, require trace/correlation continuity.
- If volume is high, prioritize aggregated metrics and milestone logs.

## Completion Checks
- Critical flow stages are observable end-to-end.
- Signal set is minimal, structured, and actionable.
- Telemetry dimensions are consistent and queryable.
