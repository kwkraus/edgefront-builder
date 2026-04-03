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
