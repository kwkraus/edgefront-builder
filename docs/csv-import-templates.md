# CSV Import Templates and Rules

This document defines the supported CSV schemas, expected formats, and behavioral rules for manual CSV imports in EdgeFront Builder.

## Overview

CSV import allows session owners to manually add registration and attendance data for a specific session. This is useful when Teams-sourced data is missing, delayed, incomplete, or when data was collected outside the Teams webinar workflow.

**Key principle:** CSV import augments local Builder data. It does not replace or delete existing Teams-synced data.

## Registration CSV

### Schema

| Column | Required | Format | Description |
|--------|----------|--------|-------------|
| `Email` | Yes | Valid email address | Registrant email (must contain `@`) |
| `RegisteredAt` | No | ISO 8601 or common datetime | Registration timestamp; defaults to current UTC time if omitted |

### Example

```csv
Email,RegisteredAt
alice@contoso.com,2026-03-15T10:00:00Z
bob@fabrikam.com,2026-03-15T11:30:00Z
carol@example.org,
```

### Minimal Example (Email Only)

```csv
Email
alice@contoso.com
bob@fabrikam.com
carol@example.org
```

## Attendance CSV

### Schema

| Column | Required | Format | Description |
|--------|----------|--------|-------------|
| `Email` | Yes | Valid email address | Attendee email (must contain `@`) |
| `Attended` | No | `true`, `false`, `1`, `0`, `yes`, `no` | Whether the person attended; defaults to `true` if omitted |
| `DurationSeconds` | No | Integer | Duration of attendance in seconds |
| `FirstJoinAt` | No | ISO 8601 or common datetime | Timestamp of first join |
| `LastLeaveAt` | No | ISO 8601 or common datetime | Timestamp of last leave |

### Example

```csv
Email,Attended,DurationSeconds,FirstJoinAt,LastLeaveAt
alice@contoso.com,true,3600,2026-03-20T14:00:00Z,2026-03-20T15:00:00Z
bob@fabrikam.com,true,1800,2026-03-20T14:15:00Z,2026-03-20T14:45:00Z
carol@example.org,false,,,
```

### Minimal Example (Email Only)

```csv
Email
alice@contoso.com
bob@fabrikam.com
```

When only `Email` is provided, `Attended` defaults to `true` and all other fields are left empty.

## Accepted Datetime Formats

The following datetime formats are accepted for timestamp fields:

- ISO 8601 roundtrip: `2026-03-15T10:00:00.0000000Z`
- ISO 8601 with Z: `2026-03-15T10:00:00Z`
- ISO 8601 without offset: `2026-03-15T10:00:00`
- Date and time with space: `2026-03-15 10:00:00`
- Date only: `2026-03-15`
- US format with 12h: `3/15/2026 10:00:00 AM`
- US format with 24h: `3/15/2026 10:00:00`
- US format date only: `03/15/2026`

All timestamps are stored in UTC. Timestamps without timezone information are assumed to be UTC.

## Column Header Rules

- Column headers are **case-insensitive** (`email`, `Email`, `EMAIL` are all accepted).
- Column order does not matter; columns are matched by name.
- Extra columns beyond the documented schema are ignored.
- The header row is always the first line of the CSV file.

## Duplicate and Repeated Import Behavior

- **Idempotent imports:** Re-importing the same CSV does not create duplicate rows or inflate counts.
- **Deduplication key:** Each record is uniquely identified by the combination of (OwnerUserId, SessionId, Email).
- **Update on match:** If a row already exists for the same email in the same session, the existing record is updated with the new values. Updated rows are reported as "skipped" in the import result.
- **New rows:** Rows with emails not previously seen for the session are inserted as new records.
- **Partial success:** Valid rows are imported even when some rows fail validation. Invalid rows are reported with row numbers and reasons.

## Email and Domain Normalization

All imported email addresses are processed through the same normalization pipeline used by the Teams sync flow:

1. **Email normalization:** Leading/trailing whitespace is trimmed, and the entire address is lowercased.
   - Example: `" Alice@Contoso.COM "` → `alice@contoso.com`
2. **Domain extraction:** The email domain is extracted from the normalized address.
   - Example: `alice@contoso.com` → `contoso.com`
3. **Registrable domain:** Sub-domains are stripped to the registrable domain (last two labels).
   - Example: `sales.contoso.com` → `contoso.com`

## Internal Domain Handling

- Rows with internal-domain email addresses (as configured in the system's internal domain list) are stored in the database.
- Internal-domain rows are **excluded** from the following external-only aggregate metrics:
  - Unique registrant account domains
  - Unique attendee account domains
  - Influenced accounts
  - Warm account calculations (W1 and W2)
- Internal-domain rows **are included** in total registration and total attendee counts.

## Metrics Recompute

After a successful import (even partial success), the system automatically recomputes:

1. **Session metrics:** Total registrations, total attendees, unique registrant domains, unique attendee domains, and W1 warm accounts.
2. **Series metrics:** Total registrations, total attendees, unique registrant domains, influenced accounts, and warm accounts (W1 + W2).

Metrics are recomputed from all stored normalized data, not incrementally from the import.

## Warm Account Rules

Warm account evaluation follows the same rules as the Teams sync pipeline:

- **W1:** Two or more distinct email addresses from the same external domain attending the same session.
- **W2:** The same email address attending two or more distinct sessions in the same series.
- **Precedence:** When a domain qualifies for both W1 and W2, the W2 rule takes precedence.

These rules are evaluated against all stored attendance data (both Teams-synced and CSV-imported).

## Authorization

- Only the authenticated session owner can import data for their sessions.
- Attempting to import data for a session owned by another user returns an error.
- Standard JWT authentication is required for all import requests.

## V1 Scope Limitations

The following are **not supported** in V1:

- Series-level bulk import (import is per-session only).
- Presenter or coordinator assignment from CSV.
- Overwriting or deleting Teams-synced data via CSV import.
- Any import type other than registrations and attendance.

## API Endpoints

| Endpoint | Method | Content-Type | Description |
|----------|--------|-------------|-------------|
| `/api/v1/sessions/{id}/imports/registrations` | POST | `multipart/form-data` | Import registration CSV |
| `/api/v1/sessions/{id}/imports/attendance` | POST | `multipart/form-data` | Import attendance CSV |

Both endpoints accept a `file` field in the multipart form data containing the CSV file.

### Response Format

```json
{
  "totalRows": 10,
  "importedCount": 7,
  "skippedCount": 2,
  "invalidCount": 1,
  "errors": [
    { "row": 3, "reason": "Invalid email format." },
    { "row": 8, "reason": "Email is required." }
  ]
}
```

| Field | Description |
|-------|-------------|
| `totalRows` | Number of non-empty data rows processed (excludes header and blank lines) |
| `importedCount` | Number of new rows successfully inserted |
| `skippedCount` | Number of rows that matched existing records (updated in place) |
| `invalidCount` | Number of rows that failed validation |
| `errors` | Row-level details for each invalid row, with 1-based row numbers |

## Traceability

- Azure DevOps Feature: 349
- Azure DevOps User Stories: 350, 351, 352, 353, 354, 355
