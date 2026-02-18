---
name: structured-logging-policy
description: 'Apply production-safe structured logging for ASP.NET Core APIs. Use for endpoint diagnostics, correlation context, event consistency, and secret-safe operational logging.'
argument-hint: 'Describe endpoint/flow, operational signals needed, and current logging gaps.'
---

# Structured Logging Policy

## When to Use
- Adding or refactoring backend endpoint logging
- Investigating low signal-to-noise logs
- Preparing APIs for production diagnostics

## Quick Checklist
1. Identify critical events for the flow.
2. Define stable structured fields for each event.
3. Set severity by operator actionability.
4. Verify sensitive data is never logged.

## Deep Workflow
1. Identify critical events for the flow: request accepted, validation failed, external dependency call, operation completed.
2. Define structured fields for each event (operation name, resource id, correlation id, duration, outcome).
3. Set log levels by intent:
   - `Information` for key business milestones
   - `Warning` for expected-but-actionable degradations
   - `Error` for failures requiring operator attention
4. Ensure sensitive data policy: never log secrets, tokens, raw credentials, or PII.
5. Keep message templates stable; prefer structured properties over string interpolation noise.
6. Verify logs remain concise and diagnostic under normal and failure paths.

## Decision Points
- If high-volume endpoint: log summary milestones only, avoid per-item noise.
- If external dependency involved: include target system + latency + result category.
- If retry behavior exists: log retry count and terminal outcome.

## Completion Checks
- Logs are structured and queryable by operation and outcome.
- Event names/properties are consistent across similar endpoints.
- Sensitive fields are excluded or redacted.
- Failures are diagnosable without stack trace spam in normal flows.
