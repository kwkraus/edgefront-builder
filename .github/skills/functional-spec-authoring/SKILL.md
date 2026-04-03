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

## Work Item Templates

### Epic Template (Description field - HTML format)

```html
<h2>Business Justification</h2>
<p>[WHY this epic exists - the business problem or opportunity being addressed]</p>

<h2>Success Criteria</h2>
<ul>
<li>[Measurable outcome 1]</li>
<li>[Measurable outcome 2]</li>
</ul>

<h2>Scope</h2>
<h3>In Scope</h3>
<ul>
<li>[Capability included]</li>
</ul>
<h3>Out of Scope</h3>
<ul>
<li>[Explicitly excluded item]</li>
</ul>

<h2>Stakeholders</h2>
<ul>
<li>[Stakeholder name - role]</li>
</ul>

<h2>Assumptions &amp; Constraints</h2>
<ul>
<li>[Known assumption or constraint]</li>
</ul>
```

### Feature Template (Description field - HTML format)

```html
<h2>Capability</h2>
<p>[WHAT this feature delivers at a high level]</p>

<h2>Business Value</h2>
<p>[How this feature contributes to the Epic's goals]</p>

<h2>Functional Scope</h2>
<ul>
<li>[Detailed scope boundary]</li>
</ul>

<h2>Dependencies</h2>
<ul>
<li>[Other features or systems this depends on]</li>
</ul>

<h2>Acceptance Criteria</h2>
<ul>
<li>[Capability-level verification 1]</li>
<li>[Capability-level verification 2]</li>
</ul>
```

### User Story Template (Description field - HTML format)

```html
<h2>User Story</h2>
<p>As a [persona], I want to [action], so that [benefit].</p>

<h2>UI/UX Notes</h2>
<p>[Any relevant UI behavior, layouts, or interaction patterns]</p>

<h2>Data &amp; Validation Rules</h2>
<ul>
<li>[Input validation, data transformations, business rules]</li>
</ul>

<h2>Edge Cases</h2>
<ul>
<li>[Known edge case and expected behavior]</li>
</ul>
```

### User Story Acceptance Criteria Field

Store the testable criteria in `Microsoft.VSTS.Common.AcceptanceCriteria`, not in Description.

```text
Given [precondition], when [action], then [expected result]
Given [precondition], when [action], then [expected result]
```

## Authoring Workflow

### Phase 1: Discovery
1. Gather the business need from the user. Ask clarifying questions if the request is vague.
2. Draft the Epic with business justification, success criteria, and initial scope.
3. Create the Epic in Azure DevOps in state `New`.

### Phase 2: Feature Decomposition
1. Identify distinct capabilities within the Epic's scope.
2. For each capability, create a Feature as a child of the Epic.
3. Put capability-level acceptance criteria inside the Feature Description.
4. Review with the user: are these the right Features, and do the boundaries make sense?

### Phase 3: Story Definition
1. For each Feature, decompose into user-facing behaviors.
2. Create User Stories as children of Features.
3. Put the story statement and supporting notes in Description.
4. Put Given/When/Then criteria in the User Story Acceptance Criteria field.
5. Review with the user: are the criteria clear, testable, and complete?

### Phase 4: Review Readiness
1. Verify the Epic Description is complete.
2. Verify every Feature Description is complete and includes an `Acceptance Criteria` subsection.
3. Verify every User Story has both:
   - a Description with the story statement and supporting notes
   - a non-empty Acceptance Criteria field
4. Add `review:ready` to the Epic while keeping the hierarchy in state `New`.
5. Present a summary to the user for stakeholder review.

### Phase 5: Approval
1. Address stakeholder feedback by refining the Epic, Features, and Stories.
2. When stakeholders approve, remove `review:ready`.
3. Move the approved Epic and implementation-ready child Features/User Stories from `New` to `Active`.
4. Add a structured approval comment on the Epic:

```markdown
✅ **Functional spec approved for implementation**

- Approved by: [name]
- Approved on: [YYYY-MM-DD]
- Notes: [summary of decision or constraints]
```

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
