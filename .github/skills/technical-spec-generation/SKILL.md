---
name: technical-spec-generation
description: 'Generate technical specifications from approved functional specs (Azure DevOps Epic hierarchies) and publish as wiki pages. Use after functional approval to create implementation-ready technical documentation.'
argument-hint: 'Provide the Epic ID to generate a technical specification for, or ask to review/update an existing tech spec.'
---

# Technical Spec Generation

## When to Use
- Generate tech spec from approved functional spec
- Update tech spec after functional revisions
- Review existing tech spec before implementation

## Prerequisites
- Epic is `Active` + has approval comment + no `review:ready` tag
- Project wiki exists: `edgefront-builder.wiki` (manual one-time setup: Azure DevOps → Project → Wiki → Create project wiki)

## Functional Input

| Type | Canonical fields |
|------|------------------|
| Epic | Description |
| Feature | Description (includes embedded AC) |
| User Story | Description + `Microsoft.VSTS.Common.AcceptanceCriteria` |

## Templates (canonical, user-editable)

| Artifact | Format | Path |
|----------|--------|------|
| Tech spec wiki page | Markdown | `.github\skills\technical-spec-generation\templates\technical-spec.md` |
| Link comment | Markdown | `.github\skills\technical-spec-generation\templates\tech-spec-link-comment.md` |
| Regeneration summary | Markdown | `.github\skills\technical-spec-generation\templates\regeneration-summary-comment.md` |

Rules: load template first; preserve metadata block + heading order; reject output with unresolved placeholders.

## Workflow

1. **Validate preconditions**: Epic `Active`, has approval comment, no `review:ready`. If `techspec:stale`, warn and proceed only on confirmed understanding.
2. **Pull hierarchy**: fetch Epic (ID, title, description, state, tags, comments), child Features (Description), and each Feature's Stories (Description + AC). Build structured hierarchy.
3. **Analyze & design**: identify architecture decisions, components, data model changes, API contracts, external deps, security, test strategy per Feature. Ask clarifying questions for ambiguity. Record risks + open questions.
4. **Generate spec**: fill every section in canonical template from hierarchy + analysis.
5. **Publish**: path `/Tech-Specs/[Epic-ID]-[Slugified-Title]` (e.g. `/Tech-Specs/356-Session-Import-from-CSV`). Use MCP `wiki_create_or_update_page` with `wikiIdentifier: edgefront-builder.wiki`, `project: edgefront-builder`.
6. **Link & clear stale**: add Epic comment with wiki link (link template); remove `techspec:stale`; if regeneration, add second comment (regeneration template).

## Versioning
- First tech spec: v1.0
- Regeneration after functional change: major bump (v2.0, v3.0)
- Editorial fixes: minor bump (v1.1, v1.2)

## Regeneration (`techspec:stale` present)
1. Fetch previous wiki content for reference.
2. Diff current hierarchy vs documented.
3. Confirm valid approval evidence (add refreshed approval comment if needed).
4. Generate new version + note changes.
5. Update wiki page.
6. Add Epic comment noting version change.
7. Remove `techspec:stale`.

## Completion Checks
- Wiki page + comments match current templates
- Wiki page exists at expected path with all sections populated
- Epic `Active` + approval comment + wiki-link comment
- No `review:ready` / `techspec:stale` remaining
- No placeholder text in wiki page
