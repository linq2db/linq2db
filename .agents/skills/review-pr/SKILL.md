---
name: review-pr
description: Deep professional review of a linq2db PR. Accepts PR link, PR number, a linked issue/task number, or a branch name. Loads PR + comments + linked issues, prepares a change summary, runs a code-correctness pass and a baselines-diff pass in parallel (a tool with a named-subagent facility can delegate each to one), classifies public-API changes against the PR milestone, assembles a severity-ordered finding list, and posts a draft pending review on GitHub after user confirmation. Never commits or edits code.
---

# review-pr

User-triggered workflow to review a PR on `linq2db/linq2db`.

Shared reference material:

- **Review orchestration** (shared skeleton with `/verify-review`): `.agents/docs/review-orchestration.md`
- **Review conventions** (severities, IDs, checkboxes, body structure): `.agents/docs/review-conventions.md`
- **GitHub review API** (endpoints, gotchas, thread-id mapping): `.agents/docs/github-review-api.md`
- **PR context prep** (one-call loader, change summary, baselines clone): `.agents/docs/pr-context-prep.md`
- **Baselines repo layout** (branch naming, file grammar): `.agents/docs/baselines-repo-layout.md`
- **PR reference resolver** (URL / number / issue / branch): `.agents/docs/pr-resolver.md`
- **API surface classification** (milestone-driven note-vs-BLK rules): `.agents/docs/api-surface-classification.md`
- **Review posting** (manifest format + wrapper invocation): `.agents/docs/review-posting.md`

The workflow relies on five PowerShell Core helper scripts to keep the permission surface to one allowlist entry per script. They share a common shape (stdin JSON → stdout JSON, no temp files, no compound commands) — see `.agents/docs/agent-rules.md` → **PowerShell Core scripts for complex operations** for the pattern:

- `.agents/scripts/pr-context.ps1` — fetches PR metadata, reviews, comments, linked issues, diff stat / name-status / commits, `origin/pr/<n>` head, in one call.
- `.agents/scripts/diff-reader.ps1` — batch file content + diff + hunk reader, called by `code-reviewer`.
- `.agents/scripts/verify-lines.ps1` — batch snippet + hunk verification, called by `code-reviewer`.
- `.agents/scripts/baselines-diff.ps1` — one-shot baselines diff + grammar parse, called by `baselines-reviewer`.
- `.agents/scripts/post-pr-review.ps1` — REST review POST + file-thread GraphQL in one process.

## When to run

Only when the user explicitly invokes `/review-pr <ref>`. Reference forms and resolver are defined in `.agents/docs/pr-resolver.md`. Draft PRs are reviewed the same way as ready-for-review PRs.

## Steps

Permission-prompt discipline, PR resolution, context loading, subagent spawning, API classification, posting, and the command-usage audit closing step are defined once in [`review-orchestration.md`](../../docs/review-orchestration.md). This skill layers `initial`-mode specifics on top: a **scope confirmation gate** (step 4 below), a **target-branch warning** (step 3), and the **review-body assembly** (step 8).

### 1. Resolve the target PR

Per `review-orchestration.md` → **Resolving the target PR**.

### 2. Load context, summarize, prepare baselines

Per `review-orchestration.md` → **Loading PR context**.

When the loaded context shows the PR's milestone differs from a linked issue's (or a linked issue has no milestone while the PR does), surface it: run `pwsh -NoProfile -File .agents/scripts/milestone-consistency.ps1 -Action check -Pr <n>` and, if it reports `laggards`, offer `-Action assign -Pr <n>` (propose, then confirm — milestone is metadata but the change is visible). Skip laggards flagged `likelyIntentional` (issue on an earlier/closed milestone — legitimate cross-milestone case). Don't block the review on it.

### 2b. Audit prior reviewer claims (bot + human)

Re-verify prior reviews authored by **other** users (Copilot, Codex, other LLM reviewers, humans) against current PR HEAD — both surfaces:

- every inline thread in `pr-context.ps1`'s `reviewThreads[]` that is **not** `resolvedBy == currentUser`, and
- every discrete claim in the **body** of any `reviews[]` entry whose `user != currentUser` that isn't already covered by one of that review's threads.

Classify each as Fixed / Inaccurate / Still actual at HEAD. Surface every verdict in the review body's `## Prior-review audit` section (step 8); batch thread replies + resolves through `post-pr-thread-replies.ps1` (body-summary claims have no thread to resolve — the audit line is their disposition, and Still-actual ones also feed the regular finding stream).

Procedure (scope, classification rules, playground-verification protocol, thread dispositions, review-quality-signal log, mid-session re-run): [`review-bot-claim-audit.md`](../../docs/review-bot-claim-audit.md).

**`currentUser` already has a *submitted* review on this PR and HEAD advanced since — recommend `/verify-review` before a fresh initial pass.** When `reviews[]` shows an entry by `currentUser` whose `state` is submitted (`CHANGES_REQUESTED` / `COMMENTED` / `APPROVED`, `submitted_at` populated) and `pr.headRefOid` differs from that review's `commit_id`, the author has almost certainly pushed fixes against your findings. Surface this and offer `/verify-review` (re-verify prior findings + audit the fixes + post a follow-up only for what's still actual) as the better-fit tool — a fresh initial `/review-pr` re-derives everything and ignores the fix replies. The user may still choose the fresh pass; if so, set the ID-continuation floor above your prior review's IDs (step 5) so the new findings don't collide. (Surfaced on #5678: prior review submitted with 5 findings, all fixed across six commits; a stale memory note had said it was still "PENDING".)

### 2c. Check the PR's CI check status

The code-review passes reason over source — they **cannot** see that the PR's own new/changed tests fail at runtime. Before spawning reviewers, run `gh pr checks <n> --repo linq2db/linq2db`. For failing legs, pull per-test failures via `.agents/scripts/azp-build-failures.ps1 -BuildId <n>` (build id from the failing check's Azure URL) and check whether the failing tests live in files the PR adds/modifies. A PR whose **own** new/changed test fails CI is a top-priority finding — fold it into the finding stream at BLK/MAJ and brief the relevant code pass with the concrete failures. Distinguish pre-existing/flaky failures (note, don't attribute) from PR-caused ones.

(Surfaced on #5678: a full review — three `code-reviewer` passes + `baselines-reviewer` — reported the new `ProviderSpecificReaderValueTests` as high-quality/non-vacuous, but it failed CI on ~15 provider jobs (Oracle needing `FROM DUAL`, MySQL 5.7 `CAST … AS DOUBLE`, Firebird 2.5/3, DuckDB, PostgreSQL, DB2); the blocker surfaced only when the user flagged "tests fail." The absent baseline folders for exactly those providers were the corroborating fingerprint — see `baselines-reviewer.md` → *Subset baseline coverage can be a failure fingerprint*.)

### 3. Target-branch check

Using `pr.baseRefName` from step 2's context output, if it is not `master`, warn the user:

> This PR targets `<base>`, not `master`. Review anyway? [y/N]

Wait for an explicit `y`. No other guards (no draft-PR guard, no size guard).

### 3b. Verify 3rd-party behavior claims in the linked issue

If the PR scope summary or linked issue body rests on an external-system claim — a DB version feature, SQL-standard requirement, driver behavior — verify it per [`create-issue.md`](../create-issue/SKILL.md) → step 1 sub-point 3 (**Verify any 3rd-party behavior claim against upstream docs**) before spawning the reviewers.

If the claim is wrong, tell the user and stop. The PR likely needs re-scoping (sometimes re-filing under a different issue), not reviewing. Running `code-reviewer` + `baselines-reviewer` on a PR whose premise is false produces findings tuned to the wrong expectation — FB5 → FB6 pivot of 2026-04-22 is the motivating case.

### 3c. Consult the KB for the changed areas

Before spawning the reviewers, orient via the knowledge base (skip silently if `.agents/knowledge-base/` isn't built). Map each changed path from step 2's file list to its area code in [`kb-areas.md`](../../docs/kb-areas.md), then **read the cheap anchors directly** for each touched area:

- `areas/<AREA>/issues.md` / `tech-debt.md` — known issues / debt the diff may hit or should fix
- `areas/<AREA>/decisions.md` — past decisions the change must respect
- `areas/<AREA>/patterns.md` — the area's idioms, to judge whether the diff conforms

Reserve `/kb-ask` for cross-area synthesis. Orientation only — the diff + current source win. See [`agent-rules.md`](../../docs/agent-rules.md) → *Consult the knowledge base*.

### 4. Pre-review confirmation

After the target-branch check passes and the change summary is in hand, ask the user two bundled questions in a single prompt so both answers land in one reply (per `agent-rules.md` → **Batching and user interaction**):

> Before I run the reviewers:
> 1. My read of the scope: `<one–two-sentence summary>`. Confirm? [y / correction / skip]
> 2. Include baselines review (test/SQL baseline diff analysis)? [y / n, default y]

**Question 1 — scope.** Answers:
- `y` — proceed with the stated scope as the confirmed scope.
- A correction — re-state the corrected scope in one sentence back to the user for implicit confirmation (no second prompt), then proceed with the corrected version.
- `skip` — proceed without a confirmed scope (only when the user explicitly opts out).

Carry the confirmed scope forward into the `code-reviewer` briefing (step 6) as an explicit `scope` field. The reviewer uses it to keep findings inside the PR's intent and to push tangential concerns to `out_of_scope_observations[]` instead of `findings[]` (see `.agents/agents/code-reviewer.md` → **Scope discipline**). Without this gate, it's easy to surface findings about pre-existing behavior that the PR doesn't cause and wasn't trying to address.

**Question 2 — baselines opt-out.** Default is include. Answers:
- `y` (or empty) — spawn `baselines-reviewer` in step 6 as usual.
- `n` — skip the `baselines-reviewer` spawn entirely. Step 6 runs `code-reviewer` alone; the `## Baselines` section in step 8 is replaced with a single line `Baselines review skipped per user request.` and none of the per-group rendering applies. Use this when the PR has no baseline changes, or when the user has already reviewed them separately and wants to save a subagent run. Determine "no baseline changes" by checking the `baselines/pr_<n>` branch (see [`baselines-repo-layout.md`](../../docs/baselines-repo-layout.md)), never from the absence of the `linq2dbot` "baselines changed" PR comment — that comment lags CI and is absent while CI is still running (the branch and baselines PR may not exist yet even though the run will produce them). Conversely, a *present* linq2dbot comment doesn't prove baselines still exist — the branch can be created then found empty and closed/pruned. A **CLOSED** tracking baselines PR (`linq2db.baselines#<n>`) plus a missing `baselines/pr_<n>` branch means no baseline changes, regardless of the comment.

### 5. Compute the ID-continuation floor

Per `.agents/docs/review-conventions.md` → **ID-continuation floor**: using `reviews` + `reviewComments` + `currentUser` already loaded in step 2, filter to entries authored by `currentUser`, regex-match IDs across their bodies, compute `max(NNN) + 1` per severity. If none, floor is `1` for every severity. Both subagents and the final assembly need it.

The floor is internal numbering bookkeeping — it steers the IDs you assign, not content for the reader. **Do not mention it in the review body** (not under `## Review notes`, not as a trailing meta line). The reader sees IDs like `MIN001` / `MIN014` directly; they don't need to know what the starting point was.

**Worked example of what this forbids.** A "Prior review continuation" bullet like the following must **never** appear in the body, even as a short one-line note:

> ~~- [x] **Prior review continuation.** IDs MIN001–MIN004 and SUG001 were used in the 2026-04-20 review. This review continues from MIN005 / SUG002.~~

The IDs on the new findings are self-announcing; the floor is not reader content. If you need to communicate *scope* of the prior review (e.g. prior review already covered X area, this one covers Y), say that directly without citing floor numbers.

### 5b. Pre-populate the diff cache (multi-pass only)

When step 6's multi-pass gate triggers (changed file count > 5), the three parallel `code-reviewer` invocations would otherwise each call `diff-reader.ps1` with the same `writeDir`, racing on exclusive-write opens. Pre-populate the cache once from the skill before spawning.

**Clear the `writeDir` directory first** — `diff-reader.ps1` overwrites only the files named in the manifest, so a re-review of a PR whose approach changed since the last run leaves stale files from the superseded commit in the cache (e.g. `*SqlBuilder.cs` overrides from an earlier approach that the current HEAD replaced with `SqlProviderFlags`). Those orphans silently mislead passes — a `Grep` over the cache surfaces code that isn't in the PR, reading as a phantom cache/ref divergence. Delete the writeDir **directory** with the PowerShell tool (`Remove-Item -Recurse -Force .build/.agents/pr<n>`) — not the sibling `pr<n>-*.json` / `pr<n>-*.ps1` manifests, which live one level up — before pre-populating:

```
pwsh -NoProfile -File .agents/scripts/diff-reader.ps1 -ManifestFile .build/.agents/pr<n>-diff-prep.json
```

(Surfaced on PR #5561 — a leftover SqlBuilder-override set from an earlier commit of the same PR triggered a false divergence flag that cost several `git diff --name-only` confirmations.)

Manifest:
```json
{
  "pr": <n>,
  "files": [ ...nameStatus paths from step 2... ],
  "writeDir": ".build/.agents/pr<n>",
  "include": { "content": false, "base": true, "diff": true, "styleScan": true }
}
```

After this returns, every pass reads the cache from disk via `Read` / `Grep` / `Glob` and skips its own initial `diff-reader.ps1` call — record this expectation in each pass's briefing as **"diff cache pre-populated at `writeDir`; do not call diff-reader.ps1 unless a needed file is missing; navigate the cache with `Glob` / `Read` / `Grep` and never `find` / `ls` it — the layout is fixed and documented"**. The agent spec already carries the no-`find`/`ls` rule, but stating it in the briefing is what makes it stick — a pass `find`-ing the cache to enumerate files recurred on PR #5542 despite the spec rule (originally surfaced on #5561).

The pre-pop manifest sets `include.styleScan: true`, so the call's stdout already carries each file's `styleFindings`. Collect the non-empty ones and pass them to the **code-correctness** pass (which owns rule 7 / style) as a `styleFindings` block in its briefing, and add to every pass's briefing **"styleScan is pre-computed — do not re-run diff-reader.ps1 for style"**. Without this, the code-correctness pass re-invokes `diff-reader.ps1` solely to recompute styleScan — a redundant opus-pass shell call (surfaced on PR #5561). **Redirect the prep call's stdout to a file on the *first* invocation** (`pwsh … diff-reader.ps1 -ManifestFile … > .build/.agents/pr<n>-diffprep-out.json`) and parse `styleFindings` from there — for a large PR (100+ files) the inline tool-result stdout truncates, and re-running the whole 122-file prep solely to recapture styleScan is itself the redundant run this note warns against (surfaced on PR #5450 re-review, 2026-06-29).

**Include the briefings' supporting-context files in the prep manifest.** The `files` list must cover not just the files each pass will review, but every changed file the pass briefings name as supporting context — and, when the PR adds query-level API markers or options, their predictable public-surface companions (`LinqExtensions/*.cs`, `Internal/Reflection/Methods.cs`, options / `TranslationModifier`-style types). They are cheap to cache up front; discovering mid-review that one is missing costs a second manifest + `diff-reader.ps1` round-trip (PR #5450 review, 2026-06-12).

For single-pass runs (count ≤ 5), skip this step — the single `code-reviewer` populates the cache itself per the agent's existing flow.

### 6. Spawn the subagents in parallel

Per `review-orchestration.md` → **Spawning the subagents in parallel**. This skill adds `initial`-mode specifics and the **multi-pass gate**.

**Multi-pass gate (initial mode only).** Count changed files (`nameStatus.length` from step 2). When **count > 5**, spawn three `code-reviewer` invocations in parallel — one per focus — to reduce per-pass context pressure and stop rule 4's per-provider fan-out from starving the other rubric categories:

- **Pass A:** `focus: "code-correctness"`, ID window `[floor+0, floor+99]` per severity.
- **Pass B:** `focus: "sql-and-provider"`, ID window `[floor+100, floor+199]` per severity.
- **Pass C:** `focus: "api-and-test"`, ID window `[floor+200, floor+299]` per severity.

When **count ≤ 5**, spawn a single `code-reviewer` with `focus: "all"` and the regular ID-continuation floor from step 5 — multi-pass cost (3× opus invocations) isn't worth it for small PRs.

**Reframe Pass B when the PR has no SQL surface.** Pass B's `sql-and-provider` charter is empty for a PR touching none of `Source/LinqToDB/DataProvider/*`, `SqlProvider/*`, or `SqlQuery/*` (pure infra: cache, GC, threading, options plumbing). Rather than waste the pass, brief it against the PR's actual cross-cutting axis — platform / cross-TFM behavior (feature-flag macros, per-TFM BCL API availability, finalizer / `#if NETFRAMEWORK` guards), threading / concurrency, or GC — keeping the same ID window `[floor+100, floor+199]`. (Surfaced on PR #5681, a QueryCache memory-pressure feature with zero SQL.)

All passes share the same `writeDir: .build/.agents/pr<n>` so the on-disk diff cache is populated once. Each `code-reviewer` briefing carries: `mode: initial`; **confirmed scope** from step 4 (absent only when the user explicitly opted out via `skip`); the assigned `focus`; the per-severity ID window (or the floor for single-pass).

**`baselines-reviewer`:** `mode: initial`. **Skip this spawn entirely** when the user answered `n` to step 4's question 2. When fired, it runs in parallel with the code-reviewer passes — 1, 2, or 4 agents total in one assistant turn.

`/verify-review` always runs single-pass with `focus: "all"` — multi-pass is initial-mode only.

### 6b. Merge multi-pass outputs (initial mode, multi-pass only)

When step 6 spawned three passes, merge their JSON outputs into one before classification:

1. **Concatenate** `findings[]`, `out_of_scope_observations[]`, `api_changes[]`, `callLog[]` across all three passes. Only Pass C's `api_changes[]` should be non-empty — Passes A and B emit `api_changes: []` per the focus contract in `code-reviewer.md`.
2. **Deduplicate findings.** Key on `(file ?? "", line ?? 0, line_end ?? line ?? 0, first 12 words of why lowercased)`. When the same key fires in two passes, keep the higher-severity entry (BLK > MAJ > MIN > SUG > NIT). When severities tie, prefer the entry whose pass owns the rubric category (e.g. a SQL-correctness duplicate keeps Pass B's entry).
3. **Deduplicate out-of-scope observations** on `title`. When the same title fires twice, concatenate the descriptions (newline-separated, deduped paragraphs).
4. **Re-pack IDs.** After dedup, walk the merged `findings[]` in submission order (Pass A → Pass B → Pass C, preserving each pass's internal order) and reassign IDs to a contiguous range per severity starting at the original `floor` from step 5. The reader sees `BLK001`, `BLK002`, `MAJ001`, … with no gaps from unused window slots. Carry the original window-internal id forward as `original_id` on each finding so the command-usage audit (step 10) can reconcile back to the emitting pass.
5. **Tag `callLog[]` entries** with `pass: "A" | "B" | "C"` so the command-usage audit can attribute calls to the originating focus. Concatenate in pass order.

**Partial pass results.** The merge assumes all three passes returned parseable JSON. When a pass returns **nothing** — a session-token limit (the agent's result is empty though it did real work), a crash, or an environment failure like a full temp filesystem (`ENOSPC`) — do **not** blindly re-spawn it: a fresh spawn usually re-hits the same limit. Either resume that agent via `SendMessage` (use its returned agent id) when the failure looks transient, or **reconstruct that focus's findings yourself** from the pre-populated diff cache at `writeDir` plus the inputs you briefed it with, then proceed with the merge from the remaining live passes. State plainly in the step-9 user-facing summary that the pass was reconstructed (not a clean agent run) so the human weighs it accordingly. (Surfaced on PR #5501 — the `api-and-test` pass hit a session limit and returned empty; its API enumeration + test findings were reconstructed from the cache.)

For single-pass runs (count ≤ 5), step 6b is a no-op — the agent's output goes straight into step 7.

### 7. Classify public-API surface changes

Per `review-orchestration.md` → **Classifying public-API surface changes**.

`code-reviewer` already verifies its own line numbers (see its spec's **Line-number verification** section). Trust that output — do not re-run verification here, and in particular do not `git show origin/pr/<n>:path` to spot-check snippets. The subagent's first `diff-reader.ps1` call with `writeDir: .build/.agents/pr<n>` persisted every changed file's full HEAD body, base-ref body, and per-file diff to disk — if parent-skill reasoning ever needs to look at a file, `Read` / `Grep` it directly at the paths listed in `.agents/docs/pr-context-prep.md` → **`writeDir` directory layout**. Do **not** `ls` the directory to discover the shape; the layout is fixed and documented. Re-fetching via `git show ref:path | sed -n` pipes costs a permission prompt each and is forbidden. Post-subagent sanity is limited to: each `line` is a positive integer, `line_end >= line` when present, and `file` points to a path that actually appears in the PR's changed-file list from step 2. Findings that fail those lightweight checks go straight to body-section — no disk caching, no second pass.

### 7b. Out-of-scope disposition gate

**Verify each observation's factual claims against source before dispositioning it.** `code-reviewer` OOS observations are narrative and have proved unreliable — a mechanism/behavior claim can be wrong, and an "already tracked by #N" reference can point at a discussion or a closed item rather than an open tracking issue. Before choosing promote / track / leave for an observation: (a) verify any mechanism/behavior claim against source at PR HEAD — read the cited file, don't trust the summary (same "files win over the reviewer's summary" discipline as the Baselines section); (b) for an "already tracked" claim, confirm #N is an open tracking *issue*, not a Q&A discussion or a closed item (`gh issue view <n>`). Drop or rewrite any observation the source contradicts. (Surfaced on PR #5600: an OOS "Oracle bulk copy bypasses CreateParameter" was false — the SQL-fallback path routes through it and the native path uses no `DbParameter`s; and "#5009 already tracks the bulk-copy gap" was false — #5009 is an unanswered discussion, not an issue.)

When `code-reviewer` returned a non-empty `out_of_scope_observations[]`, decide each one **with the user before assembling the body** — never silently render them as FYI or silently file a tracking issue. Present and disposition the observations **one-by-one** (one prompt per observation: title + one-line description + any reproducible root cause the reviewer noted) — do **not** merge them into a single batched questionnaire unless the user explicitly asks to batch (per `review-orchestration.md` → **interactive mode**, the *never-merge-without-request* rule; the general *ask-ask-do-all* batching rule does not apply to issue review). A **promote**d observation enters the finding flow and thus becomes eligible for the **prove-with-test** / **fix** actions there. Each observation's disposition is one of:

1. **Promote to in-scope finding** — the observation is actually caused by / in scope for this PR. Convert it to a `findings[]` entry: assign the next ID at a severity you propose (state the proposed severity; the user may override), set `file` / `line` when the observation names one, and remove it from `out_of_scope_observations[]`. It then flows through the normal finding pipeline (body-section or line/file comment) in step 8.
2. **Create tracking issue** — real but genuinely out of scope. Invoke [`/create-issue`](../create-issue/SKILL.md) to file it on `linq2db/linq2db` **before** posting the review, then keep the observation in the `## Out-of-scope observations` section with the issue number appended (`— tracked as #<n> · not caused by this PR`).
3. **Leave as-is** — keep as an FYI entry in `## Out-of-scope observations`, unchanged.

**A promoted finding that turns out to describe *intended* behavior is documented, not posted.** When investigation of a promoted observation shows the behavior is correct/intended (it reproduces, but is the desired semantics — not a defect), don't post it as a review finding and don't "fix" it: resolve it by documenting the behavior in code (a comment at the resolution site) and flagging a release-notes entry for the change. This differs from the could-not-reproduce reframe in `agent-rules.md` → *Before coding a fix or feature* (that one can't reproduce; this one reproduces but is by-design). (Surfaced on PR #5659: a promoted multi-table column-resolution ambiguity was confirmed to be the intended "mapping-name match wins" behavior, so it became an in-code comment plus a release-notes entry rather than a posted finding.)

This explicit gate **replaces** the old "file separately when investigation is warranted" auto-heuristic — the choice to promote, track, or leave is always the user's, per observation. When the array is empty, skip this step. Apply the dispositions, then proceed to step 8 with the reshaped `findings[]` and `out_of_scope_observations[]`. **In `interactive` mode this gate is *deferred* into the mode-choice walk** — the OOS observations are walked one-by-one alongside the findings (per `review-orchestration.md` → **interactive mode**), each offered the fuller testable-first action set (prove-with-test+fix / fix) in addition to the promote / track-issue / leave-as-FYI choices above; do not pre-disposition them here in that mode. In `submit-all` / non-interactive mode, disposition them up-front here as described.

### 8. Assemble the review body

Use the body structure defined in `.agents/docs/review-conventions.md` → **Output body structure**. No legend table — reviewers who need abbreviation meanings consult the conventions doc.

Classify each `code-reviewer` finding into one of three review output locations:

| Finding has | Posted as | Shape |
|---|---|---|
| `file` **and** `line` | Line review comment in the review's `comments[]` | `{path, line, side: "RIGHT", start_line?, body}` |
| `file` but no `line` | File-level thread via GraphQL `addPullRequestReviewThread`, posted **after** the REST review create (step 9) — **not** in `comments[]` | n/a in REST bulk POST |
| Neither | Body-section entry under the severity heading | checkbox `[ ]`, `**<ID>** — <title>`, `Why: …`, `Fix: …` |

**A finding's `file` must be in the PR diff to be a line/file comment.** Before placing a finding in the first two rows, verify its `file` appears in step 2's `nameStatus`. `addPullRequestReviewThread` silently returns `thread: null` for a path not in the PR diff (e.g. a finding whose fix site is an unchanged provider `*SqlBuilder.cs` while only the sibling `*DataProvider.cs` is in the PR), so the comment never lands. Route any such finding to the **body-section** (Neither) row instead — decide this here, not after the post fails. See [`review-posting.md`](../../docs/review-posting.md) → *Editing a pending review's body via the API submits it; file threads need an in-diff file*. (Surfaced on PR #5450.)

**No duplication across locations.** Each finding appears in **exactly one** of the three rows above — never in two. In particular, do **not** also render line-level findings as body-section bullets under their severity heading (e.g. a `- [ ] **NIT004** — … (see inline thread)` row when NIT004 is already posted as a line comment). The severity sections in the body are for findings that have no line anchor; populate them only from findings that fall into the "Neither" row. Before writing the body, filter `findings[]` to the "Neither" set, then group by severity — don't iterate the whole list. Empty severity sections are omitted entirely (no `## Minor` heading when every minor is line-level).

**Out-of-scope observations.** If `code-reviewer` returns a non-empty `out_of_scope_observations[]`, render them as a dedicated section near the end of the body, between the body-section findings and the `## Baselines` section:

    ## Out-of-scope observations

    Surfaced during review but fall outside this PR's scope. Not findings on this PR — included as FYI.

    - **<title>** — <description>

Omit the section entirely when the array is empty (after the step-7b gate — promoted observations have already moved to `findings[]`). Do not classify out-of-scope observations by severity and do not convert them to line/file comments — they are not findings. Render each surviving entry per its step-7b disposition: a **tracked** one carries its `— tracked as #<n> · not caused by this PR` suffix; a **leave-as-is** one is the bare `**<title>** — <description>`.

For line/file comments, build the `body` field as plain markdown with the shape below. The leading `<Severity>` is the spelled-out name (`Blocker`, `Major`, `Minor`, `Suggestion`, `Nit`) so a human reader seeing an isolated comment on a file line decodes the ID without context. (Shown as an indented block so the inner suggestion fence renders correctly in this doc — the actual `body` string contains the literal backticks.)

    **<Severity> · <ID>** — <why>

    Fix: <fix>

    ```suggestion
    <replacement code — only when the finding has a concrete `suggestion` value>
    ```

Append the suggestion fence only when `suggestion` is set. GitHub requires the fenced block body to be the exact replacement for the commented-on line range, preserving indentation.

**Suggestion-block audit.** Per `code-reviewer.md` → output rules, every **line-level** finding whose fix is expressible as a textual replacement must carry `suggestion`. Run this audit explicitly as a distinct step before building the manifest — don't fold it into general reasoning, or it will be skipped.

1. Enumerate every line-level finding returned by `code-reviewer` (has both `file` and `line`). Count them.
2. For each finding without `suggestion`, classify as one of:
   - **Structural omission (OK).** Fix affects lines outside the commented range, requires a new method / type / file, moves code across files, spans disjoint spots, or describes a design change the human must apply. Examples from prior runs: "move class to Internal.SqlQuery", "split into a separate PR", "add new method elsewhere".
   - **Textual replacement (not OK — must synthesize).** Single-line rewrite, whitespace or indent fix, blank-line removal (use empty suggestion), column realignment over a range (compute aligned form), XML-doc edit, exception-message change, boolean / field flip, or one option of a multi-option fix that's expressible as a replacement.
3. For every "textual replacement (not OK)" case, synthesize the `suggestion` field yourself from the prose `fix` and the cached HEAD file content under `.build/.agents/pr<n>/<path>` (use `Read` / `Grep` — the file is already on disk from the subagent's first `diff-reader.ps1` call). Only drop to file-level (remove `line`) if you genuinely cannot compute the replacement.
4. Report the audit tally to the user as part of the pre-post summary (step 9) in the form: `audited N line-level findings → K with suggestions, M structural omissions, P synthesized here`. This makes the audit a visible user-facing step, not a silent pass-through.

Do not post a line-level finding with a replaceable fix but no suggestion block.

**Baselines section rendering.** Use the subagent's structured output to compose the `## Baselines` section with these rules:

1. **Section header.** Lead with one sentence citing the baselines review anchor:

       ## Baselines
       Delta: [linq2db.baselines PR #<baselineReview.number>](<baselineReview.url>) (<baselineReview.state>) · [compare view](<baselineCompareUrl>)

   If `baselineReview` is null, drop the PR link and keep the compare link only. If `status == "no_baselines"`, emit `No baseline changes.` and skip the rest. **If the user opted out of baselines review in step 4**, render the section as a single line — `Baselines review skipped per user request.` — and skip every rule below.

2. **Per-group heading.** One `###` heading per `groups[].heading`, optionally followed by the group's `summary`.

3. **Per-subgroup rendering.** One `-` bullet per subgroup, prefixed with `**<reason>** — <subgroup.summary>`. Then render its entries as a nested list:

   | entry `sampleStatus` | Rendering |
   |---|---|
   | `A` (added)    | `- [<test>](<sampleUrl>) — added (<providerCount> providers: <comma list>)` |
   | `M` (modified) | `- [<test>](<sampleUrl>) — modified (<providerCount> providers: …)` followed by a collapsed `<details><summary>sample diff</summary>` block containing the `sampleDiff` inside a ```diff fence. |
   | `D` (deleted)  | `- <test> — deleted (<providerCount> providers: …)` (plain text, no link) |

   Provider lists longer than ~8 items get compacted to `<first 5>, … (N providers total)`. Entry `note` fields go after the parenthetical on the same line.

4. **Cross-provider anomalies** under `### Cross-provider anomalies`, one bullet per entry.

   **Verify each anomaly — and every `changed_suspect` entry — against the actual baseline `.sql` file before surfacing it.** `baselines-reviewer` summarises a 1000+-file diff from memory and *does* misreport: it has fabricated anomalies that don't exist in the files, mislabelled which direction/variant emits a key, and missed whole drift clusters it never mentioned. Its narrative is not authoritative — the files are. For each anomaly / suspect entry, read the real file: `git -C ../linq2db.baselines show origin/baselines/pr_<n>:<samplePath>` (after `git -C ../linq2db.baselines fetch origin baselines/pr_<n>`), and for a "modified" claim `git -C ../linq2db.baselines diff origin/master...origin/baselines/pr_<n> -- '<glob>'`. Drop or rewrite any entry the files contradict; **independently** `git diff --name-status origin/master...origin/baselines/pr_<n>` and eyeball the modified/deleted set for clusters the agent omitted. This mirrors `agent-rules.md` → *the code wins over the description* — here the baseline files win over the reviewer's summary. (Surfaced on PR #5561: a fabricated "redundant emulation key" anomaly disproven against the files, plus a ~30-file Sybase/Informix drift cluster the agent never reported.)

   **Always use the three-dot range (`origin/master...origin/baselines/pr_<n>`), never two-dot (`origin/master origin/baselines/pr_<n>`).** Three-dot diffs against the *merge-base* — the master commit the baselines branch was cut from — which is exactly what `baselines-diff.ps1` (the `baselines-reviewer`'s tool) and GitHub's `master...branch` compare view use, so your manual check matches the reviewer's counts. A two-dot diff compares current-master's tip to the branch tip and therefore folds in **everything master changed after the branch was cut** — most visibly whole-provider baseline sets added or removed on master in the interim — producing **phantom deletions** and **false "modified" clusters** that the PR never touched. (Surfaced on PR #5450: a two-dot diff reported ~4.2k deletions — almost all `ClickHouse.Octonica` baselines added to master after the branch cut — plus a phantom `Issue3260Test` modification, while *missing* a real `DontCloseAfterUse` modification; the three-dot diff matched the reviewer's correct `5338 A / 329 M / 0 D`. Two-dot manual "verification" overturned correct reviewer findings in the wrong direction and that error reached the posted review body.)

   **Ground the headline add/modify/delete counts in your own fresh diff — not the reviewer's totals.** Re-`git -C ../linq2db.baselines fetch origin baselines/pr_<n>`, then count A/M/D yourself from `git diff --name-status origin/master...origin/baselines/pr_<n>` for the body. The `baselines-reviewer`'s `baselines-diff.ps1` snapshot can be **wholesale** stale (not just off on individual anomalies) when CI force-recreates the baselines branch mid-review. (Surfaced on PR #5468: the reviewer reported `3829 A / 1498 M` against a pre-force-push commit while the live three-dot diff was `684 A / 244 M / 0 D`, and its "double-RANK" anomaly was contradicted by the current files — both traced to the branch having been force-updated `26dbac…→27c44e…` after the reviewer's fetch.) **Separate inert churn from substantive changes in the headline.** A large modified count can be dominated by semantically-inert churn — most commonly a provider config-name header rename touching every one of that provider's baselines (the first-line `-- <Config> <ContextName>` comment changes, SQL bodies unchanged). Before reporting the headline, split the count: report the inert header-only total and the substantive (real-SQL) total separately, since the raw modified number wildly overstates the real delta. (Surfaced on PR #5450: of 30,726 modified, ~30,251 were a `PostgreSQL` -> `PostgreSQL13` config-name header rename; only ~475 carried real SQL changes.)

   **A "modified" cluster for test groups the PR doesn't touch usually means the PR branch is behind master — not a regression.** When the modified set is dominated by tests the PR never changes (cross-check the modified baseline test groups against the PR's `nameStatus`: none of the changed source/test files relate to them), suspect a stale branch. The baselines branch is CI-regenerated from the PR's code, so if `master` has since merged SQL-generation changes the PR lacks, the branch emits the *old* SQL and diffs against the *post-fix* master baselines as phantom modifications. Confirm: `git rev-list --count origin/pr/<n>..origin/master` (commits the PR lacks) → inspect them for SQL-gen changes (`git log --stat origin/pr/<n>..origin/master -- Source/`) → tip-to-tip spot-check one file (`git -C ../linq2db.baselines diff origin/master..origin/baselines/pr_<n> -- '<one file>'`: master side = newer/cleaner SQL, PR side = old). Report these in the Baselines section as **not a regression in this PR** (the branch's baselines lag master) and flag that the baselines PR carries the unrelated modifications so it isn't merged as-is — but do **not** phrase it as a "sync with master" instruction (`review-conventions.md` → *Notes vs findings* forbids merge/sync review output). (Surfaced on PR #5605: 108 "modified" baselines across six untouched test groups — `CoalesceColumnSelection_AsFieldInit`, `SequentialAccessTest`, … — were the branch lacking master's #5604 projection-dedup; the 78 added were the PR's real new tests.)

5. **Compression feedback.** Do NOT render `compressionFeedback[]` in the review body — that surfaces separately in step 9 as proposed follow-up improvements. These are also reviewer-generated and have proven unreliable (invalid suggestions against the real normaliser) — sanity-check each against `Get-DiffFingerprint` in `_shared.ps1` before presenting it as actionable.

Entries with empty `sampleUrl` / `samplePath` (rollup entries not tied to a specific pattern) render as plain `- <test> — <providers…>` with no link and no diff block.

### 9. Confirm with user, then post

**Pre-show meta-content scan.** Before showing the user anything, grep the assembled review body **and** every line / file / reply comment body for forbidden meta-tokens. If any match, strip or rewrite the offending fragment and re-check. Do not rely on "I'll remember not to do it" — the rule is already documented twice (`.agents/docs/review-conventions.md` → *Audience*, step 5 above) and still gets violated. Tokens to reject:

- `Prior review continuation`, `continues from`, `ID-continuation`, `continuation floor`, `starting point`, `starting floor`
- `MIN00N`, `SUG00N`, `BLK00N`, `MAJ00N`, `NIT00N` in any phrase that *explains* the numbering (e.g. "IDs MIN001–MIN004 were used in…") — IDs on the new findings themselves are fine; commentary *about* the floor or prior-run IDs is not
- subagent names: `code-reviewer`, `baselines-reviewer`, `verify-lines`, `diff-reader`, `post-pr-review`
- internal paths: `.agents/`, `.build/.agents/`, `writeDir`
- skill names: `/review-pr`, `/verify-review`, `/api-baselines`, `/fix-issue`, etc.

Matches on these tokens are an assembly bug, not a reviewer-style preference — fix the body, don't ask the user to tolerate them.

Then show the user:

- The assembled review body
- Summary counts: N per-line comments, M file-level comments, K body-section findings by severity, O out-of-scope observations, baselines status
- Any `compressionFeedback[]` entries from `baselines-reviewer` — present these as **"Proposed follow-up improvements to `baselines-diff.ps1`'s normaliser"**, one short bullet per entry. These are not part of the review itself; the point is to let the user decide whether to act on them in a separate change after the review is posted.

Then run the **mode-choice gate** defined in [`review-orchestration.md`](../../docs/review-orchestration.md) → **Mode-choice gate**.

- On `submit-all` (default): post the pending review via `post-pr-review.ps1` and run the step-2b thread-disposition bundle through `post-pr-thread-replies.ps1` (the same call that handles Fixed/Inaccurate replies also carries any `{ unresolve: true }` entries for Still-actual threads closed by others).
- On `interactive`: walk every reviewable item (body / line / file findings, out-of-scope observations, baselines anomalies, audited threads) per the orchestration doc's order, with per-item `fix | reject | accept-for-post`. When the item count exceeds 20, propose groupings first (per the orchestration doc). At the end of the walk, post the accumulated `accept-for-post` set as one draft review and run the same thread-disposition bundle.
- On `cancel`: exit without writes.

**Posting mechanics — manifest-script format, invocation, manifest-to-finding mapping, verify semantics, heredoc caveats, and the stdout reporting shape — are defined in [`.agents/docs/review-posting.md`](../../docs/review-posting.md)**. The skill's job here is to supply the per-review content that fills the manifest template.

Per-review content for this skill:

- **Manifest path:** `.build/.agents/pr<n>-manifest.ps1`.
- **`body` here-string:** the assembled review body from step 8, opened by the agentic-review disclaimer and containing the review-notes section, the body-section findings grouped by severity, the `## Out-of-scope observations` section (when non-empty), and the baselines section.
- **`lineComments[]`:** every finding with both `file` and `line`. Rebuild per finding per the line-comment body shape in `.agents/docs/review-conventions.md` → **Output body structure** (so each comment leads with `**<Severity> · <ID>**`). Include a `suggestion` fenced block when the finding has one, per the **Suggestion-block audit** above.
- **`fileComments[]`:** every finding with `file` but no `line`.
- **`replyComments[]`:** empty on initial `/review-pr` runs. Reserved for `/verify-review` follow-ups and for retractions of previously-posted findings (see `.agents/docs/review-posting.md` → **Retracting a posted finding**).

### 10. Offer command-usage audit

Per `review-orchestration.md` → **Command-usage audit (closing step)**.

## Release-notes draft (opt-in)

When the user explicitly requests a release-notes-style summary alongside the review (during scope confirmation in step 4, or after seeing the preview in step 9), dispatch to [`/release-notes`](../release-notes/SKILL.md) → mode `draft <N>` rather than embedding it in the review body. That skill owns the marker-comment format (find-by-marker + idempotent in-place update via `release-notes-draft.ps1`), the omit / include-brief checkboxes, the full (wiki) + brief (release) text, and the `lastSha` change-detection block — so re-runs on the same PR update the one draft comment instead of cluttering the thread.

**Do not produce a release-notes draft by default.** The trigger is the user explicitly asking for one ("create a release-notes draft", "post a user-facing summary", or similar). Decline to fold the draft into the review body itself — release-notes drafts have a different audience (release-notes consumers, not PR reviewers) and a different lifetime, so they belong in their own comment.

When drafting, follow the per-provider claim verification discipline in `agent-rules.md` → **Agent Guardrails** → *Provider behavior claims must be verified against translator code*: provider-specific behavior claims must be checked against the actual translator code at PR HEAD before posting. Wording style for review bodies and comments: `agent-rules.md` → **GitHub content authoring** (full reference in [`github-authoring.md`](../../docs/github-authoring.md) → *Wording discipline*).

## Walk-fix

The legacy "walk-fix pivot" path is now `interactive` mode of the mode-choice gate — see [`review-orchestration.md`](../../docs/review-orchestration.md) → **Mode-choice gate** → **interactive mode** for the full per-item walking contract, including out-of-scope observations and baselines anomalies in the same order.

## Don'ts

- **Do not submit** the review. Omit `event` — this is what creates a PENDING draft.
- Do not edit any source file.
- Do not post individual comments with `POST /pulls/<n>/comments` — always go through the reviews endpoint so all findings land inside one draft.
- Do not continue to posting if the user hasn't explicitly approved.
- Do not flag the repo's column-aligned formatting — see `.agents/docs/code-design.md` → **Column-aligned formatting is intentional**.
- Do not embed a severity legend in the review body; the conventions doc is the single source of truth.
