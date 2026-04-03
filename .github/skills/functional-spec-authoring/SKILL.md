---
name: functional-spec-authoring
description: 'Guide iterative authoring of functional specifications as Azure DevOps work item hierarchies (Epic → Feature → User Story). Use for business requirement capture, scope definition, acceptance criteria, and spec approval readiness.'
argument-hint: 'Describe the business need or feature idea to explore, or provide an existing Epic ID to refine.'
---

# Functional Spec Authoring

## When to Use
- Defining a new business capability as an Azure DevOps Epic with Features and User Stories
- Iterating on requirements with stakeholders before technical specification
- Refining acceptance criteria, scope boundaries, or success metrics
- Reviewing a draft spec for approval readiness

## Hierarchy Model

| Level | Purpose | Maps to |
|-------|---------|---------|
| **Epic** | WHY — business justification and strategic goal | Azure DevOps Epic |
| **Feature** | WHAT — capability-level scope and boundaries | Azure DevOps Feature (child of Epic) |
| **User Story** | WHAT — detailed user-facing behavior and criteria | Azure DevOps User Story (child of Feature) |

Tasks and Bugs are NOT part of the functional spec — they belong to technical implementation.

## Lifecycle Tags (applied to the Epic)

| Tag | Meaning | Transition Rule |
|-----|---------|-----------------|
| `spec:draft` | Initial authoring in progress | Applied on Epic creation |
| `spec:review` | Ready for stakeholder review | Applied when all Features and Stories have descriptions and acceptance criteria |
| `spec:approved` | Stakeholders have signed off | Applied after review; enables tech spec generation |

## Work Item Templates

### Epic Template (Description field — HTML format)

```html
<h2>Business Justification</h2>
<p>[WHY this epic exists — the business problem or opportunity being addressed]</p>

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
<li>[Stakeholder name — role]</li>
</ul>

<h2>Assumptions &amp; Constraints</h2>
<ul>
<li>[Known assumption or constraint]</li>
</ul>
```

### Feature Template (Description field — HTML format)

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
```

Feature **Acceptance Criteria** field:
```
- [ ] [Criterion 1 — capability-level verification]
- [ ] [Criterion 2]
```

### User Story Template (Description field — HTML format)

```html
<h2>User Story</h2>
<p>As a [persona], I want to [action], so that [benefit].</p>

<h2>Acceptance Criteria</h2>
<ul>
<li>Given [precondition], when [action], then [expected result]</li>
<li>Given [precondition], when [action], then [expected result]</li>
</ul>

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

## Authoring Workflow

### Phase 1: Discovery
1. Gather the business need from the user (ask clarifying questions if vague)
2. Draft the Epic with business justification, success criteria, and initial scope
3. Create the Epic in Azure DevOps with tag `spec:draft`

### Phase 2: Feature Decomposition
1. Identify distinct capabilities within the Epic's scope
2. For each capability, create a Feature as a child of the Epic
3. Define scope boundaries and business value for each Feature
4. Review with user: are these the right Features? Any missing? Any overlap?

### Phase 3: Story Definition
1. For each Feature, decompose into user-facing behaviors
2. Create User Stories as children of Features using the template
3. Write acceptance criteria in Given/When/Then format
4. Identify edge cases, validation rules, and UI/UX notes
5. Review with user: are criteria clear and testable?

### Phase 4: Review Readiness
1. Verify every Feature has a description and acceptance criteria
2. Verify every User Story has acceptance criteria in Given/When/Then format
3. Verify Epic scope matches the sum of Features (no gaps, no excess)
4. Update Epic tag from `spec:draft` to `spec:review`
5. Present a summary to the user for stakeholder review

### Phase 5: Approval
1. Address stakeholder feedback by refining Features/Stories
2. When stakeholders approve, update Epic tag from `spec:review` to `spec:approved`
3. This unlocks technical specification generation

## Iteration Rules

- **Refining a Story**: Update its description and acceptance criteria; add a comment documenting the change reason
- **Splitting a Story**: Create new child Stories under the same Feature; mark the original as removed or update to reference the split
- **Adding a Feature**: Create as child of Epic; add relevant Stories
- **Removing a Feature**: Set state to Removed; add a comment explaining why
- **Scope change after approval**: Must revert Epic tag to `spec:review` and re-approve before tech spec generation

## Quality Gates

A functional spec is ready for `spec:review` when:
- [ ] Epic has business justification, success criteria, and scope defined
- [ ] Every Feature has a description and at least one acceptance criterion
- [ ] Every User Story has acceptance criteria in Given/When/Then format
- [ ] No placeholder text remains (no `[TODO]`, `[TBD]`, or empty sections)
- [ ] Scope boundaries are explicit (In Scope / Out of Scope defined)

## Decision Points

- If the user provides a vague idea, ask clarifying questions before creating the Epic
- If a Feature has more than 8 User Stories, consider splitting it into two Features
- If acceptance criteria are ambiguous, ask for specific examples or scenarios
- If scope creep is detected during iteration, flag it and ask the user to decide

## Completion Checks

- Epic, all Features, and all User Stories exist in Azure DevOps with parent-child links
- All templates are fully populated (no placeholders)
- Epic is tagged `spec:review` or `spec:approved`
- A summary of the hierarchy has been presented to the user
