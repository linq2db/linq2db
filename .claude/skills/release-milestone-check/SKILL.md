---
name: release-milestone-check
description: Pre-release milestone audit. Confirms there are no open issues or PRs on the release milestone that would mess with the release if merged after prep, and no leftover open baselines PRs (on `linq2db.baselines`) for milestone-merged source PRs — those auto-generate fresh baselines but stay open after the source PR merges and need closing/merging before the release ships. Tick passes only when both lists are clear. Invoked by `/release` step 3 or directly.
---

# /release-milestone-check

## What this skill is (and isn't)

**Is:** a read-only audit of the release milestone, before the prep PR merges. Surfaces:

1. **Open issues / PRs on the milestone** (anything not closed/merged). Each must be closed, merged, or moved off the milestone — leaving them dangling means a late merge after release prep could land changes that need a new public-API / baselines / release-notes pass.
2. **Open baselines PRs on `linq2db.baselines`** for source PRs that already merged to milestone. The baselines repo auto-generates a PR per source PR; when the source PR merges, the baselines PR isn't auto-merged — it needs deliberate action.

**Isn't:**

- Not a code review. Doesn't read PR bodies / files.
- Not authoritative on "is this PR ready to merge" — only flags presence vs. milestone state.
- Doesn't itself close, merge, or move anything. Surfaces and asks.

## When to run

- During release prep as task 3 (called by `/release` orchestrator).
- Manually whenever the user wants a milestone tidiness check (e.g. before bringing a release PR up).

## Required reading

- [`.claude/docs/pr-and-push.md`](../../docs/pr-and-push.md) → **When follow-up commits rename / move / delete tests, close the existing baselines PR**.

## Procedure

### 1. Run the audit

```
pwsh -NoProfile -File .claude/scripts/release-milestone-audit.ps1 -Milestone <ver> [-PrepPR <n>]
```

The script:

- Looks up the milestone number for `<ver>` via `gh api repos/linq2db/linq2db/milestones?state=open`.
- Lists open issues + open PRs on that milestone via `gh issue list --milestone <ver> --state open`.
- Lists merged PRs on that milestone for use in step 2.
- For each merged PR, checks `linq2db.baselines` for an open PR whose head ref is `baselines/pr_<n>` (or `baselines/pull_<n>`) — those are the auto-generated baselines PRs that linger after their source merges.
- Excludes the release-prep PR itself (the prep PR is on the milestone but is the very thing we're auditing; don't count it as "blocking").

Output: structured JSON with `openIssues[]`, `openPRs[]`, `mergedPRs[]`, `staleBaselinesPRs[]`.

### 2. Render results

Two lists. Show them as numbered tables, one per category. For each row: ID, title, last-update date, link.

**Open milestone items:**
```
#  #5510   issue   Foo bug repro                              opened 2026-05-12   <url>
#  #5520   pr      Fix bar translator                         updated 2026-05-13  <url>
```

**Stale baselines PRs:**
```
#  baselines#420   for source-PR #5510 (merged 2026-05-09)    updated 2026-05-09  <url>
```

### 3. Resolve open items

For each open milestone item, ask the user — single batched prompt:

> _"Each blocks release prep. Choose per row: `close <n>`, `move <n>` (drop the milestone), `merge <n>` (signal the user will merge before release), or `wait` (leave but acknowledge — release can't proceed)."_

User-driven actions only — this skill does not close, edit, or move anything itself. After user replies:
- `close` / `move` / `merge`: orchestrator may dispatch to `/create-issue` for follow-up tracking or to direct user actions on GitHub.
- `wait`: skill records the row as known-blocking and tells user "release can't tick task 3 until this is resolved".

### 4. Resolve stale baselines PRs

For each stale baselines PR, ask:

> _"Merge or close? Merging adds the regenerated baselines to master baselines; closing discards them. For a release, merge is the typical answer — the test results those baselines reflect are now master state."_

User can pick `merge <n>` or `close <n>`. User runs the action themselves:
- Merge: `gh pr merge <baselines-pr> --repo linq2db/linq2db.baselines --merge`
- Close + delete branch: `gh pr close <baselines-pr> --repo linq2db/linq2db.baselines --delete-branch`

### 5. Tick the checklist

If after user actions both lists are empty:

```
pwsh -NoProfile -File .claude/scripts/release-state.ps1 -Action update -Version <ver> -TaskId 3 -Status done -Annotation "<N items resolved>"
```

If anything remains open after the user's intended actions but those actions haven't completed yet (e.g. user said `merge` but hasn't merged): keep task 3 `in-progress`, not `done`. The orchestrator re-runs `/release-milestone-check` on next dispatch to verify.

## Don'ts

- Do **not** close issues / PRs on the user's behalf. Always surface + ask + let the user run the action.
- Do **not** merge baselines PRs on the user's behalf. Same reason — destructive action on a shared repo.
- Do **not** tick task 3 as `done` while anything's open. The `[x]` is the gate to the next phase; faking it produces broken release state.
