# Agentic Workflows

EdgeFront Builder uses GitHub Copilot agentic workflows to automate routine maintenance tasks. These workflows run on a schedule and use an AI agent to perform work that would otherwise require manual effort.

Workflow definitions live in `.github/workflows/` and follow the `*.md` convention used by the [gh-aw](https://github.com/github/gh-aw) runner. Each workflow has a companion `*.lock.yml` file generated at compile time.

---

## Daily Repo Status

**File:** `.github/workflows/daily-repo-status.md`  
**Schedule:** Daily  
**Output:** GitHub issue labeled `report` / `daily-status`

Scans recent repository activity — merged PRs, opened issues, code changes — and creates a daily status issue. Older issues with the same label prefix (`[repo-status]`) are closed automatically so only the latest report is open at any time.

Useful for maintainers who want a quick summary of what happened in the repo without manually reviewing the activity feed.

---

## Daily Documentation Updater

**File:** `.github/workflows/daily-doc-updater.md`  
**Schedule:** Daily at 06:00 UTC  
**Output:** Pull request labeled `documentation` / `automation`

Scans merged pull requests and commits from the previous 24 hours, identifies user-facing changes that are not yet documented, and opens a draft pull request with the necessary documentation updates.

The workflow:

1. Searches for PRs merged since the previous day.
2. Reviews the diff of each merged PR to identify new or changed features.
3. Checks the `docs/` directory for existing coverage.
4. Edits or creates documentation files to cover the identified gaps.
5. Opens a PR targeting `master` with the changes. The PR is assigned to `copilot` for review.

Pull requests created by this workflow are draft PRs with auto-merge disabled. A maintainer should review and approve before merging.

> **Note:** The workflow skips internal refactors and performance changes that have no user-facing impact.
