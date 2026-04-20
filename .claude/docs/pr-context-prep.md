## PR context preparation

Common preparation done by `/review-pr` and `/verify-review` before spawning subagents. Both skills reference this doc instead of restating the steps.

### Context load (one call)

Everything the skill needs up front — PR metadata, reviews, review comments, issue comments, closing-issues references, the PR head fetched into `origin/pr/<n>`, diff stat / name-status / commits, and the one-level linked-issue scan — is returned by a single invocation of `.claude/scripts/pr-context.ps1`:

```
pwsh -NoProfile -File .claude/scripts/pr-context.ps1 <<'EOF'
{ "pr": <n> }
EOF
```

Input fields (all optional except `pr`):

- `pr` — integer, required
- `owner` / `repo` — defaults `linq2db`/`linq2db`
- `baseRef` — default `origin/master`
- `fetchHead` — default `true`; pass `false` to skip the `git fetch` when the head ref is already current
- `linkedConcurrency` — default `6`; parallel fan-out cap when fetching linked issues

Output is a single JSON object — see the script's header comment for the exact schema. The fields the review skills consume:

| Field | Used for |
|---|---|
| `pr` | title, body, milestone, base/head refs, draft flag, URL |
| `currentUser` | ID-continuation floor (which prior reviews are "yours") |
| `reviews`, `reviewComments`, `issueComments` | prior-finding scan, linked-ref scan, thread mapping |
| `closingIssues`, `linkedRefs`, `linkedIssues` | linked-issue context for the subagent briefing |
| `diffStat`, `nameStatus`, `commits` | change summary bullets |
| `headSha`, `headRef`, `baseRef` | passed to subagents and to `post-pr-review.ps1` |

The skill does **not** need to re-run `gh api` / `git` calls for anything the script already returned. Subsequent reads of file content and hunks go through `.claude/scripts/diff-reader.ps1` (see the code-reviewer spec).

### Change summary

From the script output (`pr.body`, `commits[*].body`, `nameStatus`, `linkedIssues[*].body`) write 3–8 bullets covering:

- **Purpose** — what the PR is for, what it fixes.
- **Areas touched** — which `Source/*` projects, which providers, which tests.
- **Observable impact** — public API changes, generated SQL changes, behavior changes.
- **Anything unusual** the reviewer should know up front.

This summary is the briefing fed to both subagents so the baselines-reviewer can rationalize why each baseline moved.

### Baselines clone setup

The baselines clone is expected at **`../linq2db.baselines`** (sibling of this repo).

1. If the directory **exists**: `git -C ../linq2db.baselines fetch origin` (single Bash call).
2. If it **does not exist**: stop and ask the user:
   > Baselines clone not found at `../linq2db.baselines`. Clone `https://github.com/linq2db/linq2db.baselines.git` there now? [y/N]

   - On `y`: `git clone https://github.com/linq2db/linq2db.baselines.git ../linq2db.baselines`.
   - On `N`: skip baseline review, proceed with code review only, note the skip in the final review body.

Branch presence is checked by the baselines subagent via `baselines-diff.ps1`, so the skill doesn't need a separate `rev-parse` step. The script returns `status: "branch_missing"` when the PR produced no baseline changes, which the subagent converts into its `no_baselines` output.

Layout and branch-naming conventions for the baselines repo are in `.claude/docs/baselines-repo-layout.md`.

### `writeDir` directory layout

When the parent skill passes `writeDir: .build/.claude/pr<n>` on the first `diff-reader.ps1` call (the recommended setup), the script populates the directory with a fixed, predictable shape. The parent skill can `Read` / `Grep` at these paths directly — **do not `ls` to discover structure**, and do not re-fetch via `git show` pipes:

```
.build/.claude/pr<n>/
  <path>/<file>                    # HEAD body of every changed file, at its original repo-relative path
                                   # e.g. Source/LinqToDB/DataProvider/SqlServer/SqlFn.cs
  _diff/<path>/<file>.diff         # Per-file unified diff (the `<file>.diff` suffix is literal)
  _base/<path>/<file>              # Base-ref body — only written when include.base: true was passed on the
                                   # first diff-reader.ps1 call; omit otherwise
```

Files added by the PR have no HEAD/_base entries they wouldn't otherwise; files deleted by the PR have no top-level HEAD body (only `_diff/` and, if enabled, `_base/`). Files modified by the PR appear in every requested location.

When `Read`-ing a file you know is in the PR, construct the path directly from the `nameStatus` entry's `path` field — no directory discovery is needed.
