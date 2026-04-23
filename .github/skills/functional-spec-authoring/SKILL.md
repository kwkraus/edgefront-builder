---
name: functional-spec-authoring
description: 'Guide iterative authoring of functional specifications as Azure DevOps work item hierarchies (Epic → Feature → User Story). Use for business requirement capture, scope definition, acceptance criteria, and approval readiness.'
argument-hint: 'Describe the business need or feature idea to explore, or provide an existing Epic ID to refine.'
---

# Functional Spec Authoring

## When to Use
- Define a new capability as Epic → Feature → User Story
- Iterate requirements before technical spec
- Prepare a spec for review or approval

## Hierarchy

| Level | Purpose | Azure DevOps type |
|-------|---------|-------------------|
| Epic | WHY — business justification, strategic goal | Epic |
| Feature | WHAT — capability scope, boundaries | Feature (child of Epic) |
| User Story | WHAT — user-facing behavior, criteria | User Story (child of Feature) |

Tasks/Bugs are implementation, not functional spec.

## Field Model

| Type | Description field | Acceptance Criteria field |
|------|-------------------|---------------------------|
| Epic | business justification, success criteria, scope, stakeholders, constraints | not used |
| Feature | capability, value, scope, dependencies, **embedded** acceptance criteria | not used |
| User Story | story statement, UI/UX notes, data rules, edge cases | Given/When/Then (`Microsoft.VSTS.Common.AcceptanceCriteria`) |

Description = HTML. Comments/wiki = markdown.

## Templates (canonical, user-editable)

| Artifact | Format | Path |
|----------|--------|------|
| Epic Description | HTML | `.github\skills\functional-spec-authoring\templates\epic-description.html` |
| Feature Description | HTML | `.github\skills\functional-spec-authoring\templates\feature-description.html` |
| User Story Description | HTML | `.github\skills\functional-spec-authoring\templates\user-story-description.html` |
| User Story AC | plain text | `.github\skills\functional-spec-authoring\templates\user-story-acceptance-criteria.txt` |

Rules: load template before generating; preserve section headings/order; reject output with unresolved placeholders; if template conflicts with field model, STOP and ask.

State model, tags, transitions → see `spec-lifecycle-management` skill.

## Authoring Workflow

**Key principle:** create NO Azure DevOps items until user approves Phase 4 preview.

| Phase | Action | Create items? |
|-------|--------|---------------|
| 1 Discovery | Gather need; draft Epic in-chat per template | No |
| 2 Feature Decomposition | Identify capabilities; draft Features; confirm boundaries | No |
| 3 Story Definition | Decompose into stories + G/W/T; confirm clarity | No |
| 4 Preview + Approval | Show full hierarchy; on approval create Epic → Features → Stories in `New` | Yes |
| 5 Review Readiness | Verify all fields populated; add `review:ready` to Epic | — |
| 6 Approval | On stakeholder sign-off: remove `review:ready`, move Epic+children `New → Active`, add approval comment | — |

Preview format:
```
**Epic**: [Title]
[1–2 sentence summary]
  **Feature 1**: [Title] — [scope]
    - User Story 1.1: [Title] — G/W/T summary
```

Approval comment: `.github\skills\spec-lifecycle-management\templates\approval-comment.md`. Approval unlocks tech spec generation.

## Iteration Rules

| Change | Action |
|--------|--------|
| Refine Story | Update Description/AC + comment reason |
| Split Story | New child Stories; mark original removed or cross-reference |
| Add Feature | Create child of Epic + its Stories |
| Remove Feature | State → `Removed` + comment |
| Post-approval scope change | Move affected items to `New`, drop `review:ready`, add `techspec:stale` if tech spec exists |

## Quality Gates (ready for review)
- Epic Description has justification, success criteria, scope
- Every Feature Description has capability, value, scope, dependencies, AC
- Every User Story Description complete
- Every User Story AC field populated with G/W/T
- Artifacts match current template files
- No `[TODO]`/`[TBD]`/empty sections
- In Scope / Out of Scope explicit

## Decision Points
- Vague idea → ask clarifying questions before creating Epic
- Feature > 8 Stories → consider split
- Ambiguous AC → ask for examples
- Scope creep during iteration → flag to user

## Completion Checks
- Epic + Features + Stories exist with parent-child links
- Feature AC in Description; User Story AC in AC field
- Hierarchy is `New`+`review:ready` OR `Active`+approval comment
- No placeholder text remains
