---
name: spec-lifecycle-management
description: 'Manage the lifecycle of functional and technical specifications tracked as Azure DevOps work items. Use for state transitions, review readiness, staleness detection, change management, and process enforcement.'
argument-hint: 'Provide an Epic ID to check lifecycle state, or describe a lifecycle action (mark ready for review, approve, mark stale, check readiness).'
---

# Spec Lifecycle Management

## When to Use
- Checking the current lifecycle state of a spec hierarchy
- Transitioning a spec from drafting to review readiness to approved
- Detecting staleness when functional spec content changes after tech spec generation
- Enforcing process gates before technical spec generation or implementation
- Warning users about spec-managed work items when editing through general board tools

## Workflow Signals

### Primary Signal: Azure DevOps State

| State | Meaning | Notes |
|-------|---------|-------|
| `New` | Drafting or under stakeholder review | Default during authoring |
| `Active` | Approved and ready for implementation | Requires approval comment |
| `Resolved` | Implementation complete | Delivery tracking |
| `Closed` | Validation complete | Delivery tracking |
| `Removed` | No longer needed | Abandoned or deleted scope |

### Supplemental Query Tags (Epic only)

| Tag | Meaning | Purpose |
|-----|---------|---------|
| `review:ready` | The spec is ready for stakeholder review while still in `New` | Supports review meeting queries and dashboards |
| `techspec:stale` | The existing technical spec is no longer valid | Supports regeneration workflows and dashboards |

Tags are supplemental only. They do not replace State.

## Lifecycle Model

```text
New (drafting)
  |
  +-- quality gates pass --> New + review:ready
                                |
                                +-- stakeholder approval -->
                                    Active + approval comment
                                          |
                                          +-- implementation complete --> Resolved
                                          |
                                          +-- validated correct --> Closed

At any point after a tech spec exists:
  functional spec content changes --> add techspec:stale + audit comment
```

## Approval and Audit Comments

### Approval Comment Template

```markdown
✅ **Functional spec approved for implementation**

- Approved by: [name]
- Approved on: [YYYY-MM-DD]
- Notes: [summary of decision or constraints]
```

### Staleness Comment Template

```markdown
⚠️ **Technical spec is stale**

- Detected on: [YYYY-MM-DD]
- Changed items:
  - [Epic/Feature/User Story and what changed]
- Action: Regenerate the technical specification before continuing implementation that depends on this change.
```

## Transition Rules

| From | To | Trigger | Action |
|------|----|---------|--------|
| (none) | `New` | Epic created | Create hierarchy in draft state |
| `New` | `New` + `review:ready` | All quality gates pass | Add `review:ready` to Epic |
| `New` + `review:ready` | `New` | Stakeholder feedback requires changes | Remove `review:ready`, add comment if helpful |
| `New` + `review:ready` | `Active` | Stakeholder approval | Remove `review:ready`, add approval comment, move approved hierarchy to `Active` |
| `Active` / `Resolved` / `Closed` | same state + `techspec:stale` | Functional spec content changed after tech spec generation | Add `techspec:stale`, add staleness comment |
| any state + `techspec:stale` | same state | Tech spec regenerated | Remove `techspec:stale`, add regeneration comment |
| any active state | `Removed` | Scope no longer needed | Move to `Removed`, add abandonment comment |

## Staleness Detection

### What Triggers Staleness
A technical spec becomes stale when any of the following change after the tech spec was generated:
1. **Epic Description** modified
2. **Feature Description** modified
3. **User Story Description** modified
4. **User Story Acceptance Criteria field** modified
5. **Feature or User Story hierarchy** changes (items added, removed, or reparented)

### What Does Not Trigger Staleness
- Changing Priority, Iteration, or Assignment
- Adding comments
- Changing State by itself
- Adding or removing tags other than `review:ready` and `techspec:stale`

### Detection Method
Do not rely on the Epic revision alone.

1. When the tech spec is generated, record the generation date in:
   - the wiki page header
   - the Epic comment linking to the tech spec
2. On each invocation involving a generated tech spec:
   - fetch the Epic, child Features, and child User Stories
   - inspect revisions for each item since the tech spec generation date
   - check whether the changed fields include:
     - Epic Description
     - Feature Description
     - User Story Description
     - User Story Acceptance Criteria
     - child hierarchy membership
3. If any substantive changes are detected, add `techspec:stale` and a staleness comment.

## Process Enforcement

### Gates for `spec-driven-development`

| Action | Required condition | Block behavior |
|--------|--------------------|----------------|
| Mark ready for review | Epic in `New`; all quality gates pass | Refuse until missing content is filled in |
| Approve spec | Epic in `New` with `review:ready` | Refuse until stakeholder approval is explicit |
| Generate tech spec | Epic in `Active`; approval comment exists; no `review:ready` tag | Refuse and explain what is missing |
| Modify approved functional spec | Epic/children in `Active`, `Resolved`, or `Closed` | Warn that changes may require `techspec:stale` |

### Guards for `devops-workitem-manager`

When the `devops-workitem-manager` agent encounters spec-managed work items, it should:

1. **Read operations**: Always allowed.
2. **Safe field updates**: Priority, Iteration, Assignment, State, and unrelated tags are allowed.
3. **Functional field updates** require warning and confirmation:
   - **Epic**: Description
   - **Feature**: Description
   - **User Story**: Description or Acceptance Criteria field
4. If confirmed and a tech spec already exists for the parent Epic:
   - add `techspec:stale`
   - add a staleness comment
5. Creating child Features or User Stories under an approved hierarchy also requires a warning because it changes scope.

Treat these as strong signals that an item is part of the spec workflow:
- parent Epic tagged `review:ready`
- parent Epic tagged `techspec:stale`
- parent Epic in `Active` with an approval comment

## Audit Trail

Record lifecycle events with Epic comments:
- approval comments when moving to `Active`
- staleness comments when functional changes invalidate the tech spec
- regeneration comments when a stale tech spec is replaced
- abandonment comments when moving to `Removed`

## Decision Points

- If an Epic is `New` and not tagged `review:ready`, it is still drafting.
- If an Epic is `New` and tagged `review:ready`, it is waiting for stakeholder review.
- If an Epic is `Active`, approval has been granted and implementation may begin.
- If an Epic is in any state with `techspec:stale`, the technical spec must be refreshed before relying on it for implementation.

## Completion Checks

- The hierarchy uses Agile states, not custom lifecycle states
- `review:ready` appears only while the Epic is in `New`
- `techspec:stale` is present only when a generated tech spec needs refresh
- User Story Acceptance Criteria changes are treated as substantive functional changes
- All approval and staleness events have corresponding Epic comments
