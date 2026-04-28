---
name: spec-lifecycle-management
description: 'Manage the lifecycle of functional and technical specifications tracked as Azure DevOps work items. Use for state transitions, review readiness, staleness detection, change management, and process enforcement.'
argument-hint: 'Provide an Epic ID to check lifecycle state, or describe a lifecycle action (mark ready for review, approve, mark stale, check readiness).'
---

# Spec Lifecycle Management

Canonical owner of spec states, tags, transitions, and enforcement gates.

## When to Use
- Check current state of a spec hierarchy
- Transition drafting → review → approved
- Detect staleness when functional content changes after tech spec
- Enforce process gates before tech spec generation / implementation
- Warn when general board tools touch spec-managed items

## Required Caller Configuration

The calling agent must provide the Azure DevOps organization, project, wiki identity, and base URL when lifecycle actions need to read or write external records. This skill owns generic lifecycle semantics only; do not hardcode repository-specific Azure DevOps targets here.

## State Model (primary signal)

| State | Meaning |
|-------|---------|
| `New` | Drafting or under review (default while authoring) |
| `Active` | Approved; implementation may begin (requires approval comment) |
| `Resolved` | Implementation complete (delivery tracking) |
| `Closed` | Validation complete (delivery tracking) |
| `Removed` | Abandoned / out of scope |

No custom states.

## Query Tags (Epic only, supplemental)

| Tag | Meaning |
|-----|---------|
| `review:ready` | Ready for stakeholder review while still in `New` |
| `techspec:stale` | Existing tech spec invalidated by functional changes |

Tags never replace State.

## Lifecycle

```text
New (drafting)
  → New + review:ready        (quality gates pass)
  → Active + approval comment (stakeholder approval)
  → Resolved / Closed         (delivery)
At any post-tech-spec point:
  functional change → add techspec:stale + audit comment
```

## Transitions

| From | To | Trigger | Action |
|------|----|---------|--------|
| (none) | `New` | Epic created | Create hierarchy |
| `New` | `New` + `review:ready` | Gates pass | Add tag |
| `New` + `review:ready` | `New` | Feedback requires changes | Remove tag |
| `New` + `review:ready` | `Active` | Stakeholder approval | Remove tag, add approval comment, move hierarchy to `Active` |
| any | same + `techspec:stale` | Functional change post-tech-spec | Add tag + staleness comment |
| any + `techspec:stale` | same | Tech spec regenerated | Remove tag + regeneration comment |
| any active | `Removed` | Scope dropped | Abandonment comment |

## Comment Templates (canonical)

| Type | Path |
|------|------|
| Approval | `.github\skills\spec-lifecycle-management\templates\approval-comment.md` |
| Staleness | `.github\skills\spec-lifecycle-management\templates\staleness-comment.md` |
| Abandonment | `.github\skills\spec-lifecycle-management\templates\abandonment-comment.md` |

Load template before generating; preserve structure; reject output with unresolved placeholders.

## Staleness Detection

Triggers (post tech-spec-generation):
- Epic/Feature/User Story Description modified
- User Story Acceptance Criteria modified
- Feature or Story added/removed/reparented

Non-triggers: Priority, Iteration, Assignment, State alone, comments, other tags.

Method:
1. Record tech-spec generation date in wiki page header + Epic link comment.
2. On invocation: fetch Epic + children; inspect revisions since that date; check changed fields.
3. If substantive change detected → add `techspec:stale` + staleness comment.

## Process Enforcement

### Gates for `spec-driven-development`

| Action | Required | Block if |
|--------|----------|----------|
| Mark ready for review | Epic `New` + gates pass | Content incomplete |
| Approve spec | Epic `New` + `review:ready` | No explicit stakeholder approval |
| Generate tech spec | Epic `Active` + approval comment + no `review:ready` | Any missing |
| Modify approved spec | Epic/children `Active`/`Resolved`/`Closed` | — (warn: may need `techspec:stale`) |

### Guards for `devops-workitem-manager`

1. Reads: always allowed.
2. Safe fields: Priority, Iteration, Assignment, State, unrelated tags — allowed.
3. Functional fields (Epic/Feature Description; User Story Description or AC) → warn + confirm.
4. On confirm AND tech spec exists: add `techspec:stale` + staleness comment.
5. Creating new Features/Stories under approved hierarchy → warn (scope change).

Signals that an item is spec-managed: parent Epic has `review:ready`, `techspec:stale`, or `Active` + approval comment.

## Audit Trail
Epic comments record approval, staleness, regeneration, abandonment.

## Decision Points
- `New` alone → drafting
- `New` + `review:ready` → awaiting stakeholder review
- `Active` → approved; implementation may begin
- any + `techspec:stale` → refresh tech spec before implementation

## Completion Checks
- Hierarchy uses Agile states only
- `review:ready` only while `New`
- `techspec:stale` only when tech spec needs refresh
- User Story AC changes treated as substantive
- All approval/staleness events have Epic comments
