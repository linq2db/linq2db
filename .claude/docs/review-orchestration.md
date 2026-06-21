## Review orchestration — shared skeleton

Common orchestration reused by `/review-pr` and `/verify-review`. Everything in this doc is skill-agnostic: the mode-specific logic (scope confirmation, prior-findings parsing, per-finding action table, etc.) lives in each skill's own `SKILL.md`. This doc is the single source of truth for the steps that are word-for-word identical between the two skills.

### Permission-prompt discipline

Every Bash call is evaluated against the allowlist in `.claude/settings.local.json`. Pipes, redirects, inline `pwsh -Command`, `cat` / `head` / `tail`, or `ls` on directories whose layout is already documented each fire a prompt. Before writing a helper script to extract data from a JSON file, ask whether `Grep` on the dumped JSON or `Read` on the file would return the same information — the answer is almost always yes. See [`agent-rules.md`](agent-rules.md) → **Permission-friendly Bash patterns** for the full table.

### Resolving the target PR

Follow [`pr-resolver.md`](pr-resolver.md). The resolver returns the PR **number** only — no standalone `gh pr view` call, because the subsequent context load returns full metadata as part of its main response. If the input branch has no PR:

- `/review-pr`: stop and propose creating one (per `agent-rules.md` → **Pull request rules**).
- `/verify-review`: stop — there's nothing to verify.

### Loading PR context

One call does all of it:

```
pwsh -NoProfile -File .claude/scripts/pr-context.ps1 -Pr <n>
```

Execute the three sections of [`pr-context-prep.md`](pr-context-prep.md) in order: **Context load** (the one script call), **Change summary**, **Baselines clone setup**. Both skills need all three — draft PRs are no different from ready-for-review PRs.

### Spawning the subagents in parallel

Launch every applicable subagent in a **single assistant turn with parallel Agent tool calls** so they run concurrently. Never sequence them.

- `/verify-review` always spawns two: `code-reviewer` (single-pass, `focus: "all"`) and `baselines-reviewer`.
- `/review-pr` spawns 1, 2, or 4: one or three `code-reviewer` invocations depending on its multi-pass gate (see [`review-pr/SKILL.md`](../skills/review-pr/SKILL.md) step 6), plus `baselines-reviewer` unless the user opted out. All `code-reviewer` invocations share the same `writeDir: .build/.claude/pr<n>` so the diff cache is populated once.

Common fields across both modes, supplied by either skill:

- **`code-reviewer` briefing** (one per pass when multi-pass)
  - PR metadata, linked issues + comments, prior reviews/comments (from the context load). When a prior review carries verbatim content a pass will need — a maintainer-supplied test, exact suggested wording, a guard snippet — paste that review's **full body** into the relevant pass's briefing rather than summarizing it; otherwise the `api-and-test` pass re-fetches it via `gh`, duplicating the context load.
  - Change summary (from the context load).
  - Head ref / base ref (`origin/pr/<n>`, `origin/master`) and the file list from `nameStatus`. The subagent reads content via `.claude/scripts/diff-reader.ps1` — do not paste the diff into the briefing.
  - `writeDir: .build/.claude/pr<n>` — mandatory on the first `diff-reader.ps1` call so full file bodies land on disk for `Read` / `Grep` navigation.
  - `focus` — `"all"` for single-pass / verify-mode runs; one of `"code-correctness"` / `"sql-and-provider"` / `"api-and-test"` per pass in multi-pass mode.
  - ID-continuation floor per severity (see [`review-conventions.md`](review-conventions.md) → **ID-continuation floor**), or a disjoint ID **window** `[floor, ceiling]` per severity for each multi-pass pass.
- **`baselines-reviewer` briefing**
  - PR number and head branch.
  - Baselines clone path: `../linq2db.baselines`.
  - Baselines branch: `baselines/pr_<n>`.
  - Change summary.

Mode-specific additions — `scope` for `initial`, `prior_findings` for `verify` — are the only per-skill differences. Each skill adds its own `mode: initial` or `mode: verify` field.

**Briefing-hypothesis discipline.** When a briefing raises a specific concern for a subagent to check, distinguish "**verify whether** X holds" from "**X is likely a bug — investigate rigorously**." The second framing drives the subagent to over-invest (e.g. an out-of-repo `dotnet run` compile, an extra `verify-lines` round) chasing a concern that may be unfounded. If the concern is a cheaply-checkable language / library / framework rule — C# escape semantics, a BCL method's documented behavior, an operator's precedence — verify it yourself before planting it as a likely-bug in the briefing; pass it as a neutral "confirm X" at most. (Surfaced on PR #5544: a briefing framed C#'s variable-length `\x` hex escape as a "real correctness bug to investigate rigorously"; the per-pass agent burned a compile to confirm it was a non-issue.)

**Multi-pass consensus is not verification.** When several `code-reviewer` passes flag the *same* finding, that is not independent confirmation if they reason from the same incomplete evidence — multi-pass agreement amplifies a shared blind spot rather than cross-checking it. Before **acting** on such a finding (applying a fix in `interactive` mode, or posting it at BLK/MAJ) when its resolution requires real code change, independently verify its **load-bearing technical claim** against the actual call site / source, not just the line numbers. (Line-number verification — trusted per `review-pr` step 7 — is cheap positional data; this is about the *claim*, e.g. "this method info is the one the call site emits".) Surfaced on PR #5627: all three passes flagged an "async path unhandled" MAJ from the existence of an `*Async` method info; checking the call site showed the async extension builds the *sync* info, so the finding was a shared false positive — dropped before posting.

### Classifying public-API surface changes

Apply the decision tree in [`api-surface-classification.md`](api-surface-classification.md) to the `api_changes` returned by `code-reviewer`, using the PR's milestone title and file list from the context load. Produces the deduplicated refresh note and any milestone-gated findings. Both skills run this against **fresh** `api_changes` — never reuse classification from an earlier cycle.

Compute the `suppressions_updated` flag by filtering the in-memory `nameStatus` array for entries matching `Source/**/CompatibilitySuppressions.xml`. Do **not** re-run `git diff --name-only | grep` — the data is already in hand and the pipe would prompt on the allowlist.

### Posting via the wrapper

All posting (initial review, verification follow-up, body PUTs, thread resolves) goes through scripts under `.claude/scripts/`. Mechanics — manifest-script format, invocation, manifest-to-finding mapping, verify semantics, heredoc caveats, and the stdout reporting shape — are defined in [`review-posting.md`](review-posting.md). Each skill supplies only the per-review content that fills the manifest template:

- `/review-pr` → `.build/.claude/pr<n>-manifest.ps1` via `post-pr-review.ps1`.
- `/verify-review` → `.build/.claude/pr<n>-verify-manifest.ps1` via `post-pr-review.ps1`, plus `.claude/scripts/apply-verify-writes.ps1` for prior-review in-place edits.

### Mode-choice gate (initial / verify)

After the preview is shown and the user has seen the assembled review body + counts + any baselines `compressionFeedback`, both skills ask one question:

> How should we proceed?
> 1. **submit-all** (default) — bulk-post the review draft with all findings; bulk-disposition every audited thread from step 2b (reply+resolve for Fixed/Inaccurate; reply+unresolve for Still-actual threads that were resolved by someone other than `currentUser`).
> 2. **interactive** — walk every reviewable item (findings, out-of-scope observations, baselines anomalies, audited threads) one-by-one with `fix | reject | accept-for-post`. Items accepted for post accumulate into a final draft review at the end.
> 3. **cancel** — abort; no writes.

Wait for explicit choice — do not assume submit-all on silence.

#### submit-all mode

One bundle of writes, single allowlist prompt per script. **Order matters — post the thread dispositions *before* creating the draft review:**

- `post-pr-thread-replies.ps1` — **run this first.** Every entry from the step-2b disposition table in one call:
  - `{ resolve: true }` for Fixed and Inaccurate verdicts.
  - `{ unresolve: true }` for Still-actual + resolved-by-other-user (reopen the thread with an explanatory reply).
- `post-pr-review.ps1` — the full manifest (body + lineComments + fileComments + replyComments).
- `/verify-review` only: `apply-verify-writes.ps1` for prior-review in-place edits (body PUTs + comment PATCHes + resolve mutations).

**Why this order.** `post-pr-thread-replies.ps1` posts each reply via REST `POST …/comments/{id}/replies`, and `post-pr-review.ps1` creates a **PENDING** draft review. GitHub allows only **one pending review per user per PR**, so a standalone thread reply attempted *while a fresh draft is pending* fails with HTTP 422 `user_id can only have one pending review per pull request` (observed on PR #5558, 2026-05-30 — the reply 422'd, leaving the thread untouched; it succeeded on re-run after the draft was submitted). Thread dispositions are public, immediate actions independent of the draft, so posting them first sidesteps the collision. If the draft has *already* been created this run, don't retry-loop the thread replies — either post them after the user submits the draft, or fold the reply into the review manifest's `replyComments[]` (scoped to the pending review via GraphQL, so it doesn't collide).

The review itself remains a **PENDING draft** (`event` omitted on the REST review create) per each skill's `Don'ts`. `submit-all` controls walking style, not draft-vs-submit — the user submits the draft manually after inspection.

#### interactive mode

Walk every reviewable item in this order:

1. Body-section findings (severity order: BLK → MAJ → MIN → SUG → NIT).
2. Line-level findings (file order, line order within file).
3. File-level findings.
4. Out-of-scope observations.
5. Baselines anomalies (cross-provider distinctions the `baselines-reviewer` flagged).
6. Thread dispositions from step 2b (Fixed/Inaccurate replies; Still-actual unresolve actions).

Per item, offer three actions:

- **fix** — apply the suggested fix on a worktree branched from the PR HEAD (per [`agent-rules.md`](agent-rules.md) → *Creating a new branch* + [`worktree.md`](worktree.md)). For PRs the current user owns or fork PRs with `maintainerCanModify: true`, commit per concern (no push yet — see test-batching rule below); after the walk completes, push the accumulated commits and append a `## Follow-up commit(s)` subsection to the PR body per `agent-rules.md` → *Push to remote rules*. Otherwise stop and propose filing a follow-up issue instead. Commits are grouped per concern, not bundled into one giant fix commit.
- **reject** — drop from the draft. Record the rejection reason inline so a future `/verify-review` can see it; never silently drop.
- **accept-for-post** — keep for the final draft review accumulated at the end of the walk.

**Test batching during interactive walks.** Do not build or run tests after each individual `fix` action during the walk — the per-fix build cycle dominates wall time (≈3 min/build on this repo) and serializes the walk needlessly. Default flow:

1. Apply each fix → commit per concern (no build, no test, no push).
2. After the walk completes, before pushing or finalizing the draft review, run **one batched `dotnet build` + filtered test pass** on the worktree covering every fix applied during the session.
3. Push only after the batched build + tests pass. If they fail, identify the breaking change(s) and propose a fix commit before pushing.

Exception — when the user says "run tests now" / "build now" / similar mid-walk, build + run the filtered tests for the in-progress fix immediately, report results, then resume the walk on the user's direction.

At the end of the walk, run the thread-disposition bundle **first** (`post-pr-thread-replies.ps1`), then post the accumulated `accept-for-post` set as one draft review — the same one-pending-review ordering constraint as `submit-all` above (a standalone thread reply 422s while a fresh draft is pending).

**Grouping for high item counts (>20).** Before walking, compute clusters on the most-discriminating axis: by file path, by severity, or by shared wording (first 12 words of `why` lowercased — the same dedup key the multi-pass merge uses). Propose the most-clustered axis as a grouping: each group is dispositioned in one step (group-level `fix | reject | accept-for-post` applies to every item in the cluster). The user can accept the group disposition or expand a group to per-item walking. Single-item clusters are flattened back to per-item walking automatically — never wrap one finding in a "group of 1" prompt.

### Command-usage audit (closing step)

After the draft review (and, for `/verify-review`, its in-place edits) have been reported, ask the user (single prompt):

> Run a command-usage audit for this session? Identifies unnecessary/duplicate commands, opportunities to fold calls into existing scripts, and allowlist/guardrail gaps. [y/N]

On `y`: walk back through the Bash / `gh` / `git` / `pwsh` calls the skill issued in this session. Both `code-reviewer` and `baselines-reviewer` return `callLog[]` — include their entries too, tagged with the subagent name. For each call, classify as:

- **Necessary** — no-op, leave as-is.
- **Redundant** — already covered by a prior call's output or an existing script's output; recommend removing.
- **Batchable** — multiple calls with the same shape that could fold into a single manifest-driven script call; recommend the new / extended script.
- **Guardrail gap** — a call that should have been blocked by `agent-rules.md` or the allowlist but wasn't; recommend the guardrail update.

Report as a table plus a prioritised follow-up list. Do **not** implement fixes in this turn — propose, then wait for a second explicit go-ahead. Multi-file edits to skills / scripts / docs are not something to batch into a review run.

On `N` (or silent): end without further action.
