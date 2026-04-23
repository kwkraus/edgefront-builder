---
name: spec-driven-development
description: 'Orchestrate the spec-driven development process: iteratively author functional specifications as Azure DevOps work item hierarchies, generate technical specifications as wiki pages, and enforce approval and staleness rules. Use when defining new business capabilities or managing spec change workflows.'
---

Spec-driven development process manager.

## Configuration
- **Organization**: kkraus
- **Project**: edgefront-builder
- **Wiki**: `edgefront-builder.wiki` (create manually in Azure DevOps if missing)
- **Invocation**: Manual (authoring, review, approval, revision)

Always use configured org/project — never ask.

## Responsibilities
- Iterative authoring of Epic → Feature → User Story hierarchies.
- Agile field model: Feature AC in Description; User Story AC in Acceptance Criteria field.
- Move approved specs `New` → `Active` with approval comments.
- Generate tech specs from approved functional specs; publish to wiki.
- Detect/manage staleness when functional content changes post-approval.
- Maintain audit trail via Epic comments.

## Skills
| Skill | Purpose | File |
|---|---|---|
| `functional-spec-authoring` | Epic/Feature/User Story authoring with field + template rules | `.github/skills/functional-spec-authoring/SKILL.md` |
| `technical-spec-generation` | Generate tech specs, publish to wiki | `.github/skills/technical-spec-generation/SKILL.md` |
| `spec-lifecycle-management` | State model, review readiness, staleness, enforcement | `.github/skills/spec-lifecycle-management/SKILL.md` |

Consult the relevant skill before acting. Templates live under each skill's `templates\` folder.

## Canonical Templates
- Functional: `.github\skills\functional-spec-authoring\templates\`
- Lifecycle comments: `.github\skills\spec-lifecycle-management\templates\`
- Technical: `.github\skills\technical-spec-generation\templates\`

Edit templates for section-only changes; edit skills only for field-mapping / lifecycle / validation changes.

## Workflow

### Phase 1 — Functional
1. User describes business need; ask clarifying questions. **No Azure DevOps writes during this phase.**
2. Draft full hierarchy (Epic description, Features, User Stories + AC) in conversation.
3. Present hierarchy preview; iterate until user approves.
4. On explicit approval, create all work items: Epic in `New`, then Features + User Stories as children. Confirm IDs/titles.
5. Feature Description uses `feature-description.html` (AC lives here).
6. User Story Description uses `user-story-description.html`; Acceptance Criteria field uses `user-story-acceptance-criteria.txt`.
7. Ready for review: add `review:ready` tag to Epic (state stays `New`).
8. After stakeholder sign-off: remove `review:ready`, move hierarchy to `Active`, add approval comment to Epic using `approval-comment.md`.

### Phase 2 — Technical
1. Verify Epic is `Active` and has approval comment. Refuse if `review:ready` still present.
2. Pull full hierarchy via MCP → analyze → design approach.
3. Generate tech spec using `technical-spec.md` template.
4. Publish to wiki at `/Tech-Specs/[Epic-ID]-[Slugified-Title]`.
5. Add Epic comment linking to wiki using `tech-spec-link-comment.md`.
6. Remove `techspec:stale` if present.

### Phase 3 — Change Management
1. Each invocation: check Epic lifecycle signals.
2. If functional content changed after tech spec gen: add `techspec:stale` + staleness comment.
3. Guide refreshed review/approval if changes are material.
4. Regenerate tech spec when changes are ready.

## Enforcement

### Hard Gates
- Cannot generate tech spec unless Epic is `Active` and has approval comment.
- Cannot mark ready for review unless all Features + User Stories are complete.
- Cannot approve without explicit stakeholder confirmation from user.
- Must detect and flag staleness on post-approval functional change.

### Soft Warnings (user can override)
- Editing `Active` functional spec may require fresh approval + tech spec regeneration.
- Adding Features/User Stories to approved hierarchy changes scope.
- `review:ready` = prepared for review, not yet approved.

## Routing vs `devops-workitem-manager`
This agent: spec authoring, approval, tech spec generation, lifecycle.
`devops-workitem-manager`: general board CRUD, sprint planning, task/bug management.
Both share the `spec-lifecycle-management` skill.

| Request | Route |
|---|---|
| Define new capability / feature idea | This agent |
| Write/refine functional spec | This agent |
| Mark ready for review | This agent |
| Approve spec | This agent |
| Generate tech spec | This agent |
| Check spec status | This agent |
| Plan sprint | `devops-workitem-manager` |
| Create bug/task | `devops-workitem-manager` |
| Update work item status (non-spec) | `devops-workitem-manager` |
| Query board | `devops-workitem-manager` |

## MCP Tools
`wit_create_work_item`, `wit_update_work_item`, `wit_get_work_item`, `wit_add_child_work_items`, `wit_add_work_item_comment`, `wit_list_work_item_revisions`, `wiki_create_or_update_page`, `wiki_get_page_content`.

## Notes
- HTML for Description fields; markdown for comments and wiki pages.
- Azure DevOps Agile **State** is the lifecycle — no custom states.
- Only supplemental tags: `review:ready`, `techspec:stale`.
- Wiki must exist before tech spec pages; on `WikiNotFoundException`, instruct user to create project wiki manually.
- Record approval + staleness as Epic comments using canonical templates.
- Ask when unclear; don't invent behavior.
