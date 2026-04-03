# Spec-Driven Development Process

This document describes the spec-driven development process used in this project. Functional specifications are authored as Azure DevOps work item hierarchies, and technical specifications are generated as wiki pages.

## Overview

The process has two phases:

1. **Functional Specification** — iteratively define business requirements as an Epic → Feature → User Story hierarchy in Azure DevOps
2. **Technical Specification** — generate an implementation-ready technical spec from the approved functional spec and publish it as a wiki page

A lifecycle state machine tracks progress and enforces gates between phases.

## Agents

| Agent | Purpose |
|-------|---------|
| `spec-driven-development` | Orchestrates the full spec process: authoring, approval, tech spec generation, change management |
| `devops-workitem-manager` | General board CRUD, sprint planning, task/bug management (spec-lifecycle-aware) |

Both agents are pre-configured for organization `kkraus` and project `edgefront-builder`.

## Work Item Hierarchy

| Level | Work Item Type | Purpose |
|-------|---------------|---------|
| **Epic** | Epic | WHY — business justification and strategic goal |
| **Feature** | Feature | WHAT — capability-level scope and boundaries |
| **User Story** | User Story | WHAT — detailed user-facing behavior and acceptance criteria |

Tasks and Bugs are NOT part of the functional spec — they belong to technical implementation managed by `devops-workitem-manager`.

## Lifecycle Tags

Tags are applied to the **Epic** to track lifecycle state.

### Functional Spec Tags (mutually exclusive)

| Tag | Meaning |
|-----|---------|
| `spec:draft` | Functional spec is being authored or revised |
| `spec:review` | Functional spec is ready for stakeholder review |
| `spec:approved` | Stakeholders have approved the functional spec |

### Technical Spec Tags (mutually exclusive)

| Tag | Meaning |
|-----|---------|
| `techspec:current` | A technical spec exists and is up-to-date |
| `techspec:stale` | Technical spec is outdated due to functional spec changes |

### State Machine

```
[New Epic] → spec:draft → spec:review → spec:approved → techspec:current
                ▲              │                              │
                │              ▼                              ▼
                │        (feedback →                   (functional spec
                │         spec:draft)                   changes detected)
                │                                             │
                │                                             ▼
                │                                       techspec:stale
                │                                             │
                └─────── (revise & re-approve) ───────────────┘
```

## Workflow

### Phase 1: Functional Specification

1. **Invoke** the `spec-driven-development` agent with your business need
2. **Discover** — the agent asks clarifying questions to understand scope
3. **Create Epic** — business justification, success criteria, scope (tagged `spec:draft`)
4. **Decompose into Features** — capability-level work items as children of the Epic
5. **Define User Stories** — detailed behaviors under each Feature with acceptance criteria
6. **Iterate** — refine with stakeholders until quality gates pass
7. **Review** — transition to `spec:review` when all items have descriptions and criteria
8. **Approve** — transition to `spec:approved` after stakeholder sign-off

### Phase 2: Technical Specification

1. **Verify** the Epic has `spec:approved` — the agent refuses to generate otherwise
2. **Pull hierarchy** — the agent reads the full Epic → Feature → Story tree via MCP
3. **Analyze** — identify architecture, data model, API, and test implications
4. **Generate** — produce a structured tech spec using the wiki page template
5. **Publish** — create a wiki page at `/Tech-Specs/[Epic-ID]-[Slugified-Title]`
6. **Link** — add a comment to the Epic with the wiki page URL
7. **Tag** — add `techspec:current` to the Epic

### Phase 3: Change Management

If functional spec items change after a tech spec is generated:

1. **Detect** — the agent checks Epic revision history against the tech spec generation point
2. **Flag** — transition `techspec:current` → `techspec:stale` with an audit comment
3. **Revise** — update functional spec items as needed
4. **Re-approve** — transition back through `spec:review` → `spec:approved`
5. **Regenerate** — generate a new version of the tech spec (version incremented)

## Prerequisites

### One-Time Setup

1. **Enable Epic backlog**: In Azure DevOps, go to Project Settings → Team Configuration → Backlogs → enable "Epics"
2. **Create project wiki**: In Azure DevOps, go to Project → Wiki → Create project wiki (named `edgefront-builder.wiki`)

These steps cannot be automated via MCP and must be done manually in the Azure DevOps web UI.

## Templates

Work item descriptions and tech spec wiki pages follow consistent templates defined in the skills:

- **Epic, Feature, User Story templates**: See `.github/skills/functional-spec-authoring/SKILL.md`
- **Tech spec wiki page template**: See `.github/skills/technical-spec-generation/SKILL.md`
- **Lifecycle rules and staleness detection**: See `.github/skills/spec-lifecycle-management/SKILL.md`

## Agent Coordination

The `spec-driven-development` agent owns the spec process. The `devops-workitem-manager` agent is spec-lifecycle-aware:

- It warns before editing Description or Acceptance Criteria on spec-managed items
- It can trigger staleness if edits are confirmed on items with `techspec:current`
- It does NOT manage spec lifecycle tags or generate tech specs
- It handles sprint planning, task/bug management, and general board queries

### When to Use Which Agent

| Task | Agent |
|------|-------|
| Define a new business capability | `spec-driven-development` |
| Write/refine functional spec | `spec-driven-development` |
| Generate technical spec | `spec-driven-development` |
| Check spec lifecycle status | `spec-driven-development` |
| Plan a sprint | `devops-workitem-manager` |
| Create tasks or bugs | `devops-workitem-manager` |
| Update work item status/assignment | `devops-workitem-manager` |
| Query the board | `devops-workitem-manager` |

## Relationship to docs/specs/

This spec-driven development process is **independent** of the existing `docs/specs/` system. The two systems serve different purposes:

- `docs/specs/` — implementation specifications committed to the repository (code-adjacent)
- Spec-driven development — business and technical specifications managed in Azure DevOps (stakeholder-facing)

They may coexist and can cross-reference each other, but neither depends on the other.
