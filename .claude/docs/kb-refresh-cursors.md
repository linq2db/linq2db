# KB refresh + cursors

Reference for `/kb-refresh`. Defines the cursor format per source, delta-fetch rules, and the random-sample citation audit.

## Cursors

All cursors live in one file: `.claude/knowledge-base/state/cursors.json`. Single-file is intentional — atomic writes, no partial-update windows.

```json
{
  "schema": 1,
  "code":        {"sha": "<HEAD-sha-at-last-refresh>", "verified_at": "<ISO>"},
  "commits":     {"sha": "<last-indexed-commit>",       "year_done_through": 2025},
  "issues":      {"updated_at": "<ISO>"},
  "prs":         {"updated_at": "<ISO>"},
  "discussions": {"updated_at": "<ISO>"},
  "wiki":        {"sha": "<wiki-HEAD-sha>"}
}
```

`code.sha` is the linq2db `master` HEAD that was used for the last code-scan pass. `commits.sha` is the most recently *indexed* commit (may equal `code.sha` after a refresh, may lag if commits are processed in batches). `wiki.sha` is the linq2db.wiki repo HEAD.

Cursors advance only after the artifact write completes successfully. An interrupted refresh is safe to re-run from any cursor.

## Delta-fetch rules

### `code`

```
git diff --name-status <cursor.code.sha>..HEAD
```

Affected files → resolve to areas via path-pattern match against `kb-areas.md` → spawn `kb-architect` (and `kb-issue-detector` for the same areas) scoped to those areas.

If `cursor.code.sha == HEAD`: skip.

### `commits`

```
git log <cursor.commits.sha>..HEAD --pretty=format:'%H|%aI|%an|%s' --name-only
```

Append entries to the current year's `history/by-year/<year>.md`. Apply decision heuristics; new decisions → `history/decisions/<slug>.md`. Update `cursor.commits.year_done_through` if the year boundary moved.

### `issues` / `prs` / `discussions`

```
gh api -X GET 'repos/linq2db/linq2db/issues' \
  -f state=all -f sort=updated -f direction=asc -f since=<cursor.updated_at>
```

(Same shape for PRs via `gh api repos/.../pulls` and discussions via GraphQL.)

For each item:
1. Upsert into the corresponding `*-index.json`.
2. If `area` changed (path-pattern match against `linked_files[]` or extracted from labels), re-extract themes for affected areas.

Cursor advances per page consumed.

### `wiki`

```
git -C .build/.claude/kb-wiki fetch origin master
git -C .build/.claude/kb-wiki log --name-only <cursor.wiki.sha>..origin/master
```

Changed `.md` files → re-mirror into `github/wiki/`.

If the wiki repo isn't cloned yet, `kb-fetch-wiki.ps1` clones it on first run.

## Random-sample citation audit

Goal: detect when a KB file's cited code line has changed under it, and demote `confidence` accordingly.

### Procedure

1. List all KB files under `architecture/`, `conventions/`, `history/decisions/`, `areas/<area>/`.
2. Random-sample K=5 files.
3. For each sampled file, parse `Source/...:NNN` and `Tests/...:NNN` patterns from the body (regex: `\b(Source|Tests|Build)/[\w./-]+\.(?:cs|csproj|md|json|ps1):(\d+)\b`).
4. For each citation, run `kb-audit-citations.ps1` to verify the line still exists and (loosely) still refers to the cited construct.
5. If ≥ 1 citation in a sampled file fails, demote `confidence` by one step (`high → medium → low`); set `last_verified` to today; append a `## <ISO> — confidence demotion` entry to `audit-log.md` with the file path and which citations failed.
6. Demoted files are queued for re-indexing on the *next* `/kb-refresh` (the current refresh does not auto-rebuild them — that's a manual decision).

### Verification semantics

`kb-audit-citations.ps1` does a structural check:

- `Read` the cited file at the cited line ± 3 lines.
- Tokenize the original cited construct from the KB file body if possible (e.g. the citation appears as `BasicSqlBuilder.cs:1842` near the word `BuildExpression`).
- Match: line still exists AND token still appears in ±3 line window → **hit**. Otherwise → **miss**.

This is intentionally loose — exact line drift (a refactor adding 10 lines above) shouldn't demote confidence; the construct moving to a different file or being deleted should.

## `/kb-refresh` pseudocode

```
Read state/cursors.json
For each source in [code, commits, issues, prs, discussions, wiki]:
  Fetch deltas via kb-fetch-<source>.ps1
  If no deltas: continue
  Determine affected areas (path-pattern match)
  For each affected area, spawn the relevant indexer agent scoped to {area, since: cursor}
  Apply fenced output via kb-state.ps1 apply-fences
  Update cursor atomically
Random-sample K=5 KB files; run kb-audit-citations.ps1
Append refresh summary to audit-log.md
Update last_verified + last_verified_sha on all touched files (set in fenced output by indexers)
```

## Per-source ownership

| Source | Indexer | Cursor field | Helper script |
|---|---|---|---|
| `code` | `kb-architect` (architecture) + `kb-issue-detector` (debt) | `code.sha` | (none — uses `Glob`/`Read`/`Grep` directly) |
| `coverage` | `kb-architect` (`coverage-fill` mode) | (none — queue is `state/deferred-coverage.json`) | (none) |
| `commits` | `kb-historian` | `commits.sha`, `commits.year_done_through` | `kb-fetch-commits.ps1` |
| `issues` / `prs` / `discussions` | `kb-github-curator` | `issues.updated_at`, etc. | `kb-fetch-github.ps1` |
| `wiki` | `kb-github-curator` (mirror pass) | `wiki.sha` | `kb-fetch-wiki.ps1` |

## Deferred-coverage queue

A persistent budget-paced backlog of Tier-2 files that an indexer ran knowingly skipped (typically `architecture-per-area` runs that finished below the 90% gate). `/kb-refresh --source coverage` drains this queue gradually so coverage catches up without ever blocking a step that's otherwise architecturally complete.

### State file

`.claude/knowledge-base/state/deferred-coverage.json`:

```json
{
  "schema": 1,
  "areas": {
    "EXPR-TRANS": {
      "deferred_at": "2026-04-26",
      "deferred_at_sha": "3727a580c828e4f983da2de934d4cfc12d0cb255",
      "files": [
        {"path": "Source/LinqToDB/Internal/Linq/Builder/MergeBuilder.Operations.cs", "reason": "budget"},
        {"path": "Source/LinqToDB/Internal/Linq/Builder/ExpressionBuildVisitor.cs",  "reason": "budget"}
      ]
    }
  }
}
```

The `files[]` array is treated as a **set keyed by path**: re-emitting the same path overwrites the prior reason. Reasons match the Tier-2 skip values in [`kb-coverage-tiers.md`](kb-coverage-tiers.md).

### How entries land

- An indexer agent emits `=== DEFERRED-COVERAGE: <area> ===` (see [`../agents/_shared/kb-protocol.md`](../agents/_shared/kb-protocol.md)) — `kb-state.ps1 apply-fences` merges it into the queue and appends a one-line summary to `audit-log.md`.
- The one-shot backfill script `kb-coverage-backfill.ps1` may seed the queue for areas whose `INDEX.md` predates this mechanism, by enumerating the area's Tier-2 path patterns and subtracting Tier-1 + any explicitly-named "Read:" entries from the existing coverage block.
- Direct manual writes via `kb-state.ps1 set-deferred-area` are allowed for ad-hoc additions — the orchestrating skill is the only writer otherwise.

### How entries leave

- `/kb-refresh --source coverage` drains up to `--coverage-budget` files per run. The default is **10**; passing `0` skips the source for that run. Files are taken in path-sort order, fairly distributed across areas (`ceil(budget / N)` per area, capped by area queue size).
- The `code` source prunes overlap automatically: any file in the diff that is currently queued is removed when the code-source pass is applied. Re-scanning a file on the trunk supersedes its queued status.
- An indexer can emit `=== DEFERRED-COVERAGE-CLEAR: <area> ===` to remove specific paths after re-visiting them; this is the standard `coverage-fill` exit.

### Effect on confidence

Files leaving the queue via `coverage-fill` push the area's `coverage_tier_2` numerator up. When the new ratio reaches ≥ 90%, the agent promotes `confidence` from `medium` to `high` in the area's `INDEX.md`. The file's `last_verified` / `last_verified_sha` advance to the run's HEAD. There is no automatic demotion when entries land — adding to the queue is signalling, not regret.

## Failure modes and recovery

- **`gh api` rate-limit hit**: scripts emit `{"status":"rate-limited", "reset_at":"<ISO>"}`; `/kb-refresh` exits cleanly with a note in `audit-log.md`. User retries after the reset.
- **Wiki repo gone / private**: `kb-fetch-wiki.ps1` returns `{"status":"unreachable"}`; refresh skips wiki, advances no other cursors, and warns the user.
- **Code scan finds a file in an area's pattern set that doesn't classify into Tier 1/2/3**: indexer flags it in the artifact body as `unclassified-file` and continues. User extends `kb-areas.md` or `kb-coverage-tiers.md` and re-runs.
- **Cursor file corrupt / missing**: `/kb-refresh` refuses to run and prints the path. User restores from git history (`.claude/` is committed).
