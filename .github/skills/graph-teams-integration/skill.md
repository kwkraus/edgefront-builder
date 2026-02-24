---
name: graph-teams-integration
description: 'Design and implement Microsoft Graph Virtual Events integration with hybrid OBO/client-credential token flows, webinar lifecycle, and subscription management.'
argument-hint: 'Describe the Graph operation, token flow context, and target SPEC section.'
---

# Graph Teams Integration

## When to Use
- Implementing webinar create/update/delete via Graph API
- Setting up OBO token exchange or client credential flows
- Managing Graph subscription lifecycle (create, renew, delete)
- Implementing drift detection against Graph metadata
- Any work involving Microsoft.Identity.Web or Microsoft.Graph SDK

## Quick Checklist
1. Identify operation and required token flow (OBO vs client credentials).
2. Verify Graph API endpoint and permission requirements.
3. Implement centralized token acquisition via TeamsGraphClient.
4. Add error handling with correlation IDs and Graph-specific failure modes.

## Deep Workflow
1. Classify the operation:
   - Webinar CRUD → OBO flow (delegated `VirtualEvent.ReadWrite`)
   - Registration/attendance reads → client credentials (`VirtualEvent.Read.Chat`)
   - Subscription management → client credentials (`VirtualEvent.Read.All`)
   - Background renewal → client credentials (no user present)
2. Implement token acquisition:
   - OBO: Extract user JWT from request → call `ITokenAcquisition.GetAccessTokenForUserAsync` with Graph scopes
   - Client credentials: Use `IConfidentialClientApplication` or `GraphServiceClient` with app-only auth
3. Implement the Graph API call with retry and error classification:
   - 401/403: Token or permission issue — log and surface clearly
   - 404: Resource not found — handle gracefully for drift/delete
   - 429: Throttled — respect Retry-After header
   - 5xx: Transient — retry with exponential backoff
4. Map Graph response to domain model (e.g., webinar ID → teamsWebinarId).
5. Log all Graph operations with correlation ID, operation name, and result.

## Publish Flow (SPEC-200)
1. For each session in series:
   a. Create webinar via OBO → store teamsWebinarId
   b. Create registration subscription via client credentials → store subscriptionId + expirationDateTime
   c. Create attendance report subscription via client credentials → store subscriptionId + expirationDateTime
2. If any step fails:
   a. Run compensating rollback: best-effort delete created webinars + subscriptions
   b. If rollback fails: log failures, surface partial-failure state
   c. Return failure to caller

## Subscription Renewal (Background Worker)
1. Query GraphSubscription table for expiring subscriptions (within renewal window).
2. Filter to sessions where status=Published and reconcileStatus != Disabled.
3. PATCH subscription via client credentials to extend expirationDateTime.
4. On failure: exponential backoff, 24h retry window, then mark reconcileStatus=Disabled.
5. On successful reconciliation: DELETE subscriptions for that session.

## Decision Points
- If Graph API returns licensing error (e.g., user lacks Teams Premium): classify as non-retryable, surface clear message.
- If subscription renewal fails beyond 24h window: mark Disabled and stop retrying.
- If drift detection fetch fails: keep previous driftStatus, do not clear.
- If webinar delete fails during series/session delete: log as best-effort failure, continue with local delete.

## Completion Checks
- Token flow matches operation requirements (never OBO in background, never client creds for webinar CRUD).
- All Graph calls have error handling with correlation IDs.
- Publish is atomic with compensating rollback.
- Subscription lifecycle is fully automated.
- Integration tests mock Graph API responses for all flows.
