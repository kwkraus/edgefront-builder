---
name: technical-spec-generation
description: 'Generate technical specifications from approved functional specs (Azure DevOps Epic hierarchies) and publish as wiki pages. Use after functional approval to create implementation-ready technical documentation.'
argument-hint: 'Provide the Epic ID to generate a technical specification for, or ask to review/update an existing tech spec.'
---

# Technical Spec Generation

## When to Use
- Generating a technical specification from an approved functional spec
- Updating a technical spec after functional spec revisions
- Reviewing an existing technical spec for completeness before implementation begins

## Prerequisites
- The Epic must be in state `Active`
- The Epic must have a structured approval comment recording stakeholder sign-off
- The Epic must not be tagged `review:ready`
- An Azure DevOps project wiki must exist (named `edgefront-builder.wiki`)
  - **Manual setup required**: Go to Azure DevOps -> Project -> Wiki -> Create project wiki
  - This only needs to be done once per project

## Functional Input Model

When reading the hierarchy, treat these fields as canonical:

| Work item type | Canonical fields |
|----------------|------------------|
| **Epic** | Description |
| **Feature** | Description (including embedded acceptance criteria) |
| **User Story** | Description + `Microsoft.VSTS.Common.AcceptanceCriteria` |

## Generation Workflow

### Step 1: Validate Preconditions
1. Fetch the Epic by ID.
2. Verify the Epic state is `Active`.
3. Verify the Epic has an approval comment.
4. If `review:ready` is present, STOP and inform the user the spec is still awaiting stakeholder review.
5. If `techspec:stale` is present, warn the user and proceed with regeneration only after confirming the functional changes are understood and approved.

### Step 2: Pull the Work Item Hierarchy
1. Fetch the Epic details (ID, title, description, state, tags, comments).
2. Fetch all child Features with their Descriptions.
3. For each Feature, fetch all child User Stories with:
   - Description
   - Acceptance Criteria field
4. Build a structured hierarchy in memory.

### Step 3: Analyze and Design
1. Review the functional requirements and identify:
   - Architecture and design decisions needed
   - Components affected or to be created
   - Data model changes required
   - API contracts to add or modify
   - External dependencies and integration points
   - Security considerations
   - Test strategy per Feature
2. Ask the user clarifying questions about technical approach if ambiguous.
3. Document risks and open questions.

### Step 4: Generate the Technical Specification
Use the wiki page template below. Fill in every section based on the functional spec hierarchy and technical analysis.

### Step 5: Publish to Wiki
1. Create or update the wiki page at path: `/Tech-Specs/[Epic-ID]-[Slugified-Epic-Title]`
   - Example: `/Tech-Specs/356-Session-Import-from-CSV`
2. Use MCP tool `wiki_create_or_update_page` with:
   - `wikiIdentifier`: `edgefront-builder.wiki`
   - `project`: `edgefront-builder`
   - `path`: `/Tech-Specs/[Epic-ID]-[Slugified-Title]`
   - `content`: the generated markdown

### Step 6: Link, Comment, and Clear Staleness
1. Add a comment to the Epic with a link to the wiki page:

```markdown
📄 **Technical Specification v[VERSION]**: [View Tech Spec](https://dev.azure.com/kkraus/edgefront-builder/_wiki/wikis/edgefront-builder.wiki/Tech-Specs/[Epic-ID]-[Title])

- Generated on: [YYYY-MM-DD]
- Based on Epic state: Active
- Based on [N] Features and [M] User Stories
- Notes: [summary of regeneration reason if applicable]
```

2. Remove `techspec:stale` if present.
3. If this is a regeneration, add a second comment summarizing what changed from the prior version.

## Wiki Page Template

```markdown
# Technical Specification: [Epic Title]

| Field | Value |
|-------|-------|
| **Epic** | #[Epic ID] - [Epic Title] |
| **Epic State** | Active |
| **Version** | [1.0, 2.0, etc.] |
| **Generated** | [YYYY-MM-DD] |
| **Approval Evidence** | [Reference to Epic approval comment] |

---

## 1. Overview

[Brief summary of what this tech spec covers. Reference the business justification from the Epic. State the technical goal.]

## 2. Architecture & Design Decisions

[High-level architecture approach. Key design decisions and their rationale. Reference existing system patterns where applicable.]

### 2.1 Component Design

| Component | Responsibility | New/Modified |
|-----------|---------------|--------------|
| [Component name] | [What it does] | [New / Modified] |

### 2.2 Data Model Changes

[Schema changes, new entities, relationship changes, migration requirements.]

```sql
-- Example migration sketch
ALTER TABLE ... ADD COLUMN ...
CREATE TABLE ...
```

### 2.3 API Contracts

| Endpoint | Verb | Purpose | Request | Response |
|----------|------|---------|---------|----------|
| [Route] | [GET/POST/etc.] | [Purpose] | [DTO or body shape] | [Response shape + status codes] |

## 3. Implementation Plan

[Ordered implementation steps, grouped by Feature.]

### Feature: [Feature Title] (#[Feature ID])

| User Story | Implementation Approach | Files Affected | Complexity |
|-----------|------------------------|----------------|------------|
| #[Story ID] - [Title] | [Approach summary] | [File paths] | Low / Medium / High |

[Repeat for each Feature]

## 4. Dependencies & Integration Points

| Dependency | Type | Impact |
|-----------|------|--------|
| [Dependency] | [Internal/External] | [What happens if unavailable] |

## 5. Security Considerations

- **Authentication**: [Auth requirements]
- **Authorization**: [Access control rules]
- **Data Protection**: [Sensitive data handling]
- **Input Validation**: [Validation approach]

## 6. Test Strategy

| Level | Scope | Approach |
|-------|-------|----------|
| Unit | [What to unit test] | [Framework/approach] |
| Integration | [What to integration test] | [Framework/approach] |
| E2E | [What to E2E test] | [Framework/approach] |

## 7. Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| [Risk description] | High/Medium/Low | High/Medium/Low | [Mitigation strategy] |

## 8. Open Questions

- [ ] [Unresolved technical question 1]
- [ ] [Unresolved technical question 2]

---

## Appendix: Functional Spec Summary

### Epic: [Title] (#[ID])
[One-paragraph summary of business justification]

**Features:**
- **[Feature Title]** (#[ID]) - [One-line summary]
  - [Story Title] (#[ID])
  - [Story Title] (#[ID])

[Repeat for each Feature]
```

## Version Numbering
- First tech spec for an Epic: v1.0
- Regeneration after functional spec change: increment major version (v2.0, v3.0)
- Minor editorial fixes (typos, clarifications): increment minor version (v1.1, v1.2)

## Regeneration Rules
- When regenerating for an Epic tagged `techspec:stale`:
  1. Fetch the previous wiki page content for reference
  2. Compare the current hierarchy against what was documented
  3. Confirm there is still valid approval evidence on the Epic, or add a refreshed approval comment if needed
  4. Generate a new version, noting what changed
  5. Update the wiki page
  6. Add a new Epic comment noting the version change
  7. Remove `techspec:stale`

## Completion Checks
- Wiki page exists at the expected path with all template sections populated
- Epic is in state `Active`
- Epic has an approval comment
- Epic has a comment linking to the wiki page
- Epic does not have `review:ready`
- Epic does not have `techspec:stale` after generation
- No `[TODO]` or placeholder text remains in the wiki page
