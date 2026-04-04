---
name: functional-spec-authoring
description: 'Guide iterative authoring of functional specifications as Azure DevOps work item hierarchies (Epic → Feature → User Story). Use for business requirement capture, scope definition, acceptance criteria, and approval readiness.'
argument-hint: 'Describe the business need or feature idea to explore, or provide an existing Epic ID to refine.'
---

# Functional Spec Authoring

## When to Use
- Defining a new business capability as an Azure DevOps Epic with Features and User Stories
- Iterating on requirements with stakeholders before technical specification
- Refining acceptance criteria, scope boundaries, or success metrics
- Preparing a spec for stakeholder review or approval

## Hierarchy Model

| Level | Purpose | Maps to |
|-------|---------|---------|
| **Epic** | WHY - business justification and strategic goal | Azure DevOps Epic |
| **Feature** | WHAT - capability-level scope and boundaries | Azure DevOps Feature (child of Epic) |
| **User Story** | WHAT - detailed user-facing behavior and criteria | Azure DevOps User Story (child of Feature) |

Tasks and Bugs are not part of the functional spec. They belong to technical implementation.

## State Model

Use Azure DevOps Agile states as the primary lifecycle model.

| State | Meaning | Notes |
|-------|---------|-------|
| `New` | Drafting and stakeholder review | Default while authoring and refining |
| `Active` | Approved and ready for implementation | Enter only after stakeholder sign-off and approval comment |
| `Resolved` | Implementation complete | Delivery tracking, not authoring |
| `Closed` | Validation complete | Delivery tracking, not authoring |
| `Removed` | No longer needed | Use when abandoning or deleting scope |

Do not add custom states for this workflow.

## Supplemental Query Tags (Epic only)

| Tag | Meaning | Why it exists |
|-----|---------|---------------|
| `review:ready` | Spec is ready for stakeholder review while still in `New` | Supports Azure DevOps queries and dashboards for review meetings |
| `techspec:stale` | Functional changes have invalidated the current technical specification | Supports dashboards and follow-up regeneration work |

Tags are operational signals only. They do not replace Azure DevOps State.

## Work Item Field Model

| Work item type | Description field | Acceptance Criteria field |
|----------------|-------------------|---------------------------|
| **Epic** | Canonical source for business justification, success criteria, scope, stakeholders, and constraints | Not used as a separate canonical field |
| **Feature** | Canonical source for capability, business value, scope, dependencies, and feature-level acceptance criteria | Not used in the Agile template for Features |
| **User Story** | Canonical source for story statement, UI/UX notes, data rules, and edge cases | Canonical source for executable Given/When/Then criteria (`Microsoft.VSTS.Common.AcceptanceCriteria`) |

## Output Contracts

The section-only templates for functional spec content are user-editable files. Treat them as the canonical source for section headings, ordering, and placeholder structure.

| Artifact | Format | Canonical template |
|----------|--------|--------------------|
| **Epic Description** | HTML | `.github\skills\functional-spec-authoring\templates\epic-description.html` |
| **Feature Description** | HTML | `.github\skills\functional-spec-authoring\templates\feature-description.html` |
| **User Story Description** | HTML | `.github\skills\functional-spec-authoring\templates\user-story-description.html` |
| **User Story Acceptance Criteria** | Plain text | `.github\skills\functional-spec-authoring\templates\user-story-acceptance-criteria.txt` |

### Output Validation Rules

1. Load the relevant template file before generating or validating output.
2. Preserve the required section headings and ordering from the template file.
3. Keep prose flexible inside each section; do not force identical wording.
4. If a template file changes, follow the new structure automatically unless it conflicts with the Agile field model in this skill.
5. If a template change conflicts with field mapping or lifecycle rules, STOP and ask the user rather than inventing behavior.
6. Reject or repair output that is missing required sections or still contains unresolved placeholders.

## Authoring Workflow

> **Key principle:** Do not create any Azure DevOps work items until the user approves the full hierarchy preview in Phase 4. Phases 1–3 are entirely conversational.

### Phase 1: Discovery (conversational only)
1. Gather the business need from the user. Ask clarifying questions until the idea reaches a logical conclusion.
2. Draft the Epic description in-conversation: business justification, success criteria, initial scope, stakeholders, and constraints. Use the sections from `epic-description.html` as a guide.
3. **Do NOT create any work items in Azure DevOps yet.**

### Phase 2: Feature Decomposition (conversational only)
1. Identify distinct capabilities within the Epic's scope.
2. Draft each Feature description in-conversation: capability, business value, scope boundaries, dependencies, and acceptance criteria. Use the sections from `feature-description.html` as a guide.
3. Review with the user: are these the right Features, and do the boundaries make sense?
4. **Do NOT create any work items in Azure DevOps yet.**

### Phase 3: Story Definition (conversational only)
1. For each Feature, decompose into user-facing behaviors.
2. Draft each User Story in-conversation: story statement, supporting notes, and Given/When/Then acceptance criteria. Use `user-story-description.html` and `user-story-acceptance-criteria.txt` as guides.
3. Review with the user: are the criteria clear, testable, and complete?
4. **Do NOT create any work items in Azure DevOps yet.**

### Phase 4: Hierarchy Preview and Approval
1. Verify all quality gates pass (check in-conversation content for completeness before showing the preview).
2. Present the complete hierarchy as a structured preview in the conversation using this format:

```
**Epic**: [Title]
[1–2 sentence business justification summary]

  **Feature 1**: [Title]
  [Scope summary — 1 sentence]
    - User Story 1.1: [Title]
      Given/When/Then summary
    - User Story 1.2: [Title]
      Given/When/Then summary

  **Feature 2**: [Title]
  ...
```

3. Ask the user to review the hierarchy and either approve it or request changes.
4. If the user requests changes, loop back to the relevant Phase (1, 2, or 3) and iterate. Re-present the preview after changes.
5. After explicit user approval, create all work items in Azure DevOps in this order:
   - Create the Epic in state `New` using `epic-description.html` template
   - Create all Features as children of the Epic
   - Create all User Stories as children of their respective Features
6. After creation, confirm completion by listing all created work item IDs and titles.

### Phase 5: Review Readiness
1. Verify the Epic Description is complete.
2. Verify every Feature Description is complete and includes an `Acceptance Criteria` subsection.
3. Verify every User Story has both:
   - a Description with the story statement and supporting notes
   - a non-empty Acceptance Criteria field
4. Add `review:ready` to the Epic while keeping the hierarchy in state `New`.
5. Present a summary to the user for stakeholder review.

### Phase 6: Approval
1. Address stakeholder feedback by refining the Epic, Features, and Stories.
2. When stakeholders approve, remove `review:ready`.
3. Move the approved Epic and implementation-ready child Features/User Stories from `New` to `Active`.
4. Add a structured approval comment on the Epic using `.github\skills\spec-lifecycle-management\templates\approval-comment.md`.

This approval unlocks technical specification generation.

## Iteration Rules

- **Refining a User Story**: Update Description and/or the Acceptance Criteria field. Add a comment documenting why.
- **Splitting a User Story**: Create new child Stories under the same Feature. Mark the original as removed or update it to reference the split.
- **Adding a Feature**: Create it as a child of the Epic, then add the relevant Stories.
- **Removing a Feature**: Set state to `Removed` and add a comment explaining why.
- **Scope change after approval**: If stakeholder review is reopened, move affected items back to `New`, remove `review:ready` until the revision is ready again, and add `techspec:stale` to the Epic if a technical spec already exists.

## Quality Gates

A functional spec is ready for stakeholder review when:
- [ ] Epic Description has business justification, success criteria, and scope
- [ ] Every Feature Description includes capability, business value, scope, dependencies, and acceptance criteria
- [ ] Every User Story Description is complete
- [ ] Every User Story Acceptance Criteria field is populated with Given/When/Then criteria
- [ ] All generated artifacts conform to the current canonical template files
- [ ] No placeholder text remains (`[TODO]`, `[TBD]`, or empty sections)
- [ ] Scope boundaries are explicit (In Scope / Out of Scope defined)

## Decision Points

- If the user provides a vague idea, ask clarifying questions before creating the Epic.
- If a Feature has more than 8 User Stories, consider splitting it into two Features.
- If acceptance criteria are ambiguous, ask for specific examples or scenarios.
- If scope creep is detected during iteration, flag it and ask the user to decide.

## Completion Checks

- Epic, all Features, and all User Stories exist in Azure DevOps with parent-child links
- Feature acceptance criteria live in Description, not a separate field
- User Story acceptance criteria live in the Acceptance Criteria field
- The hierarchy is either:
  - `New` with Epic tagged `review:ready`, or
  - `Active` with an approval comment on the Epic
- No placeholder text remains
