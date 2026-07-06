---
name: kb-refresh
description: Incremental update for the knowledge base under .agents/knowledge-base/. Reads cursors, fetches deltas (commits, GH issues/PRs/discussions, wiki, code), re-runs the relevant indexer agents scoped to the deltas, and runs a random-sample citation audit. Atomically advances cursors. Interruptible at every cursor boundary.
---

# /kb-refresh

User-triggered incremental refresh of the knowledge base. Run this whenever the upstream sources have moved (new commits, new issues, wiki updates) and you want the KB to reflect the current state.

## What this skill does

For each source with new data since its cursor:

1. Fetch the delta via the source's helper script.
2. Determine which areas are affected (path-pattern match against `kb-areas.md` for code; label / linked-files match for GH items).
3. Spawn the relevant indexer agent scoped to `{area, since: cursor}` — produces fenced output.
4. Validate + apply via `kb-state.ps1 apply-fences`.
5. Advance the source cursor atomically.

After all source deltas: random-sample K=5 KB files; run `kb-audit-citations.ps1`; demote `confidence` and queue re-indexing on failed audits. Append a refresh summary to `audit-log.md`.

## Shared reference material

- [`../../docs/kb-refresh-cursors.md`](../../docs/kb-refresh-cursors.md) — cursor format, delta-fetch rules per source, audit sampling rule
- [`../../docs/kb-architecture.md`](../../docs/kb-architecture.md) — KB schema
- [`../../docs/kb-areas.md`](../../docs/kb-areas.md) — area registry (used to map changed files → areas)
- [`../../agents/_shared/kb-protocol.md`](../../agents/_shared/kb-protocol.md) — fenced output contract

## Pre-conditions

- `/kb-build` has reached at least step 0 (state files exist). If not, abort with "Run /kb-build first".
- `git fetch origin master` has run recently (the user is responsible — the skill doesn't fetch behind their back). The `code` and `commits` sources read deltas against `origin/master`, not the local `HEAD`, so a stale `origin/master` ref means the KB will miss recently-merged work. Skill aborts at step 2 if `origin/master` is missing.
- **The working tree must already contain `origin/master`'s tip.** Deltas are computed against `origin/master` (step 2), but the indexer agents (`kb-architect`, `kb-issue-detector`, `kb-historian`, `kb-github-curator`) read source files from the **working tree**. If the checked-out branch is behind or diverged from `origin/master` (e.g. a long-lived `infra/agents-curation` clone that hasn't merged master), agents read **stale on-disk source** for files that changed on master since the branch point — silently producing wrong KB content, or forcing fragile per-file `git show <sha>:<path>` workarounds. The skill enforces this at step 2 and aborts if the guard fails; run from a worktree checked out at `origin/master` instead.

## When to run

Only when the user explicitly invokes `/kb-refresh`. Typical prompts:
- `/kb-refresh` — full delta sweep across all sources.
- `/kb-refresh --source code` — only re-scan the codebase delta.
- `/kb-refresh --source issues` (or `prs` / `discussions` / `wiki` / `commits` / `coverage`) — only one source.
- `/kb-refresh --coverage-budget 20` — drain up to 20 deferred-coverage files this run (default 10). 0 disables the coverage source for this run.
- `/kb-refresh --skip-audit` — skip the random citation audit (faster).

## Steps

### 1. Verify state is initialized

```bash
pwsh -NoProfile -File .agents/scripts/kb-state.ps1 <<'EOF'
{"op": "get-progress"}
EOF
```

If the file is missing or the result errors, abort with "Run /kb-build first".

### 2. Capture currentSha

The KB tracks the upstream `master` branch — not the active local branch. This avoids polluting the KB with feature-branch / WIP commits that haven't landed on master yet.

```bash
git rev-parse origin/master
```

If this errors (e.g. `origin/master` is missing), abort with "Run `git fetch origin master` first" — see the pre-condition.

`currentSha` is used for both the `code` and `commits` source delta-bounds and for every artifact's `last_verified_sha` frontmatter. Do not substitute `HEAD` anywhere downstream.

**Then verify the working tree actually contains that commit** (indexers read on-disk source — see pre-conditions):

```bash
git merge-base --is-ancestor <currentSha> HEAD
```

If this exits non-zero — `currentSha` is **not** an ancestor of `HEAD`, i.e. the checkout is behind or diverged from `origin/master` — **resolve it yourself before spawning any indexer; don't hand it back to the user with a shell command:**

- **If the working tree is clean** (`git status --porcelain` is empty): bring master's source into the current branch with `git merge origin/master --no-edit`. This is the established sync pattern for a long-lived curation branch (e.g. `infra/agents-curation`) — it merges master's `Source/` / `Tests/` in while keeping the branch's own `.agents/knowledge-base/`, so the source-to-index and the KB-to-update both live in this one clone. Re-run the `git merge-base --is-ancestor` guard (it now passes because the merge makes `origin/master` an ancestor of `HEAD`) and proceed. Leave the merge commit **unpushed** — pushing is a separate, explicitly-requested user action.
- **If `git merge` reports conflicts, or the working tree was dirty to begin with:** stop and ask the user how to proceed — do **not** auto-resolve conflicts or silently stash. A worktree checked out at `origin/master` (`git worktree add ../<clone-dir>.kb origin/master`, carrying the KB across per [`worktree.md`](../../docs/worktree.md)) is the fallback when merging into the current branch isn't wanted.

Do **not** work around a failed guard by having agents read `git show <sha>:<path>` instead of the working tree: it is fragile, per-file, and easy to get partially wrong across a large delta (some agents will silently index the stale on-disk copy).

### 3. Iterate sources

In this order: `code → coverage → commits → issues → prs → discussions → wiki`. Skip any source not in `--source` (if specified).

For each source, follow the per-source procedure below. Cursor advances are written *after* each indexer's `apply-fences` succeeds — partial progress is safe.

#### Running indexer agents — capture contract (applies to every source below)

Every "Spawn `<indexer>` … apply fences" step in the per-source procedures runs the same three-beat loop. **Do not deviate:**

1. **Spawn in parallel batches; batch size is a run parameter (default 10).** At the start of a source that maps to multiple areas/units, if the user has not already set a batch size for this run, **ask them how many indexer agents to run in parallel (default 10)** and record the answer for the rest of the session. Then spawn up to that many at a time as **synchronous** (`run_in_background: false`) `Agent` calls in a single message — they run concurrently and each returns a capturable result. Wait for the whole batch, capture + apply each (beats 2–3), then launch the next batch. **Never use background agents** (their result arrives only as an HTML-escaping notification over an empty transcript — uncapturable, beat 2) and **never exceed the agreed batch size** (a 30+-area delta fanned out all at once exhausts the session rate limit). Still surface the total scope up front when a delta maps to many areas so the user can narrow it (`--source`, or an explicit subset) — you just no longer process strictly one-at-a-time. The skill remains *interruptible at every cursor boundary*.
2. **Capture each agent's fenced envelope — from its inline result or persisted tool-result JSON, never from a background notification.** A synchronous agent returns its full envelope with literal `<` / `>` / `&`: small results come back **inline** in the tool result (usable directly — write it verbatim to `.build/.agents/kb-refresh-<agent>-<area>.txt`); large results are **persisted by the harness to `.../tool-results/<id>.json`** (the tool result gives the path). For the persisted case, feed that path through the capture helper (one allowlisted call):
   ```bash
   pwsh -NoProfile -File .agents/scripts/extract-agent-output.ps1 <<'EOF'
   {"sourceJson": "<path to tool-results/<id>.json>", "outFile": ".build/.agents/kb-refresh-<agent>-<area>.txt"}
   EOF
   ```
   It reports `hasEnvelope: true` when the file holds a usable envelope. **Never reconstruct an envelope from a background agent's notification/chat text** — that channel HTML-escapes `<`→`&lt;`, `>`→`&gt;`, `&`→`&amp;`, silently corrupting every `<details>` / `<T>` / `<n>` written into the KB, and the background transcript is empty. Synchronous spawns (beat 1) are what make the inline / persisted-path capture available.
3. **Apply, then advance:** `kb-state.ps1 apply-fences` with `{"op":"apply-fences","agentOutputFile":"<outFile>"}`. Advance the source's cursor only after every unit in that source has applied cleanly.

If `extract-agent-output.ps1` returns `hasEnvelope: false`, or `apply-fences` reports `gateFailures`, **stop at that boundary** — surface it, leave the cursor un-advanced, and do not keep spawning.

##### Parallel-batch operational notes (learned from full sweeps)

When batches actually run, these failure modes recur — handle them proactively:

- **Agents self-persist their envelope; the orchestrator only applies the path.** At batch scale, relaying each envelope back through the orchestrator's context does not scale (a batch of 10 floods it). Instruct each indexer to write its COMPLETE envelope to `.build/.agents/kb-refresh-<agent>-<unit>.txt` and return only a 3–4 line summary. A single large `cat <<'EOF'` heredoc **fails with `ENAMETOOLONG`**, and **apostrophes in a heredoc body break it**, so tell agents to write via many small `cat >>` appends (first `cat >`). Always require the path be **repository-relative** (`.build/.agents/…`) — some agents otherwise write to an absolute `C:\.build\…` or a mangled repo-root literal filename.
- **Normalize + apply the whole batch with [`kb-apply-envelopes.ps1`](../../scripts/kb-apply-envelopes.ps1).** Agents frequently emit the closing marker as `=== END KB-INDEXER OUTPUT v1 ===` (stray `v1`), which `apply-fences` rejects as "envelope not found". The script normalizes that in place, then applies every file matching a `glob` **sequentially**, reporting per-file `ok/patches/gate` — replacing the hand-rolled normalize→apply loop:
  ```bash
  pwsh -NoProfile -File .agents/scripts/kb-apply-envelopes.ps1 <<'EOF'
  {"glob": ".build/.agents/kb-refresh-arch-*.txt"}
  EOF
  ```
- **INDEX-PATCH envelopes must apply sequentially.** issues/prs/discussions patches all target one shared `github/*-index.json`; concurrent apply races and silently loses patches (`kb-apply-envelopes.ps1` is sequential by design). Per-area `ARTIFACT` envelopes (separate `INDEX.md`/`issues.md`) are parallel-safe, but sequential is used uniformly.
- **Give parallel `kb-issue-detector`s distinct DI-ID bases.** Each detector independently picks "next ID after the current global max", so a parallel batch all emits the same `DI-<n>` and overwrites on apply. Pass a distinct `assign IDs starting at DI-<base>` per area in the prompt (leave gaps), or renumber collisions before applying.
- **Advance issues/prs/discussions cursors from the delta's max `updated_at` (ISO).** `kb-fetch-github`'s `next_cursor` is a locale-formatted string (`07/06/2026 14:54:08`) that can break the next `since` parse — compute `max(item.updated_at)` in ISO (`yyyy-MM-ddTHH:mm:ssZ`) and set that instead.
- **Run wide-range GitHub fetches in the background.** `kb-fetch-github` for `prs`/`issues` over a multi-week range exceeds the 2-minute foreground Bash timeout — launch those with `run_in_background`.
- **Sweep for stray outputs before finishing.** Because of the absolute-path issue above, after each batch check `C:\.build\.agents\` (recover any envelope written there into the repo `.build/.agents/`) and remove mangled literal-named files from the repo root before applying / declaring done.

#### `code` source

The code source compares against `origin/master`, not the local `HEAD`. Local feature branches, WIP commits, and unpushed work do not contribute to the KB delta — the KB tracks merged-to-master state only.

1. Read `cursors.json.code.sha`.
2. If equal to `currentSha` (`origin/master` tip from step 2): skip (no new code on master since last refresh).
3. Otherwise: `git diff --name-status <cursor.sha>..origin/master` → changed file list. Do **not** use `HEAD` here — see step 2's note.
4. Map to areas via path-pattern match against `kb-areas.md`. Build `{area: [files...]}` map.
5. For each area, **finish one area before starting the next** — follow the capture contract above (synchronous spawn → `extract-agent-output.ps1` → `apply-fences`); never fan out all areas at once:
   - Spawn `kb-architect` with `mode: "delta"`, `area: <code>`, `changedFiles: [...]`, `currentSha`; capture + apply-fences.
   - Spawn `kb-issue-detector` with `mode: "delta"`, same inputs plus `existingIndex` and `openGithubIssues`; capture + apply-fences.
6. **Prune deferred queue overlap.** For each `(area, file)` in the delta where `file` is currently in `state/deferred-coverage.json[area].files`, drop it from the queue (the code-source pass just re-scanned it). One `set-deferred-area` call per affected area with the surviving file list.
7. Update `cursors.json.code` to `{sha: currentSha, verified_at: <ISO>}`.

#### `coverage` source

1. Read `state/deferred-coverage.json` via `kb-state.ps1 get-deferred`. If `areas` is empty: skip.
2. Compute the per-area share of the budget. With `B = --coverage-budget` (default 10) and `N` areas with entries, take `ceil(B/N)` files per area in path-sort order, capped by the area's queue size, until `B` is exhausted globally. If `B == 0`, skip the source entirely.
3. For each affected area:
   - Read the existing `areas/<area>/INDEX.md` (path supplied to the agent so it doesn't re-read independently).
   - Spawn `kb-architect` with `mode: "coverage-fill"`, `area: <code>`, `targetFiles: [...]`, `currentSha`, `existingArtifacts: ["areas/<area>/INDEX.md"]`.
   - Capture stdout to `.build/.agents/kb-refresh-coverage-<area>.txt`, run `apply-fences`.
   - The agent's envelope must contain (a) the refreshed `areas/<area>/INDEX.md` artifact and (b) one `=== DEFERRED-COVERAGE-CLEAR: <area> ===` listing every path in `targetFiles`. `apply-fences` removes those from the queue.
4. After all areas processed, re-read the queue and append a one-line summary per area to `audit-log.md` (handled implicitly by `apply-fences` deferred audit entry).

The `coverage` source has **no cursor** — its progress is the queue itself shrinking. The skill stops when the budget is spent or the queue is empty, whichever comes first.

#### `commits` source

The commit history mirrored into `history/by-year/*.md` and `history/decisions/*.md` is master-only — feature branches and unreleased work are not indexed.

1. Read `cursors.json.commits.sha`.
2. `kb-fetch-commits.ps1` with `since: <cursor.sha>`, `until: <currentSha>` (the `origin/master` tip from step 2 — do **not** use `HEAD`).
3. If `fetched: 0`: skip.
4. Group commits by year. For each year:
   - Spawn `kb-historian` with `mode: "history-by-year"`, that year's commit list, `currentSha`.
   - Apply fences (this *appends* to the year's existing file — the agent reads the existing artifact via `existingArtifacts` and merges).
5. Spawn `kb-historian` with `mode: "history-decisions"` and the full delta commit list (decision detection over new commits only).
6. Apply fences.
7. Update `cursors.json.commits` to `{sha: <last commit SHA processed>, year_done_through: <last year touched>}`.

#### `issues` / `prs` / `discussions` source (each handled the same way)

1. Read `cursors.json.<source>.updated_at`.
2. `kb-fetch-github.ps1` with `source: <x>`, `since: <cursor>`.
3. If `fetched: 0`: skip.
4. If `status == "rate-limited"`: append to `audit-log.md`, surface the reset time to the user, stop.
5. Spawn `kb-github-curator` with `mode: "github-indexes"`, the fetched data.
6. Apply fences (this emits one INDEX-PATCH per item).
7. Identify affected areas: union of areas referenced by upserted items.
8. For each affected area, spawn `kb-github-curator` with `mode: "github-themes"`, `area: <code>` to regenerate the area's `issues.md` from updated index data.
9. Apply fences.
10. Update `cursors.json.<source>.updated_at` to `next_cursor` from the fetch result.

#### `wiki` source

1. Read `cursors.json.wiki.sha`.
2. `kb-fetch-wiki.ps1` with `since: <cursor>`.
3. If `status == "unreachable"`: append to `audit-log.md`, skip without erroring.
4. If `changed_files` is empty: skip.
5. Spawn `kb-github-curator` with `mode: "wiki-mirror"` and the changed/added/deleted file lists.
6. Apply fences. Note: deleted files need explicit removal — the skill applies `Bash rm` for each entry in `deleted_files` (only inside `github/wiki/`).
7. Update `cursors.json.wiki.sha`.

### 4. Random citation audit

Unless `--skip-audit`:

```bash
pwsh -NoProfile -File .agents/scripts/kb-audit-citations.ps1 <<'EOF'
{"k": 5}
EOF
```

For each `verdict: stale` or `verdict: deleted` entry in the result:

1. Demote that file's `confidence` by one step (`high → medium → low`). Use `Edit` on the frontmatter.
2. Append a `## <ISO> — confidence demotion` entry to `audit-log.md` with the file path and `miss_details`.

Demoted files are not auto-rebuilt by this skill — that's a manual call (`/kb-build --force <step>` if needed). The demotion is the signal.

### 5. Append refresh summary

```bash
pwsh -NoProfile -File .agents/scripts/kb-state.ps1 <<'EOF'
{"op": "append-audit", "event": "kb-refresh", "lines": [
  "code: <delta count> files re-scanned",
  "commits: <fetched> new",
  "issues: <fetched> updated, prs: <N>, discussions: <N>",
  "wiki: <changed>/<added>/<deleted>",
  "audit: <files_audited> sampled, <files_stale> demoted"
]}
EOF
```

### 6. Print summary to user

A compact table:

```
| Source       | Delta | Cursor advanced to        |
|--------------|-------|---------------------------|
| code         | 12    | <sha>                     |
| coverage     | 8 drained / 134 remaining | (no cursor)   |
| commits      | 47    | <sha>                     |
| issues       | 8     | 2026-04-25T...            |
| prs          | 5     | 2026-04-25T...            |
| discussions  | 0     | (unchanged)               |
| wiki         | 2     | <sha>                     |
| audit        | 5/5 sampled, 1 demoted | n/a       |
```

Plus any rate-limit / unreachable warnings collected during the run.

## Failure modes

- **Cursor file corrupt**: `kb-state.ps1` errors out; the skill surfaces the path and suggests `git restore .agents/knowledge-base/state/cursors.json`.
- **Fetch script returns rate-limited**: skill appends to audit log, surfaces reset time, stops cleanly. Cursors not advanced for that source.
- **Indexer emits invalid envelope**: `apply-fences` reports `gateFailures`; skill stops at that source's boundary, surfaces failures, leaves cursor un-advanced.
- **A delta touches a file in a path not classified by any area**: `kb-architect` emits an `=== UNCLASSIFIED-FILE ===`; the skill surfaces it and asks the user to extend `kb-areas.md`.
- **Working tree behind / diverged from `origin/master`**: step 2's `git merge-base --is-ancestor <currentSha> HEAD` guard fails; **self-recover before spawning any agent** — if the tree is clean, `git merge origin/master --no-edit` into the current branch and re-check the guard (leave the merge unpushed); only stop and ask the user if the merge conflicts or the tree was dirty. Do not proceed by reading `git show <sha>:<path>` in the agents — that indexes a mix of correct and stale source.
- **Agent output can't be captured**: `extract-agent-output.ps1` returns `hasEnvelope: false` — usually because the agent was spawned in the **background** (only the HTML-escaped notification exists and the transcript is empty) or the wrong `tool-results/<id>.json` was passed. Stop at that boundary and re-run the agent **synchronously**; never reconstruct the envelope by hand from notification text (it corrupts `<`/`>`/`&`).

## Do not

- Modify the `.agents/docs/kb-*.md` reference files. Those are hand-maintained, not generated.
- Advance a cursor past content that wasn't successfully applied. Any partial-write must leave the cursor at the last successful boundary.
- Skip the audit silently. If `--skip-audit` is set, surface that the audit was skipped in the final summary.
- Auto-rebuild stale files. Demotion is the signal; rebuilding is the user's call.
