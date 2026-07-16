# Feature Specification: Restore People Lookup

**Feature Branch**: `master`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "Enable the presenter and coordinator lookup controls to pull users from the Microsoft graph for the configured tenant. This was previously working, but was accidentally removed or disabled from the previous refactoring. Functionality should be type ahead and surface alternatives as the user is typing. Multiple presenters and multiple coordinators can be selected and managed by these controls."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find and Select Tenant Users (Priority: P1)

As an authorized session editor, I can type a person's name or email address into either the Presenter or Coordinator control and select a matching person from the configured tenant directory, so that I do not need to enter participant details manually.

**Why this priority**: Restoring directory-backed lookup is the core regression fix and is required before either role can be assigned reliably.

**Independent Test**: Open an editable session, enter at least two characters from a known tenant user's name or email in each role control, and select that user from the suggestions. This independently delivers accurate assignment of one presenter or coordinator.

**Acceptance Scenarios**:

1. **Given** an authorized editor is viewing an editable session, **When** the editor types at least two characters into the Presenter control, **Then** matching users from the configured tenant are shown without requiring a separate submit action.
2. **Given** matching users are displayed, **When** the editor selects a user, **Then** that user appears in the session's selected Presenters and the search entry is cleared for another lookup.
3. **Given** an authorized editor is viewing an editable session, **When** the editor searches and selects a user in the Coordinator control, **Then** that user appears in the selected Coordinators independently of the Presenter selections.
4. **Given** multiple people have similar names, **When** suggestions are displayed, **Then** each suggestion includes enough identity information, including display name and email address when available, for the editor to distinguish the correct person.

---

### User Story 2 - Manage Multiple Role Assignments (Priority: P2)

As an authorized session editor, I can add and remove multiple presenters and multiple coordinators before saving, so that the session accurately reflects everyone responsible for delivery and coordination.

**Why this priority**: Sessions commonly involve more than one person in each role, and editors need to correct selections without restarting their work.

**Independent Test**: Add at least three people to each role, remove one person from each role, save, and reopen the session. The remaining assignments must match the editor's final selections.

**Acceptance Scenarios**:

1. **Given** one presenter is already selected, **When** the editor searches for and selects another presenter, **Then** both people remain selected.
2. **Given** one coordinator is already selected, **When** the editor searches for and selects another coordinator, **Then** both people remain selected.
3. **Given** a person is selected for a role, **When** the editor removes that person, **Then** the person is removed from that role without changing other role assignments.
4. **Given** the editor has changed presenter and coordinator selections, **When** the editor saves the session successfully and later reopens it, **Then** the final selections for both roles are preserved.

---

### User Story 3 - Recover from Lookup Problems (Priority: P3)

As an authorized session editor, I receive clear feedback when lookup cannot return a person, so that I can adjust the search or retry without losing existing selections.

**Why this priority**: Directory lookup depends on connectivity and permissions; predictable feedback prevents silent failure and protects in-progress edits.

**Independent Test**: Exercise no-match and unavailable-directory conditions while existing people are selected, then verify that feedback is shown and all existing selections remain unchanged.

**Acceptance Scenarios**:

1. **Given** the entered text matches no eligible tenant users, **When** lookup completes, **Then** the editor sees a no-results message and existing selections remain unchanged.
2. **Given** the tenant directory cannot be reached or searched, **When** lookup fails, **Then** the editor sees a clear retryable error and existing selections remain unchanged.
3. **Given** suggestions are visible, **When** the editor uses the keyboard to move through, select, or dismiss them, **Then** the same lookup and selection outcomes are available without pointer input.

### Edge Cases

- Entering fewer than two characters does not initiate lookup or display stale suggestions.
- Rapidly changing the query shows suggestions for the current text, not an older lookup that finishes later.
- A person already selected for a role is not offered again for that same role.
- Presenter and Coordinator selections are independent; the same eligible person may be assigned to both roles when responsibilities overlap.
- A tenant user with no email address can still be identified and selected using the available directory identity information.
- Disabled, deleted, inaccessible, or out-of-tenant users are not offered as new selections.
- Clearing or dismissing a query does not remove previously selected people.
- Existing saved assignments remain visible if a previously selected person can no longer be found in a new directory lookup.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide separate tenant-directory lookup controls for Presenters and Coordinators when an authorized user edits a session.
- **FR-002**: Each control MUST begin searching after the editor enters at least two characters of a person's display name or email address.
- **FR-003**: Each control MUST update matching suggestions as the editor types, without requiring a separate search submission.
- **FR-004**: Lookup results MUST be limited to eligible users in the application's configured tenant.
- **FR-005**: Each suggestion MUST show the person's display name and email address when available, or other available identity information when an email address is absent.
- **FR-006**: The editor MUST be able to select a suggestion and see that person added to the applicable role.
- **FR-007**: The editor MUST be able to add and retain multiple people in each role.
- **FR-008**: A person already selected for a role MUST NOT be selectable a second time for that same role.
- **FR-009**: The editor MUST be able to remove any selected person from either role before saving.
- **FR-010**: Presenter and Coordinator selections MUST be managed independently, including allowing the same eligible person to hold both roles.
- **FR-011**: A successful session save MUST preserve the complete ordered set of Presenter and Coordinator selections so that they are shown again when the session is reopened.
- **FR-012**: No-result and lookup-failure states MUST provide distinct, understandable feedback without clearing the query or changing existing selections.
- **FR-013**: Suggestions MUST correspond to the editor's current query when multiple lookups complete out of order.
- **FR-014**: The lookup, suggestion review, selection, dismissal, and selected-person removal interactions MUST be operable by keyboard and expose each person's role and identity to assistive technology.
- **FR-015**: Editors without permission to modify a session MUST NOT be able to change its Presenter or Coordinator assignments.

### Key Entities

- **Session**: The editable event being configured; holds separate collections of Presenter and Coordinator assignments.
- **Tenant User**: A person available from the configured organization's directory, identified by a stable tenant identity and described by a display name plus email address or other available identity information.
- **Role Assignment**: The association between one Session, one Tenant User, and either the Presenter or Coordinator role. A tenant user appears at most once per role but may hold both roles.
- **Lookup Query**: The editor's current type-ahead text and its resulting eligible tenant-user suggestions; it is transient and does not alter saved assignments.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In at least 95% of normal lookups, editors see current suggestions or a definitive no-results state within 2 seconds after pausing their typing.
- **SC-002**: At least 90% of representative users can find and assign a known tenant user to each role on their first attempt without assistance.
- **SC-003**: Editors can add, review, and remove 10 Presenters and 10 Coordinators in one session without losing or duplicating a selection.
- **SC-004**: In validation tests, 100% of successfully saved role assignments match the selections shown after the session is reopened.
- **SC-005**: In validation tests, no user outside the configured tenant is offered as a new Presenter or Coordinator.
- **SC-006**: All primary lookup and selection tasks can be completed using only a keyboard, with no loss of functionality compared with pointer use.
- **SC-007**: No-result and lookup-failure tests preserve 100% of the editor's pre-existing Presenter and Coordinator selections.

## Assumptions

- The feature restores an existing session-editing capability rather than adding role assignment to other product areas.
- Users accessing the controls are already authenticated, and existing session-edit authorization rules remain authoritative.
- The configured tenant directory and delegated user-search permissions already exist; changing tenant configuration, consent, or directory permissions is outside this feature's scope.
- Type-ahead begins at two characters, matching the established product behavior before the regression.
- Display name and email address are the preferred identity cues; an available tenant identity may substitute when email is absent.
- Presenter and Coordinator lists are independent, and there is no product-level maximum below the volume stated in the success criteria.
- Changes become durable only when the editor saves the session; cancellation retains the previously saved assignments.
