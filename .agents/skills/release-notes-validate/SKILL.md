---
name: release-notes-validate
description: Release-notes coverage audit. Cross-references the milestone's merged PRs and closed issues with the release-notes on the linq2db GitHub wiki landing page (`Releases-and-Roadmap.md`). For each milestone item, checks whether its issue number, PR number, or a clear textual reference appears in the notes; mentioning either side of an issue↔PR pair satisfies coverage. The user marks each gap as missing (add to notes) or intentional omission (record locally for this release). The drafting + publishing of notes is `/release-notes`; this skill is the coverage safety check that runs after. Invoked by `/release` step 5 or directly during release prep.
---

# /release-notes-validate

## What this skill is (and isn't)

**Is:** the release-notes coverage check. Builds the milestone-item list from GitHub (closed PRs + closed issues), pairs them via PR `closingIssuesReferences` and PR-body `Fixes #N` / `Closes #N` / `Resolves #N` patterns (shared with [`/release-notes`](../release-notes/SKILL.md) via `release-notes-audit.ps1` + `_shared.ps1`), fetches the wiki landing page, and computes per-pair coverage. Surfaces gaps for user decision: add to notes vs. intentional omission.

**Isn't:**

- Not where release notes are drafted/applied — that's [`/release-notes`](../release-notes/SKILL.md) (per-PR draft → on-merge wiki apply → orphan sweep). This skill runs *after* as the coverage safety check and only validates what's there.
- Not for normal-development changelog tracking — that's the PR body / commit message and is out of scope.
- Doesn't write to the wiki. The user edits notes on GitHub; the skill re-runs to verify.

## When to run

- During release prep as task 5 (called by `/release` orchestrator).
- Standalone anytime to spot-check notes coverage mid-prep.

## Required reading

- [`.agents/docs/release/external-repos.md`](../../docs/release/external-repos.md) → release notes location (`Releases-and-Roadmap.md` on the wiki — the single full-notes page).

## Procedure

### 1. Run the audit

```
pwsh -NoProfile -File .agents/scripts/release-notes-audit.ps1 -Action audit -Milestone <ver>
```

The script:

1. Resolves the milestone via `gh api repos/linq2db/linq2db/milestones`.
2. Lists closed PRs on the milestone — `gh api repos/.../pulls?state=closed&base=master` paginated, plus `--json closingIssuesReferences,body,title,labels` for issue-link extraction. Filters to merged-only.
3. Lists closed issues on the milestone — `gh api repos/.../issues?state=closed&milestone=<n>`. Filters to non-PRs (issues only).
4. Pairs items:
   - For each merged PR, scan `closingIssuesReferences` + body `Fixes/Closes/Resolves #<n>` (case-insensitive) — pair the PR with each referenced issue that's also in the milestone.
   - Issues with no closing PR in scope → standalone issue items.
   - PRs with no linked issue → standalone PR items.
   - One row per logical pair (issue↔PR) or per standalone item.
5. Fetches the wiki landing page `https://raw.githubusercontent.com/wiki/linq2db/linq2db/Releases-and-Roadmap.md` (per-version pages are retired).
6. Computes coverage per row: row is **covered** if either `#<issueNumber>` or `#<prNumber>` appears in the notes text (whole-word, leading `#`). Plain textual references without `#N` aren't matched — the user controls the doc and adding the `#N` reference is the cheapest fix.
7. Writes `.build/.agents/release-<ver>-notes-plan.json` with the table.

### 2. Render the coverage table

Render as a single numbered table:

```
#  Issue   PR      Title                                       In notes?  Suggested
1  #5510   #5511   Fix association resolution for nested ...   ✓ (#5510)  —
2  —       #5495   Add AsQueryable() API                       ✗          mention PR #5495
3  #5499   —       (issue closed without PR — chore?)          ✗          confirm if intentional
4  #5402   #5403   Improve Pomelo MySQL compat                 ✓ (#5403)  —
...
```

Columns:
- `Issue` / `PR`: the pair (or `—` if standalone).
- `Title`: PR title preferred; falls back to issue title if no PR.
- `In notes?`: `✓ (#N)` showing which side matched, or `✗` if neither.
- `Suggested`: for `✗` rows, a brief recommendation (e.g. "mention PR #5495").

Optional filters the user can apply after seeing the table:
- "show only ✗ rows" — surface only gaps.
- "show by label `<label>`" — filter by GitHub label (e.g. only `enhancement`).

### 3. Resolve each gap

Single batched prompt covering all `✗` rows:

> _"For each gap: `missing <n>` (add to notes), `intentional <n>` (record local omission with reason — surface a follow-up prompt), or `skip` (defer to manual review). You can combine: `missing 2,4; intentional 3 (chore, no user impact)`."_

For each `missing` row:
- Skill prints the suggested mention text (one-liner the user could paste into the wiki).
- User edits the wiki manually. After they confirm done, skill re-runs the audit (step 1) for that row and updates the coverage table.

For each `intentional` row:
- Skill records `{issue?, pr?, reason}` into `state.notes.intentionalOmissions[]` by editing `.build/.agents/release-<ver>.json` directly (the orchestrator's state schema carries this list). `release-state.ps1 -Action update` cannot write it — that action addresses `state.tasks` only, and `-Annotation` is a free-text note on a task, not a list append (it also hard-requires `-Status`). To additionally annotate task 5, pass a full call: `release-state.ps1 -Action update -Version <ver> -TaskId 5 -Status in-progress -Annotation '<text>'`.
- Don't pollute the long-term `.agents/docs/release/` docs with per-release omissions — they're release-specific judgment calls.

### 4. Re-run + tick

After all gaps are either `missing → added to notes` or `intentional → recorded`:

1. Re-run step 1 to confirm `✗` count is 0 (ignoring intentional omissions).
2. Tick:
   ```
   pwsh -NoProfile -File .agents/scripts/release-state.ps1 -Action update -Version <ver> -TaskId 5 -Status done -Annotation "<N covered, M intentionally omitted>"
   ```

### Optional — draft early

Release notes drafting may be started early in the prep PR flow as a side-track. When that's the workflow, this skill is run iteratively as the user fills in the wiki. Each run shows shrinking `✗` lists; the final run is the formal validation. Matches `/review-pr`'s release-notes draft pattern.

## Don'ts

- Do **not** auto-edit the wiki. Even with explicit user request, this skill defers to the user's manual wiki edit — the wiki is shared content and edit auditability matters.
- Do **not** require both `#<issue>` and `#<pr>` to be present. Per user rule: "we can list issue fixed, but not PR that fix it - this is ok". Either side is sufficient.
- Do **not** mark a release-only intentional omission in `.agents/docs/release/nuget-package-notes.md` or other accruing docs — those are long-term policy, not per-release judgment.
- Do **not** tick task 5 while any non-intentional `✗` remains. The orchestrator's prep-merge gate depends on task 5 being honest.
