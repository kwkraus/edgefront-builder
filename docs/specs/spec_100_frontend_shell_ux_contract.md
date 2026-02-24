# SPEC-100 — Frontend Shell & UX Contract (Build Ready)

## Scope
Authenticated web UI for Series, Sessions, Metrics.

## Authentication Flow
- Login via Entra ID redirect (MSAL.js / next-auth)
- On unauthenticated access → redirect to Entra login
- After login → redirect to Series List
- Logout clears session and redirects to login
- Token refresh handled silently; on failure → redirect to login

## Screens

### Series List
- Fields: title, status (Draft/Published), session count, totalRegistrations, totalAttendees, uniqueAccountsInfluenced, reconcile status badge (if any session Retrying/Disabled)
- Default sort: createdAt descending
- Empty state: "No series yet. Create your first series."
- Actions: Create Series, click row → Series Detail

### Series Detail
- Header: title, status, SeriesMetrics summary (totalRegistrations, totalAttendees, uniqueAccountsInfluenced, warmAccounts count)
- Sessions table: title, startsAt, endsAt, status, totalRegistrations, totalAttendees, reconcileStatus, driftStatus
- Empty state (no sessions): "No sessions in this series. Add a session."
- Actions: Add Session, Edit Series, Delete Series, Publish Series (if Draft)
- Delete Series: confirmation dialog ("This will delete all sessions and their Teams webinars. Continue?")

### Session Create/Edit (Draft)
- Fields: title (text), startsAt (datetime picker), endsAt (datetime picker)
- Validation: title required, endsAt > startsAt
- Save → persist locally only (no Teams interaction)

### Session Edit (Published)
- Same fields as draft edit
- Save action labeled "Save & Publish to Teams"
- On save: show spinner/loading overlay
- On success: return to Series Detail
- On failure: show inline error "Publish failed" with Retry button
- If user navigates away during failure: discard unsynced edits (no prompt)

### Publish Series Flow
- Confirmation dialog: "This will create Teams webinars for all sessions. Continue?"
- Show progress indicator during publish
- On success: refresh Series Detail with Published status
- On failure: show error with Retry button; display partial-failure state if rollback also fails

### Drift & Reconcile Status Display
- Drift detected: yellow warning badge on session row + banner on Session Detail with Builder vs Teams comparison values (title, start, end)
- Reconcile status: badge on session row (Synced=green, Reconciling=blue spinner, Retrying=orange, Disabled=red)
- Disabled state: show "Webhook disabled — manual intervention required" message

### Metrics Display
- Read-only panels on Series Detail and Session Detail
- Series metrics: totalRegistrations, totalAttendees, uniqueAccountsInfluenced, warm accounts list
- Session metrics: totalRegistrations, totalAttendees, uniqueRegistrantAccountDomains, uniqueAttendeeAccountDomains, warm accounts triggered

## Loading & Error States
- Page-level loading skeleton for initial data fetch
- Inline error banners with retry for failed API calls
- Optimistic updates not used in V1 (wait for server confirmation)
- Teams licensing error: if publish fails due to missing webinar license, show "Teams webinar license required" with guidance (no retry — user must resolve licensing)

## Responsive Layout
- Desktop-first; ensure readable on tablet
- Mobile not required for V1

## UX Contracts
- Published sessions require Save & Publish
- Unsynced edits discarded if leaving on failure
- Metrics read-only from API
- No pagination in V1 — all results returned

## Definition of Done
- Core flows implemented (list, detail, create, edit, publish, delete)
- Auth redirect flow functional
- Loading and error states present
- Delete confirmation dialogs implemented
- Minimal UI tests for publish, atomic edit, and delete flows

