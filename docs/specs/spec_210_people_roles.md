# SPEC-210 — People Roles & Presenter/Coordinator Management (Build Ready)

## Authority
Defines session presenter and coordinator roles, Graph directory search, Builder → Teams role sync, and session detail page redesign.

**Depends on:** SPEC-000, SPEC-010, SPEC-110, SPEC-120, SPEC-200

## Scope
- Add presenter and coordinator roles to sessions
- Search Entra ID directory for people (via Microsoft Graph)
- Sync roles from Builder → Teams on publish and save-when-published
- Redesign session detail page: inline title edit, people pickers, calendar+time pickers

**Out of scope:** Teams → Builder sync (no inbound sync of presenter/coordinator changes made directly in Teams)

---

## Domain Model

### SessionPresenter
A person assigned as a presenter to a session. Maps 1:1 to a Teams webinar presenter.

- **Identity**: Entra ID user (Azure AD Object ID)
- **Fields**: SessionPresenterId (UUID), SessionId (FK), EntraUserId (string), DisplayName (string), Email (string), CreatedAt (datetime2 UTC)
- **Constraint**: One presenter per Entra user per session — unique (SessionId, EntraUserId)
- **Lifecycle**: Managed in Builder, synced to Teams on publish/save

### SessionCoordinator
A person assigned as a coordinator to a session. Maps 1:1 to a Teams webinar co-organizer.

- **Identity**: Entra ID user (Azure AD Object ID)
- **Fields**: SessionCoordinatorId (UUID), SessionId (FK), EntraUserId (string), DisplayName (string), Email (string), CreatedAt (datetime2 UTC)
- **Constraint**: One coordinator per Entra user per session — unique (SessionId, EntraUserId)
- **Lifecycle**: Managed in Builder, synced to Teams on publish/save

### Graph Role Mapping
| Builder Role | Teams Webinar Role | Graph API |
|---|---|---|
| Presenter | Presenter | `POST /solutions/virtualEvents/webinars/{id}/presenters` |
| Coordinator | Co-Organizer | `PATCH /solutions/virtualEvents/webinars/{id}` → `coOrganizers` collection |

---

## App Registration — Additional Permission

| Permission | Type | Purpose |
|---|---|---|
| `User.ReadBasic.All` | Delegated | Search Entra directory users by displayName/email for people picker |

**Existing permissions** (per SPEC-200): `VirtualEvent.ReadWrite`, `OnlineMeetingArtifact.Read.All`, `openid`, `profile`, `email`, `offline_access`

**Setup**: Run `tools/update-app-registration.ps1` — uses `az login --tenant {tenant} --allow-no-subscriptions` + `az ad app permission add` + `az ad app permission admin-consent`

---

## Data Schema

### SessionPresenter Table
- sessionPresenterId (UUID, PK)
- sessionId (UUID, FK → Session, cascade delete)
- entraUserId (string, Entra AD Object ID)
- displayName (string)
- email (string)
- createdAt (datetime2, UTC)

**Constraints:**
- Unique (sessionId, entraUserId)
- FK cascade delete: Session → SessionPresenter

**Indexes:**
- SessionPresenter(sessionId)

### SessionCoordinator Table
- sessionCoordinatorId (UUID, PK)
- sessionId (UUID, FK → Session, cascade delete)
- entraUserId (string, Entra AD Object ID)
- displayName (string)
- email (string)
- createdAt (datetime2, UTC)

**Constraints:**
- Unique (sessionId, entraUserId)
- FK cascade delete: Session → SessionCoordinator

**Indexes:**
- SessionCoordinator(sessionId)

### Conventions
- UUID PKs (ValueGeneratedNever)
- UTC datetime2 with UTC ValueConverter
- EF Core migrations only

---

## API Endpoints

### People Search
- `GET /api/v1/people/search?q={query}` → `PersonSearchResult[]`
  - Proxies to Microsoft Graph user search via OBO token
  - Returns up to 10 results
  - Minimum query length: 2 characters
  - Graph query: `GET /users?$filter=startswith(displayName,'{q}')&$select=id,displayName,mail,userPrincipalName&$top=10`

### Session Presenters
- `GET /api/v1/sessions/{id}/presenters` → `SessionPresenterDto[]`
- `PUT /api/v1/sessions/{id}/presenters` ← `SetPresentersRequest` → `SessionPresenterDto[]`
  - Replaces all presenters for the session
  - If session is Published: syncs to Teams after DB update
  - Idempotent — safe to call with same list

### Session Coordinators
- `GET /api/v1/sessions/{id}/coordinators` → `SessionCoordinatorDto[]`
- `PUT /api/v1/sessions/{id}/coordinators` ← `SetCoordinatorsRequest` → `SessionCoordinatorDto[]`
  - Replaces all coordinators for the session
  - If session is Published: syncs to Teams after DB update
  - Idempotent — safe to call with same list

### Updated Session Response
`GET /sessions/{id}` and `PUT /sessions/{id}` responses now include:
```json
{
  "presenters": [{ "sessionPresenterId": "uuid", "entraUserId": "string", "displayName": "string", "email": "string" }],
  "coordinators": [{ "sessionCoordinatorId": "uuid", "entraUserId": "string", "displayName": "string", "email": "string" }]
}
```

## Request/Response DTOs

### PersonSearchResult
```json
{
  "entraUserId": "string",
  "displayName": "string",
  "email": "string"
}
```

### SetPresentersRequest
```json
{
  "people": [
    { "entraUserId": "string", "displayName": "string", "email": "string" }
  ]
}
```

### SetCoordinatorsRequest
```json
{
  "people": [
    { "entraUserId": "string", "displayName": "string", "email": "string" }
  ]
}
```

### SessionPresenterDto
```json
{
  "sessionPresenterId": "uuid",
  "entraUserId": "string",
  "displayName": "string",
  "email": "string"
}
```

### SessionCoordinatorDto
```json
{
  "sessionCoordinatorId": "uuid",
  "entraUserId": "string",
  "displayName": "string",
  "email": "string"
}
```

---

## Graph Integration

### User Search
- Endpoint: `GET /users?$filter=startswith(displayName,'{query}')&$select=id,displayName,mail,userPrincipalName&$top=10`
- Uses OBO token with `User.ReadBasic.All` delegated permission
- Returns basic profile: `id` (EntraUserId), `displayName`, `mail` (fallback to `userPrincipalName`)
- Debounced from frontend (300ms) to limit API calls

### Presenter Sync (Builder → Teams)
- **Diff-based**: List current Teams presenters → compare with Builder list → add new / remove deleted
- **Add**: `POST /solutions/virtualEvents/webinars/{id}/presenters` with `communicationsUserIdentity` body
- **Remove**: `DELETE /solutions/virtualEvents/webinars/{id}/presenters/{presenterId}`
- **Identity payload**:
```json
{
  "identity": {
    "@odata.type": "#microsoft.graph.communicationsUserIdentity",
    "id": "{entraUserId}",
    "tenantId": "{tenantId}"
  }
}
```
- `tenantId` sourced from AzureAd configuration (`AzureAd:TenantId`)

### Co-Organizer Sync (Builder → Teams)
- **Full replacement**: `PATCH /solutions/virtualEvents/webinars/{id}` with complete `coOrganizers` collection
- **Payload**:
```json
{
  "coOrganizers": [
    { "user": { "id": "{entraUserId}" } }
  ]
}
```
- Empty array clears all co-organizers
- Must always send the complete list (not additive)

### Sync Timing
| Event | Presenter Sync | Co-Organizer Sync |
|---|---|---|
| Session Publish | After webinar creation | After webinar creation |
| Save (Published session) | After metadata update | After metadata update |
| PUT presenters (Published) | Immediately after DB write | — |
| PUT coordinators (Published) | — | Immediately after DB write |
| Delete session | Handled by webinar delete | Handled by webinar delete |

### Error Handling
- If presenter sync fails: log error, return success for DB write, surface sync warning to user
- If co-organizer sync fails: same behavior — local changes saved, sync failure surfaced
- Teams licensing errors: surface "webinar license required" message, do not retry
- User not found in Graph: return validation error, do not persist

---

## Frontend UX — Session Detail Page Redesign

### Layout Structure
```
┌─────────────────────────────────────────────────┐
│ ← Back to Series                                │
│                                                 │
│ Session Title ✏️            [Status Badge]       │
│ (click pencil → modal dialog to edit title)     │
│                                                 │
│ ┌─── Schedule ──────────────────────────────┐   │
│ │ Start: [📅 Calendar + Time Picker]        │   │
│ │ End:   [📅 Calendar + Time Picker]        │   │
│ └───────────────────────────────────────────┘   │
│                                                 │
│ ┌─── Presenters ────────────────────────────┐   │
│ │ [🔍 Search people...                    ] │   │
│ │ [Jane Doe ×] [John Smith ×]               │   │
│ └───────────────────────────────────────────┘   │
│                                                 │
│ ┌─── Coordinators ──────────────────────────┐   │
│ │ [🔍 Search people...                    ] │   │
│ │ [Admin User ×]                            │   │
│ └───────────────────────────────────────────┘   │
│                                                 │
│ ┌─── Teams Integration ─────────────────────┐   │
│ │ Sync status · Join link · Last synced     │   │
│ └───────────────────────────────────────────┘   │
│                                                 │
│ [Save] [Save & Publish to Teams] [Delete]       │
│                                                 │
│ ┌─── Metrics (published only) ──────────────┐   │
│ │ Registrations · Attendees · Domains · ... │   │
│ └───────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
```

### Title Editing
- Session title displayed as heading (not form field)
- Pencil icon (✏️) to right of title — click opens modal dialog
- Modal: text input with current title, Save/Cancel buttons
- Same pattern as Series title edit in series-detail-view
- Validation: title required, non-empty

### DateTime Pickers
- Replace native `<input type="datetime-local">` with calendar + time component
- Calendar popover with date grid (shadcn/ui Popover + Calendar)
- Time input below or beside calendar (hours:minutes selector)
- Trigger button shows formatted date/time (e.g., "Mar 5, 2026 at 2:00 PM")
- Validation: end must be after start

### People Picker
- Typeahead search input with search icon
- Debounced search (300ms) — calls `/api/v1/people/search?q={query}`
- Dropdown shows results: displayName + email
- Click result → adds to selected list as badge chip
- Badge chip: displayName with × remove button
- Keyboard accessible: arrow keys navigate results, Enter selects, Escape closes
- Uses shadcn/ui Command component (combobox pattern)
- Minimum 2 characters before search fires

### Card Sections
- Group related fields into visual card sections with headers
- Schedule section: start/end pickers
- Presenters section: people picker + selected badges
- Coordinators section: people picker + selected badges
- Teams Integration section: sync status, join link, last synced (read-only)
- Metrics section: read-only panels (visible only for published sessions)

### Component Library
- shadcn/ui (Radix-based, Tailwind CSS styled)
- Components to install: `popover`, `calendar`, `command`, `dialog`, `button`, `input`, `label`, `badge`, `separator`, `tooltip`
- Date library: `react-day-picker` + `date-fns`

### Loading & Error States
- People picker: loading spinner in dropdown during search
- DateTime picker: disabled state during save
- Presenter/coordinator save: inline success/error feedback
- Teams sync failure: inline warning banner (local changes saved, sync failed)

---

## Definition of Done

### Backend
- SessionPresenter and SessionCoordinator entities with EF Core migration
- Unique constraint tests (duplicate entraUserId per session rejected)
- People search endpoint with Graph user search via OBO
- Presenter/coordinator CRUD endpoints with validation
- Graph presenter sync (add/remove) tested with mocked Graph client
- Graph co-organizer sync (PATCH replacement) tested with mocked Graph client
- Publish flow includes presenter + co-organizer sync
- Save-when-published flow includes role sync
- Error handling: sync failures logged, local changes preserved

### Frontend
- PeoplePicker component: search, select, remove, keyboard navigation
- DateTimePicker component: calendar popover, time input, formatted display
- Session detail page: inline title edit, card sections, people pickers, datetime pickers
- API client functions for people search, presenter/coordinator CRUD
- Loading and error states for all new interactions
- `npm run build` and `npm run lint` pass

### Infrastructure
- `tools/update-app-registration.ps1` script working with `--allow-no-subscriptions`
- `docs/setup-entra-permissions.md` documenting permission setup
- `User.ReadBasic.All` permission added and consented in Entra app registration
