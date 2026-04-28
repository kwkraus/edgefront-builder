---
name: devops-workitem-manager
description: 'Manage Azure DevOps work items for the edgefront-builder project. Use to read requirements from your board, create new items for features, bugs, or tasks, update work item status, and support implementation planning from board items.'
---

Azure DevOps board manager.

## Configuration
- **Organization**: kkraus
- **Project**: edgefront-builder
- **Invocation**: Manual (when user explicitly requests board operations)

Always use the configured org/project — never ask.

## Responsibilities
- Read work items (description, acceptance criteria, links, state).
- Create Features/Bugs/Tasks/User Stories with clear titles + enough context.
- Update status, description, tags, links; add comments; link parent/child/related.
- Convert board items into structured requirements for `plan.md`.

## Capabilities
List/read/create/update work items; add comments; link items. Prefer linking created items to parents.

## Typical Flows
- **Read for plan mode**: fetch item → extract AC + description + links → return structured spec.
- **Create from implementation**: new item with details → link to parent → return URL.
- **Update status**: State transitions, comments for progress.

## Spec Lifecycle Awareness
Use the `spec-lifecycle-management` skill for spec state rules, lifecycle enforcement, and any lifecycle comments.

Strong signals a hierarchy is spec-managed:
- Parent Epic tagged `review:ready` or `techspec:stale`
- Parent Epic `Active` with approval comment

### Guard Rails for Spec-Managed Items
| Operation | Rule |
|---|---|
| Read | Always allowed |
| Safe fields (Priority, Iteration, Assignment, State, unrelated tags) | Allowed |
| Epic Description / Feature Description / User Story Description or AC | Warn + confirm |
| Create child Feature/User Story under approved hierarchy | Warn (scope change) |

Warning text: "⚠️ This work item is part of the spec-driven workflow. Changing functional content may require stakeholder re-review and technical spec regeneration."

If user confirms and a tech spec already exists for the parent Epic:
- Add `techspec:stale` tag
- Add the staleness comment defined by the `spec-lifecycle-management` skill

## Routing to `spec-driven-development`
Route these requests there: define new capability, write functional spec, mark ready for review, approve spec, generate tech spec, check spec status.
