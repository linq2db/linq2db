## PR context preparation

Common preparation done by `/review-pr` and `/verify-review` before spawning subagents. Both skills reference this doc instead of restating the steps.

### Context load (one call)

Everything the skill needs up front — PR metadata, reviews, review comments, issue comments, closing-issues references, the PR head fetched into `origin/pr/<n>`, diff stat / name-status / commits, and the one-level linked-issue scan — is returned by a single invocation of `.claude/scripts/pr-context.ps1`. Use named parameters (single allowlist-friendly command line, no stdin pipe needed):

```
pwsh -NoProfile -File .claude/scripts/pr-context.ps1 -Pr <n>
```

Optional named parameters:

- `-Pr <int>` — required
- `-Owner <name>` / `-Repo <name|owner/repo>` — defaults `linq2db`/`linq2db`
- `-BaseRef <ref>` — default `origin/master`
- `-NoFetch` — switch; skip the bundled `git fetch` (PR head **and** base branch) when both refs are already current
- `-LinkedConcurrency <int>` — default `6`; parallel fan-out cap when fetching linked issues

Stdin JSON is still accepted for callers that prefer the heredoc form (`pwsh ... pr-context.ps1 <<'EOF' { "pr": <n> } EOF`); see the script header for the JSON schema.

Output is a single JSON object — see the script's header comment for the exact schema. The fields the review skills consume:

| Field | Used for |
|---|---|
| `pr` | title, body, milestone, base/head refs, draft flag, URL |
| `currentUser` | ID-continuation floor (which prior reviews are "yours") |
| `reviews`, `reviewComments`, `issueComments` | prior-finding scan, linked-ref scan |
| `reviewThreads` | databaseId → thread.id map for `/verify-review` step 7 (see below) |
| `closingIssues`, `linkedRefs`, `linkedIssues` | linked-issue context for the subagent briefing |
| `diffStat`, `nameStatus`, `commits` | change summary bullets |
| `headSha`, `headRef`, `baseRef` | passed to subagents and to `post-pr-review.ps1`; **`headSha` is authoritative** — don't re-derive from `git log` / `git rev-parse` |

`reviewThreads[]` has one entry per GraphQL review thread on the PR, shape `{ threadId, isResolved, firstCommentId }`. Resolve a REST `comment_id` to its thread by matching `firstCommentId == comment_id` (the first comment's `databaseId` equals the REST listing's `id`). `/verify-review` uses this to drive the step 7 per-finding action table — no separate `gh api graphql reviewThreads` call is needed.

The skill does **not** need to re-run `gh api` / `git` calls for anything the script already returned. Subsequent reads of file content and hunks go through `.claude/scripts/diff-reader.ps1` (see the code-reviewer spec).

### Change summary

From the script output (`pr.body`, `commits[*].body`, `nameStatus`, `linkedIssues[*].body`) write 3–8 bullets covering:

- **Purpose** — what the PR is for, what it fixes.
- **Areas touched** — which `Source/*` projects, which providers, which tests.
- **Observable impact** — public API changes, generated SQL changes, behavior changes.
- **Anything unusual** the reviewer should know up front.

Ignore `master`-sync merge commits when drafting this — commits matching `^Merge branch 'master'` (and mirror forms like `Merge remote-tracking branch 'origin/master'`) are routine branch maintenance, not the PR's work. The same goes for any PRs those merges transitively absorb: if PR #X was already merged into `master` and a master-sync pulled it into this branch, #X is not part of this PR's scope and must not appear in the change summary, review notes, or any finding. The diff against `origin/master` should already exclude that content; if something from an absorbed PR still surfaces in the diff (e.g. a conflict resolution), review only the conflict-resolution delta, not the absorbed PR's own changes.

This summary is the briefing fed to both subagents so the baselines-reviewer can rationalize why each baseline moved.

### Baselines clone setup

The baselines clone is expected at **`../linq2db.baselines`** (sibling of this repo).

Run `git -C ../linq2db.baselines fetch origin` directly as a single Bash call — do **not** pre-probe with `ls ../linq2db.baselines`. The `ls` is documented as a violation in `.claude/docs/agent-rules.md` → **Permission-friendly Bash patterns** (it's not allowlisted and prompts every time), and the `git fetch` is self-diagnosing:

1. If the clone **exists**: the fetch succeeds (usually silent; possibly a few `origin/baselines/pr_*` updates printed).
2. If the clone **does not exist**: the fetch errors out with `fatal: not a git repository ...`. On that error, stop and ask the user:
   > Baselines clone not found at `../linq2db.baselines`. Clone `https://github.com/linq2db/linq2db.baselines.git` there now? [y/N]

   - On `y`: `git clone https://github.com/linq2db/linq2db.baselines.git ../linq2db.baselines`.
   - On `N`: skip baseline review, proceed with code review only, note the skip in the final review body.

Branch presence is checked by the baselines subagent via `baselines-diff.ps1`, so the skill doesn't need a separate `rev-parse` step — and equally must not pre-probe with `git -C ../linq2db.baselines branch -a --list 'origin/baselines/pr_<n>'` or any other `branch --list` / `rev-parse` / `ls-remote` shape. The script returns `status: "branch_missing"` when the PR produced no baseline changes, which the subagent converts into its `no_baselines` output; the same script is also the one that errors clearly when the *clone* is missing. Pre-probing adds nothing the script doesn't already do and costs a permission prompt per run.

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

`diff-reader.ps1` **creates `writeDir` itself** when it's set — do not pre-run `mkdir -p .build/.claude/pr<n>` from the skill. Every such pre-mkdir call fires its own permission prompt and adds nothing the script doesn't already do.
