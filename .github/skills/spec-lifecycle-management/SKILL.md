---
name: spec-lifecycle-management
description: 'Manage the lifecycle of functional and technical specifications tracked as Azure DevOps work items. Use for state transitions, staleness detection, change management, and process enforcement.'
argument-hint: 'Provide an Epic ID to check lifecycle state, or describe a lifecycle action (approve spec, mark stale, check readiness).'
---

# Spec Lifecycle Management

## When to Use
- Checking the current lifecycle state of a spec (Epic)
- Transitioning a spec between lifecycle states
- Detecting staleness (functional spec changed after tech spec was generated)
- Enforcing process gates (e.g., blocking tech spec generation before approval)
- Warning users about spec-managed work items when editing via general board tools

## Lifecycle State Machine

```
                    ┌──────────────────────────────────┐
                    │                                  │
                    ▼                                  │
  [New Epic] → spec:draft → spec:review → spec:approved → techspec:current
                    ▲            │              │                │
                    │            │              │                │
                    │            ▼              │                ▼
                    │      (feedback)           │         (functional spec
                    │            │              │          changes detected)
                    │            ▼              │                │
                    │      spec:draft           │                ▼
                    │                           │         techspec:stale
                    │                           │                │
                    │                           │                ▼
                    │                           │         (revise & re-approve)
                    │                           │                │
                    └───────────────────────────┘────────────────┘
```

## Tag Definitions

| Tag | Meaning | Who Sets It | Prerequisites |
|-----|---------|-------------|---------------|
| `spec:draft` | Functional spec is being authored or revised | `spec-driven-development` agent | Epic exists |
| `spec:review` | Functional spec is ready for stakeholder review | `spec-driven-development` agent | All Features/Stories have descriptions and acceptance criteria |
| `spec:approved` | Stakeholders have approved the functional spec | `spec-driven-development` agent | Review complete, stakeholder sign-off |
| `techspec:current` | A technical spec exists and is up-to-date | `spec-driven-development` agent | Tech spec wiki page published and linked |
| `techspec:stale` | Technical spec is outdated due to functional spec changes | Either agent (on detection) | Functional spec modified after tech spec generation |

## Tag Rules

### Mutual Exclusivity
- `spec:draft`, `spec:review`, and `spec:approved` are mutually exclusive — only one may be present at a time
- `techspec:current` and `techspec:stale` are mutually exclusive
- `spec:approved` and `techspec:current` CAN coexist (spec is approved and tech spec is current)
- `spec:approved` and `techspec:stale` CAN coexist (spec re-approved but tech spec needs regeneration)

### Transition Rules

| From | To | Trigger | Action |
|------|-----|---------|--------|
| (none) | `spec:draft` | Epic created | Add tag |
| `spec:draft` | `spec:review` | All quality gates pass | Replace tag |
| `spec:review` | `spec:approved` | Stakeholder approval | Replace tag |
| `spec:review` | `spec:draft` | Stakeholder feedback requires changes | Replace tag |
| `spec:approved` | `spec:review` | Scope change or revision needed | Replace tag; if `techspec:current` exists, replace with `techspec:stale` |
| (with `spec:approved`) | `techspec:current` | Tech spec generated | Add tag |
| `techspec:current` | `techspec:stale` | Functional spec items modified | Replace tag; add comment to Epic |

## Staleness Detection

### What Triggers Staleness
A tech spec becomes stale when ANY of the following occur after the tech spec was generated:
1. **Epic Description or Acceptance Criteria modified**
2. **Feature added, removed, or Description/Acceptance Criteria modified**
3. **User Story added, removed, or Description/Acceptance Criteria modified**
4. **Epic scope tags changed** (In Scope / Out of Scope)

### What Does NOT Trigger Staleness
- Changing Priority, Iteration, or Assignment on any work item
- Adding comments to work items
- Changing State (New → Active → Resolved)
- Modifying Tags that are not spec lifecycle tags

### Detection Method
1. Record the Epic's revision number when the tech spec is generated (stored in wiki page header)
2. On each agent invocation involving this Epic:
   - Fetch the Epic's current revision number via `list_work_item_revisions`
   - Compare against the recorded revision
   - If revisions have occurred, check if any modified Description, Acceptance Criteria, or child item structure
   - If substantive changes detected, transition to `techspec:stale`

### Staleness Response
When staleness is detected:
1. Replace `techspec:current` with `techspec:stale` on the Epic
2. Add a comment to the Epic:
   ```
   ⚠️ **Tech spec is now stale.** Functional spec was modified after tech spec v[VERSION] was generated.
   
   Changes detected:
   - [Description of what changed]
   
   A new technical specification must be generated before implementation continues.
   ```
3. Inform the user that the tech spec needs regeneration

## Process Enforcement

### Gates for `spec-driven-development` Agent
| Action | Required State | Block Behavior |
|--------|---------------|----------------|
| Generate tech spec | `spec:approved` | Refuse; instruct user to complete approval |
| Modify functional spec after approval | `spec:approved` | Warn that this will trigger staleness; ask for confirmation |
| Start new spec authoring | No existing `spec:*` tags, or `spec:draft` | If `spec:review` or `spec:approved`, warn about overwriting |

### Guards for `devops-workitem-manager` Agent
When the `devops-workitem-manager` agent encounters work items with spec lifecycle tags, it should:

1. **Read operations**: No restrictions — always allow reading spec-managed items
2. **Field updates that trigger staleness** (Description, Acceptance Criteria on Epic/Feature/Story):
   - Display a warning: "⚠️ This work item is managed by the spec-driven development process (tag: [tag]). Modifying its description or acceptance criteria may trigger tech spec staleness."
   - Ask the user to confirm before proceeding
   - If confirmed and `techspec:current` is present, transition to `techspec:stale`
3. **Safe field updates** (Priority, Iteration, Assignment, State, non-spec Tags):
   - Allow without warning
4. **Creating child items** under a spec-managed parent:
   - Warn that adding items may affect the functional spec scope
   - If `techspec:current` is present, transition to `techspec:stale` after creation

## Audit Trail

All lifecycle transitions should be documented with work item comments:
- Tag changes: Record old tag → new tag and reason
- Staleness detection: Record what changed and when
- Tech spec generation: Record version number and wiki page link
- Re-approval: Record that stakeholder re-approved after changes

## Decision Points

- If an Epic has no spec tags, it is NOT managed by this process — do not apply lifecycle rules
- If an Epic has both `spec:draft` and `techspec:current` (inconsistent state), flag as error and ask user to resolve
- If a user wants to abandon a spec, remove all `spec:*` and `techspec:*` tags and add a comment explaining why

## Completion Checks

- Epic has exactly one `spec:` tag (draft, review, or approved)
- If `techspec:*` tag exists, it is consistent with the spec state
- All lifecycle transitions have corresponding comments on the Epic
- No inconsistent tag combinations exist
