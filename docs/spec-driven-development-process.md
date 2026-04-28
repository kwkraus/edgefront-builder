# Spec-Driven Development Process

This document describes the spec-driven development process used in this project. Functional specifications are authored as Azure DevOps work item hierarchies, and technical specifications are generated as wiki pages.

## Overview

The process has two phases:

1. **Functional Specification** - iteratively define business requirements as an Epic -> Feature -> User Story hierarchy in Azure DevOps
2. **Technical Specification** - generate an implementation-ready technical spec from the approved functional spec and publish it as a wiki page

Azure DevOps Agile **State** is the primary lifecycle signal. Tags are used only for queryable operational signals.

## Agents

| Agent | Purpose |
|-------|---------|
| `spec-driven-development` | Orchestrates the full spec process: authoring, review readiness, approval, tech spec generation, and change management |
| `devops-workitem-manager` | General board CRUD, sprint planning, task/bug management, and spec-aware edits |

Both agents are pre-configured for organization `kkraus` and project `edgefront-builder`.

## Work Item Hierarchy

| Level | Work Item Type | Purpose |
|-------|---------------|---------|
| **Epic** | Epic | WHY - business justification and strategic goal |
| **Feature** | Feature | WHAT - capability-level scope and boundaries |
| **User Story** | User Story | WHAT - detailed user-facing behavior and acceptance criteria |

Tasks and Bugs are not part of the functional spec. They belong to technical implementation managed by `devops-workitem-manager`.

## State Model

| State | Meaning |
|-------|---------|
| `New` | Drafting and stakeholder review |
| `Active` | Approved and ready for implementation |
| `Resolved` | Implementation complete |
| `Closed` | Validation complete |
| `Removed` | No longer needed |

No custom Azure DevOps states are used for this workflow.

## Supplemental Query Tags

Tags are only used where dashboards and queries benefit from extra visibility.

| Tag | Meaning |
|-----|---------|
| `review:ready` | The spec is ready for stakeholder review while still in `New` |
| `techspec:stale` | The technical specification is no longer valid and must be regenerated |

## Field Model

The process aligns to the Azure DevOps **Agile** template.

| Work Item Type | Description field | Acceptance Criteria field |
|----------------|-------------------|---------------------------|
| **Epic** | Canonical source for business justification, scope, stakeholders, and success criteria | Not used as a separate canonical field |
| **Feature** | Canonical source for capability, business value, scope, dependencies, and feature-level acceptance criteria | Not used for Features in Agile |
| **User Story** | Canonical source for story statement, UI/UX notes, data rules, and edge cases | Canonical source for executable Given/When/Then criteria |

## Workflow

### Phase 1: Functional Specification

1. **Invoke** the `spec-driven-development` agent with the business need
2. **Discover** - the agent asks clarifying questions to understand scope
3. **Create Epic** - create the Epic in state `New`
4. **Decompose into Features** - capability-level work items as children of the Epic
5. **Define User Stories** - detailed behaviors under each Feature
6. **Populate fields correctly**
   - Feature acceptance criteria stay in the **Feature Description**
   - User Story acceptance criteria go in the **Acceptance Criteria field**
7. **Mark ready for review** - when the hierarchy is complete, add `review:ready` to the Epic while it remains in `New`
8. **Approve** - after stakeholder sign-off:
   - remove `review:ready`
   - move the approved hierarchy to `Active`
   - add a structured approval comment on the Epic

### Phase 2: Technical Specification

1. **Verify approval** - the Epic must be `Active` and have an approval comment
2. **Pull hierarchy** - read the full Epic -> Feature -> Story tree via MCP
3. **Analyze** - identify architecture, data model, API, and test implications
4. **Generate** - produce a structured tech spec using the `technical-spec-generation` skill
5. **Publish** - create or update a wiki page at `/Tech-Specs/[Epic-ID]-[Slugified-Title]`
6. **Link** - add an Epic comment with the wiki page URL
7. **Clear stale flag** - remove `techspec:stale` if present

### Phase 3: Change Management

If functional spec content changes after a tech spec is generated:

1. **Detect** - inspect revisions across the Epic, Features, and User Stories
2. **Flag** - add `techspec:stale` and an audit comment on the Epic
3. **Review changes** - if the changes materially alter the approved scope, refresh stakeholder approval evidence
4. **Regenerate** - generate a new version of the tech spec when ready
5. **Clear stale flag** - remove `techspec:stale` after regeneration

## Review and Approval Evidence

### Review Readiness
- `review:ready` is the signal used for Azure DevOps review queries and dashboard views
- The Epic remains in state `New`

### Approval
- Approval is recorded by:
  1. moving the approved hierarchy to `Active`
  2. adding a structured approval comment on the Epic
- The `spec-lifecycle-management` skill owns the approval comment format

### Technical Spec Staleness
- Staleness is recorded by:
  1. adding `techspec:stale`
  2. adding a staleness comment on the Epic

## Staleness Rules

These changes invalidate the current technical specification:

- Epic Description changes
- Feature Description changes
- User Story Description changes
- User Story Acceptance Criteria changes
- Feature or User Story hierarchy changes (added, removed, or reparented items)

These do **not** invalidate the technical specification by themselves:

- Priority changes
- Iteration changes
- Assignment changes
- Comments
- State changes by themselves

## Prerequisites

### One-Time Setup

1. **Enable Epic backlog**: Azure DevOps -> Project Settings -> Team Configuration -> Backlogs -> enable "Epics"
2. **Create project wiki**: Azure DevOps -> Wiki -> Create project wiki (`edgefront-builder.wiki`)

These steps cannot be automated via MCP and must be done manually in the Azure DevOps web UI.

## Skill-Owned Templates

The canonical process rules live in:

- `.github/skills/functional-spec-authoring/SKILL.md`
- `.github/skills/technical-spec-generation/SKILL.md`
- `.github/skills/spec-lifecycle-management/SKILL.md`

Each skill owns its own internal templates and explains how to use them. Agents should invoke the appropriate skill for template-backed output rather than depending on template file names or folder structure.

## Agent Coordination

The `spec-driven-development` agent owns the spec process. The `devops-workitem-manager` agent is spec-aware:

- It warns before changing functional content on spec-managed items
- It can add `techspec:stale` when confirmed edits invalidate an existing tech spec
- It does not own approval, review readiness, or tech spec generation
- It handles sprint planning, task/bug management, and general board queries

### When to Use Which Agent

| Task | Agent |
|------|-------|
| Define a new business capability | `spec-driven-development` |
| Write or refine functional spec content | `spec-driven-development` |
| Mark ready for review | `spec-driven-development` |
| Approve a spec for implementation | `spec-driven-development` |
| Generate technical spec | `spec-driven-development` |
| Check spec lifecycle status | `spec-driven-development` |
| Plan a sprint | `devops-workitem-manager` |
| Create tasks or bugs | `devops-workitem-manager` |
| Update work item status/assignment | `devops-workitem-manager` |
| Query the board | `devops-workitem-manager` |

## Relationship to docs/specs/

This spec-driven development process is **independent** of the existing `docs/specs/` system.

- `docs/specs/` - implementation specifications committed to the repository
- Spec-driven development - business and technical specifications managed in Azure DevOps

They may coexist and can cross-reference each other, but neither depends on the other.
