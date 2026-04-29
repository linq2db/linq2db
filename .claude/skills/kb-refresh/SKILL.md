---
name: kb-refresh
description: Incremental update for the knowledge base under .claude/knowledge-base/. Reads cursors, fetches deltas (commits, GH issues/PRs/discussions, wiki, code), re-runs the relevant indexer agents scoped to the deltas, and runs a random-sample citation audit. Atomically advances cursors. Interruptible at every cursor boundary.
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
- `git fetch origin master` has run recently (the user is responsible — the skill doesn't fetch behind their back).

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
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "get-progress"}
EOF
```

If the file is missing or the result errors, abort with "Run /kb-build first".

### 2. Capture currentSha

```bash
git rev-parse HEAD
```

### 3. Iterate sources

In this order: `code → coverage → commits → issues → prs → discussions → wiki`. Skip any source not in `--source` (if specified).

For each source, follow the per-source procedure below. Cursor advances are written *after* each indexer's `apply-fences` succeeds — partial progress is safe.

#### `code` source

1. Read `cursors.json.code.sha`.
2. If equal to `currentSha`: skip (no code changes).
3. Otherwise: `git diff --name-status <cursor.sha>..HEAD` → changed file list.
4. Map to areas via path-pattern match against `kb-areas.md`. Build `{area: [files...]}` map.
5. For each area:
   - Spawn `kb-architect` with `mode: "delta"`, `area: <code>`, `changedFiles: [...]`, `currentSha`.
   - Capture stdout, write to `.build/.claude/kb-refresh-arch-<area>.txt`, run `apply-fences`.
   - Spawn `kb-issue-detector` with `mode: "delta"`, same inputs plus `existingIndex` and `openGithubIssues`.
   - Apply fences.
6. **Prune deferred queue overlap.** For each `(area, file)` in the delta where `file` is currently in `state/deferred-coverage.json[area].files`, drop it from the queue (the code-source pass just re-scanned it). One `set-deferred-area` call per affected area with the surviving file list.
7. Update `cursors.json.code` to `{sha: currentSha, verified_at: <ISO>}`.

#### `coverage` source

1. Read `state/deferred-coverage.json` via `kb-state.ps1 get-deferred`. If `areas` is empty: skip.
2. Compute the per-area share of the budget. With `B = --coverage-budget` (default 10) and `N` areas with entries, take `ceil(B/N)` files per area in path-sort order, capped by the area's queue size, until `B` is exhausted globally. If `B == 0`, skip the source entirely.
3. For each affected area:
   - Read the existing `areas/<area>/INDEX.md` (path supplied to the agent so it doesn't re-read independently).
   - Spawn `kb-architect` with `mode: "coverage-fill"`, `area: <code>`, `targetFiles: [...]`, `currentSha`, `existingArtifacts: ["areas/<area>/INDEX.md"]`.
   - Capture stdout to `.build/.claude/kb-refresh-coverage-<area>.txt`, run `apply-fences`.
   - The agent's envelope must contain (a) the refreshed `areas/<area>/INDEX.md` artifact and (b) one `=== DEFERRED-COVERAGE-CLEAR: <area> ===` listing every path in `targetFiles`. `apply-fences` removes those from the queue.
4. After all areas processed, re-read the queue and append a one-line summary per area to `audit-log.md` (handled implicitly by `apply-fences` deferred audit entry).

The `coverage` source has **no cursor** — its progress is the queue itself shrinking. The skill stops when the budget is spent or the queue is empty, whichever comes first.

#### `commits` source

1. Read `cursors.json.commits.sha`.
2. `kb-fetch-commits.ps1` with `since: <cursor.sha>`, `until: HEAD`.
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
pwsh -NoProfile -File .claude/scripts/kb-audit-citations.ps1 <<'EOF'
{"k": 5}
EOF
```

For each `verdict: stale` or `verdict: deleted` entry in the result:

1. Demote that file's `confidence` by one step (`high → medium → low`). Use `Edit` on the frontmatter.
2. Append a `## <ISO> — confidence demotion` entry to `audit-log.md` with the file path and `miss_details`.

Demoted files are not auto-rebuilt by this skill — that's a manual call (`/kb-build --force <step>` if needed). The demotion is the signal.

### 5. Append refresh summary

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
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

- **Cursor file corrupt**: `kb-state.ps1` errors out; the skill surfaces the path and suggests `git restore .claude/knowledge-base/state/cursors.json`.
- **Fetch script returns rate-limited**: skill appends to audit log, surfaces reset time, stops cleanly. Cursors not advanced for that source.
- **Indexer emits invalid envelope**: `apply-fences` reports `gateFailures`; skill stops at that source's boundary, surfaces failures, leaves cursor un-advanced.
- **A delta touches a file in a path not classified by any area**: `kb-architect` emits an `=== UNCLASSIFIED-FILE ===`; the skill surfaces it and asks the user to extend `kb-areas.md`.

## Do not

- Modify the `.claude/docs/kb-*.md` reference files. Those are hand-maintained, not generated.
- Advance a cursor past content that wasn't successfully applied. Any partial-write must leave the cursor at the last successful boundary.
- Skip the audit silently. If `--skip-audit` is set, surface that the audit was skipped in the final summary.
- Auto-rebuild stale files. Demotion is the signal; rebuilding is the user's call.
