## Review orchestration — shared skeleton

Common orchestration reused by `/review-pr` and `/verify-review`. Everything in this doc is skill-agnostic: the mode-specific logic (scope confirmation, prior-findings parsing, per-finding action table, etc.) lives in each skill's own `SKILL.md`. This doc is the single source of truth for the steps that are word-for-word identical between the two skills.

### Permission-prompt discipline

Every Bash call is evaluated against the allowlist in `.agents/settings.local.json`. Pipes, redirects, inline `pwsh -Command`, `cat` / `head` / `tail`, or `ls` on directories whose layout is already documented each fire a prompt. Before writing a helper script to extract data from a JSON file, ask whether `Grep` on the dumped JSON or `Read` on the file would return the same information — the answer is almost always yes. See [`windows-gotchas.md`](windows-gotchas.md) → **Permission-friendly Bash patterns** for the full table.

### Resolving the target PR

Follow [`pr-resolver.md`](pr-resolver.md). The resolver returns the PR **number** only — no standalone `gh pr view` call, because the subsequent context load returns full metadata as part of its main response. If the input branch has no PR:

- `/review-pr`: stop and propose creating one (per `agent-rules.md` → **Pull request rules**).
- `/verify-review`: stop — there's nothing to verify.

### Re-review at an unchanged (or master-merge-only) HEAD already reviewed by `currentUser`

After the context load, if the PR already carries a `reviews[]` entry authored by `currentUser`, check whether the PR's *own* code actually changed since that review **before** spawning reviewers — a blind fresh run re-derives near-identical findings and risks posting duplicate threads for findings already open on the PR. Two triggers:

- **Exact-SHA match** — `headRefOid` equals the prior review's `commit_id`.
- **Master-merge-only advance** — `headRefOid` moved, but the advance is only a `Merge branch 'master'` (or equivalent) with the PR's *own* files unchanged. Confirm by diffing the PR's `nameStatus` files between the prior review's `commit_id` and HEAD (`git diff <commit_id> <headRefOid> -- <the PR's changed paths>`) and discarding entries that are pure master churn (unrelated files, or `PublicAPI.Unshipped.txt` / `Directory.Build.props` lines the merge pulled in). If nothing feature-relevant differs, the reviewed code is unchanged even though the SHA moved — the common case the exact-SHA check misses. (Surfaced on PR #5681: HEAD `6e415ef` was a master-merge over the reviewed `cb31dba2`; the feature code was byte-identical, so an exact-SHA check would not have caught it.)

Offer the user three paths:

- **fresh re-review** — run the full pipeline anyway (the user may want a clean pass).
- **verify open-only** — re-check just the still-open prior findings against HEAD (since the code is unchanged, they'll all confirm "still actual"); cheaper, no duplicate threads.
- **summarize & stop** — report the current open-findings + CI state, no subagents, no new review.

Carry the choice forward (a fresh re-review still walks `initial`-mode normally). This is orientation, not a hard gate — proceed on the user's pick. (Surfaced on PR #5525: "review 5525" targeted a HEAD identical to the prior `currentUser` review's commit with three findings still open.)

**If `currentUser` already has a PENDING draft review on the PR**, a fresh `post-pr-review.ps1` will collide — GitHub allows only one pending review per user per PR. Surface the delivery choice before posting: **replace** (verify the old review is still `PENDING` via `--jq '{state,submitted_at}'`, then `gh api -X DELETE …/reviews/<id>`, then post fresh — carry over any still-relevant observations from the old draft yourself, since the delete drops them), **append** (add only the new findings as `replyComments[]` / comments onto the existing draft), or **report-only** (hand the findings to the user, no GitHub write). (Surfaced on PR #5681: a pre-existing PENDING draft over unchanged code forced a delete-first replace.)

### Loading PR context

One call does all of it:

```
pwsh -NoProfile -File .agents/scripts/pr-context.ps1 -Pr <n>
```

Execute the three sections of [`pr-context-prep.md`](pr-context-prep.md) in order: **Context load** (the one script call), **Change summary**, **Baselines clone setup**. Both skills need all three — draft PRs are no different from ready-for-review PRs.

**Check the PR's build CI result.** After the context load, query CI status on the PR head: `gh pr checks <n> --repo linq2db/linq2db`. If the required Azure Pipelines `build` check is **failing / errored** (not merely pending or running), surface it to the user before spawning reviewers and record it as a `## Review notes` item in the review body — a red build means the diff may not compile, so some findings can be noise and the PR may be mid-flight. Don't block the review on it (draft and in-progress branches legitimately carry red CI); the build state is review-relevant context the human reader should see. Applies to both `/review-pr` and `/verify-review`.

**When the `build` check is red but `test-all` is green, suspect a Release-only failure the code-reviewer passes cannot see** — a Meziantou/Roslyn analyzer error (`MA*` / `RS*`), a banned-API hit, or an API-missing-on-older-TFM break (`net462` / `netstandard2.0`). The `test-all` matrix builds a non-analyzer config, so it stays green while the dedicated Release `build` fails. Don't stop at "build is red": fetch the actual diagnostic with [`azp-build-failures.ps1`](../scripts/azp-build-failures.ps1) (resolve the build ID from `gh pr checks`'s `details_url`; full recipe in [`ci-tests.md`](ci-tests.md) → *Reading failed CI test runs*) — its `buildFailures[]` array carries exactly these compile/analyzer messages when `failedTaskCount` is `0` (a non-test build break). **Do not hand-roll the Azure DevOps timeline API** — the script does the timeline + log fetch + parse in one call. Surface the extracted diagnostic as a finding (BLK when it blocks merge), not just an FYI — the code-reviewer passes won't have caught it, since they neither compile nor run analyzers. (Surfaced on PR #5525: a red `build` with green `test-all` was an `MA0186` analyzer error in a new file; the timeline API was hand-rolled three times before recalling the existing script.)

### Spawning the subagents in parallel

Launch every applicable subagent in a **single assistant turn with parallel Agent tool calls** so they run concurrently. Never sequence them.

- `/verify-review` always spawns two: `code-reviewer` (single-pass, `focus: "all"`) and `baselines-reviewer`.
- `/review-pr` spawns 1, 2, or 4: one or three `code-reviewer` invocations depending on its multi-pass gate (see [`review-pr/SKILL.md`](../skills/review-pr/SKILL.md) step 6), plus `baselines-reviewer` unless the user opted out. All `code-reviewer` invocations share the same `writeDir: .build/.agents/pr<n>` so the diff cache is populated once.

Common fields across both modes, supplied by either skill:

- **`code-reviewer` briefing** (one per pass when multi-pass)
  - PR metadata, linked issues + comments, prior reviews/comments (from the context load). When a prior review carries verbatim content a pass will need — a maintainer-supplied test, exact suggested wording, a guard snippet — paste that review's **full body** into the relevant pass's briefing rather than summarizing it; otherwise the `api-and-test` pass re-fetches it via `gh`, duplicating the context load.
  - Change summary (from the context load).
  - Head ref / base ref (`origin/pr/<n>`, `origin/master`) and the file list from `nameStatus`. The subagent reads content via `.agents/scripts/diff-reader.ps1` — do not paste the diff into the briefing.
  - `writeDir: .build/.agents/pr<n>` — mandatory on the first `diff-reader.ps1` call so full file bodies land on disk for `Read` / `Grep` navigation. **When the session runs in a git worktree, also spell out the absolute worktree-prefixed cache root in the briefing** (e.g. `<worktree-root>/.build/.agents/pr<n>`): subagents that guess the main-repo prefix get permission-denied on `Read`/`Grep`, burn calls rediscovering the path, and in the worst observed case fell back to degraded `Grep`-only access that produced a false finding (PR #5450 review, 2026-06-12).
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

**The primary clone is on its own branch during a review — not the PR branch.** `/review-pr` is read-only and works from `origin/pr/<n>` via the diff cache; it never checks out the PR. So a plain `Grep` / `Read` / `Glob` against the working-tree `Source/` reflects **whatever branch the clone sits on** (often `infra/agents-curation`), giving false negatives for PR-added files (they don't exist there) and stale content for PR-modified ones. To inspect PR code, read the diff cache at `writeDir` — but that holds only the PR's *changed* files. For a repo-wide "is symbol X still referenced anywhere" check (e.g. before confirming a type is safe to delete), the diff cache is insufficient: create the PR-branch worktree first and grep there. To read a *specific* known non-diff file from the PR ref without a worktree (a sibling `.csproj`, `Directory.Build.props`, `Directory.Packages.props`), add its path to a `diff-reader.ps1` manifest with `include.content: true` — the reader resolves it against `origin/pr/<n>`; reserve the worktree for repo-wide "is X referenced anywhere" sweeps. Never trust a primary-working-tree grep as evidence about PR code. (Surfaced on #5711: a working-tree `Grep Source/ Lfu` returned nothing and the file read as "does not exist" because the clone was on `infra/agents-curation`, briefly reading as a contradiction of a confirmed finding.)

### Posting via the wrapper

All posting (initial review, verification follow-up, body PUTs, thread resolves) goes through scripts under `.agents/scripts/`. Mechanics — manifest-script format, invocation, manifest-to-finding mapping, verify semantics, heredoc caveats, and the stdout reporting shape — are defined in [`review-posting.md`](review-posting.md). Each skill supplies only the per-review content that fills the manifest template:

- `/review-pr` → `.build/.agents/pr<n>-manifest.ps1` via `post-pr-review.ps1`.
- `/verify-review` → `.build/.agents/pr<n>-verify-manifest.ps1` via `post-pr-review.ps1`, plus `.agents/scripts/apply-verify-writes.ps1` for prior-review in-place edits.

### Mode-choice gate (initial / verify)

After the preview is shown and the user has seen the assembled review body + counts + any baselines `compressionFeedback`, both skills ask one question:

> How should we proceed?
> 1. **submit-all** (default) — bulk-post the review draft with all findings; bulk-disposition every audited thread from step 2b (reply+resolve for Fixed/Inaccurate; reply+unresolve for Still-actual threads that were resolved by someone other than `currentUser`).
> 2. **interactive** — walk every reviewable item (findings, **out-of-scope observations**, baselines anomalies, audited threads) one-by-one with `prove-with-test+fix | fix | reject | accept-for-post` (out-of-scope observations additionally offer **promote-to-finding / track-issue / leave-as-FYI**). Items accepted for post accumulate into a final draft review at the end.
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
4. Out-of-scope observations (each with the fuller action set below).
5. Baselines anomalies (cross-provider distinctions the `baselines-reviewer` flagged).
6. Still-actual prior-review findings from step 2b — other authors' review **body-summary** claims *and* inline threads that reproduce at HEAD (see the note below).
7. Thread dispositions from step 2b (Fixed/Inaccurate replies; Still-actual unresolve actions).

**Out-of-scope observations are walked here in interactive mode** — the up-front out-of-scope disposition gate (`review-pr` step 7b) is *deferred* into this walk rather than done before the mode choice. Each observation is dispositioned one-by-one with the same testable-first options as findings, plus the three OOS-specific terminal choices: **promote-to-finding** (assign a severity + ID, it joins the accept-for-post set), **track-issue** (invoke `/create-issue`, then keep it as an FYI line with the issue number), or **leave-as-FYI** (unchanged FYI entry). A promoted observation is eligible for **prove-with-test+fix** / **fix** exactly like any other finding. (In `submit-all` / non-interactive mode, step 7b still runs up-front instead.)

**Still-actual prior-review findings are walk items, not just audit lines.** A prior review by another author (a human `CHANGES_REQUESTED`, a bot) whose findings still reproduce at HEAD — whether posted as inline threads or **only in the review body** — enters the walk with the same `prove-with-test+fix | fix | reject | accept-for-post` action set, so the user can resolve them in-session instead of only reading them under `## Prior-review audit`. This is easy to miss for **body-summary** findings, because a human's `CHANGES_REQUESTED` review often has no inline threads (item 6 above exists to catch exactly that). **Do not re-post another author's finding as your own comment** — that duplicates their open review (`github-authoring.md` → *never edit / duplicate others' content*); when a still-actual finding is fixed, its disposition is the pushed commit plus a reply on the author's thread/review, and the audit line flips to `Fixed`. `reject` here means "disagree with the prior reviewer" — record the reasoning as a reply, never a silent drop. (Surfaced on #5703: igor's three still-actual body findings were kept audit-only and fell out of the walk, so the one the user wanted fixed had to be pulled into scope by hand.)

**Verify a code-reviewer OOS observation's control-flow claim before surfacing it.** An observation that asserts a *mechanism* — "path X bypasses Y", "code is dead / a no-op", "flag is never set" — carries the same from-summary imprecision as a baselines anomaly (`review-pr` SKILL step 8 → *verify each anomaly against the actual file*): the subagent's *conclusion* (usually "moot / low-risk") can be right while its *mechanism wording* is wrong. Before presenting such an observation, trace the actual call graph (`Grep`/`Read` the cited sites); present the verified conclusion, not the subagent's mechanism phrasing. The same applies to your own restatement — don't add a stronger claim ("dead", "no-op by design") than the trace supports. (Surfaced on #5600: a "bulk-copy bypasses `CreateParameter`" OOS was traced to bulk copy routing its `DataParameter`s through `CommandInfo`→`CreateParameter` after all — the spotted `SetParameter` call was a value-converter, not command-parameter creation; and an "`IsDbDataTypeExplicit` LINQ leg is dead" framing was corrected to "used by the raw-SQL path, correctly `false` on LINQ". Both were caught only because the user probed the mechanism.)

**Confirm where a `fix` lands when the PR is another author's.** The `fix` / `prove-with-test+fix` actions edit files. When the target PR is authored by someone else — even a **same-repo, non-fork** branch — confirm the destination before editing: their PR branch (worktree checkout + push, which needs its own explicit go-ahead), a local edit for the user to fold in, or a review comment for the author to apply. Don't default to pushing onto their branch. Pushing commits to another author's PR branch isn't covered by the "never edit others' bodies/comments" rule, but it's still an outward action on their work — get the target confirmed. (Surfaced on #5721: the fix target — sdanyliv's `infra/skills-review` — was confirmed with the user before the worktree checkout and push.)

Walk **one finding per prompt**, even when the finding count is small (< 5). **Never merge several findings into a single questionnaire without an explicit user request to do so** — the general *ask-ask-do-all* batching rule in [`agent-rules.md`](agent-rules.md) → **Batching and user interaction** does **not** apply to interactive issue review; default to one-by-one. Batching here defeats the per-item action choice and the per-item severity/scope reconsideration the walk is for. The only sanctioned merge is the >20-item grouping below, and it applies only after the user accepts the proposed grouping. (Repeated correction; reinforced on PR #5657, where batched disposition prompts were redirected to one-by-one each time.)

Per item, **always surface prove-with-test+fix as an offered action** — not just `accept-for-post` / `reject`. For a testable finding (reproducible wrong result, wrong SQL, or throw) offer **prove-with-test+fix**; for a non-testable one (a design / style / doc / naming point with no reproducible runtime symptom) offer plain **fix**. **prove-with-test** and **fix** may be chosen together (see *Sequencing* below); **reject** / **accept-for-post** are terminal:

- **prove-with-test** — when the finding is testable (a reproducible wrong result, wrong SQL, or throw), write a regression test that reproduces it on a worktree branched from PR HEAD and run it to confirm it goes **red** against PR HEAD. Run the PR's own / newly-added tests **yourself, not via `test-runner`** (see [`worktree.md`](worktree.md) → *Running tests from a worktree*). The red test is the empirical proof the finding is real, per [`agent-rules.md`](agent-rules.md) → *Before coding a fix or feature* (define the red regression test first). **If the test cannot reproduce the finding, do not fix on speculation** — reframe it as a "could not reproduce" FYI to the author with the repro details and a test pinning the current (correct) behaviour, per the same rule. **When the prove-with-test is for an *out-of-scope* observation, attribute it before acting: run the same repro on a fresh `origin/master` worktree.** An identical failure on master proves the defect is *pre-existing* — not introduced or worsened by this PR — so it is a tracking issue (cite "verified failing identically on master" in the issue and the review FYI), not a PR finding to fix here. Only a failure that reproduces on PR HEAD but **not** on master is a regression the PR caused. (Surfaced on PR #5680: a Convert-wrapped-branch recursive-CTE column drop reproduced on both PR HEAD and master → filed #5683 as pre-existing rather than blocking the PR.)
- **fix** — apply the suggested fix on a worktree branched from the PR HEAD (per [`agent-rules.md`](agent-rules.md) → *Creating a new branch* + [`worktree.md`](worktree.md)). For PRs the current user owns or fork PRs with `maintainerCanModify: true`, commit per concern (no push yet — see test-batching rule below); after the walk completes, push the accumulated commits and append a `## Follow-up commit(s)` subsection to the PR body per `agent-rules.md` → *Push to remote rules*. Otherwise stop and propose filing a follow-up issue instead. Commits are grouped per concern, not bundled into one giant fix commit.
- **reject** — drop from the draft. Record the rejection reason inline so a future `/verify-review` can see it; never silently drop.
- **accept-for-post** — keep for the final draft review accumulated at the end of the walk.

**Sequencing when both prove-with-test and fix are chosen.** Run **prove-with-test first**, and proceed to **fix only after the test reproduces the issue** (goes red against PR HEAD). The failing test is what confirms the fix targets a real defect; the fix then flips it green. If prove-with-test fails to reproduce, the fix is **not** executed — the finding becomes a could-not-reproduce FYI instead (per the reframe rule in the prove-with-test action above).

**Test batching during interactive walks.** Do not build or run tests after each individual `fix` action during the walk — the per-fix build cycle dominates wall time (≈3 min/build on this repo) and serializes the walk needlessly. Default flow:

1. Apply each fix → commit per concern (no build, no test, no push).
2. After the walk completes, before pushing or finalizing the draft review, run **one batched `dotnet build` + filtered test pass** on the worktree covering every fix applied during the session.
3. Push only after the batched build + tests pass. If they fail, identify the breaking change(s) and propose a fix commit before pushing.

Exception — when the user says "run tests now" / "build now" / similar mid-walk, build + run the filtered tests for the in-progress fix immediately, report results, then resume the walk on the user's direction.

At the end of the walk, run the thread-disposition bundle **first** (`post-pr-thread-replies.ps1`), then post the accumulated `accept-for-post` set as one draft review — the same one-pending-review ordering constraint as `submit-all` above (a standalone thread reply 422s while a fresh draft is pending).

When every finding was dispositioned `fix` (the `accept-for-post` set is empty), **no draft review is posted** — the pushed commits plus the PR-body `## Follow-up commit(s)` subsection are the entire outcome. Still run the thread-disposition bundle if any prior-review threads were audited; skip the empty draft-review POST.

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
