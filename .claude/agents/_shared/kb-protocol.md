# KB indexer fenced-output protocol

Contract every KB indexer agent (`kb-architect`, `kb-historian`, `kb-github-curator`, `kb-issue-detector`) follows. Skills parse this protocol; agents emit it; `kb-state.ps1 apply-fences` validates and applies it.

The indexer is **read-only**. It never writes to `.claude/knowledge-base/` directly. Its single output is a structured stdout payload of fences. The orchestrating skill applies all writes after validation.

## Required envelope

The agent's final message starts with `=== KB-INDEXER OUTPUT v1 ===` and ends with `=== END KB-INDEXER OUTPUT ===`. Everything between is one or more fenced blocks. The agent may emit prose before the envelope (analysis, questions, intermediate notes) — the skill ignores everything outside the envelope.

```
=== KB-INDEXER OUTPUT v1 ===

<one or more fenced blocks>

=== END KB-INDEXER OUTPUT ===
```

## Fence types

### `=== ARTIFACT: <relative-path> ===` ... `=== END ARTIFACT ===`

Emit a complete file under `.claude/knowledge-base/`. Path is relative to `.claude/knowledge-base/` and uses forward slashes. The file is written verbatim — the skill validates frontmatter and coverage block but does not edit content.

```
=== ARTIFACT: areas/SQL-PROVIDER/INDEX.md ===
---
area: SQL-PROVIDER
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-25
last_verified_sha: abc1234def
coverage_tier_1: 8/8
coverage_tier_2: 23/25
---

# SQL-PROVIDER

<body>

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 8 / 8 ✓
  - Source/LinqToDB/SqlProvider/BasicSqlBuilder.cs
  - ...
- Tier 2 (visited / total): 23 / 25 (92%) ✓
  - skipped: BasicSqlBuilder.OldOverload (deprecated)
  - skipped: BasicSqlBuilder.LegacyEntry (near-duplicate)
- Tier 3 (skipped, logged): 4
</details>
=== END ARTIFACT ===
```

Validation rules applied by the skill:

1. Path must match one of the known KB locations (`architecture/`, `conventions/`, `history/by-year/`, `history/decisions/`, `github/wiki/`, `detected-issues/items/`, `areas/<area>/`, root-level `glossary.md` / `README.md`).
2. Frontmatter must parse as YAML and contain the required fields for the file's `kind` (see [`kb-architecture.md`](../../docs/kb-architecture.md) → **Frontmatter contract**).
3. `area:` must match a code from [`kb-areas.md`](../../docs/kb-areas.md) (or `GLOBAL`).
4. If frontmatter has `coverage_tier_1` / `coverage_tier_2`, the body MUST contain a coverage block; the visited/total numbers must match the frontmatter.
5. `last_verified` must be today's ISO date; `last_verified_sha` must be the current `git rev-parse HEAD` of `linq2db`.

Failures are recorded in `gate_failures[]`; the skill writes the file *only* if all rules pass. A failed artifact does not block other artifacts in the same envelope.

### `=== INDEX-PATCH: <relative-path> ===` ... `=== END INDEX-PATCH ===`

Per-entry mutation of a JSON index file. Body is a single JSON object: `{op, entry}`.

Supported ops:

- `upsert` — insert if `entry.id` not present, else replace.
- `delete` — remove entry by `id` (body: `{op: "delete", id: "DI-0042"}`).
- `update` — partial merge into existing entry (body: `{op: "update", id: "DI-0042", patch: {...}}`).

```
=== INDEX-PATCH: detected-issues/index.json ===
{"op":"upsert","entry":{"id":"DI-0123","severity":"med","category":"legacy-pattern",...}}
=== END INDEX-PATCH ===
```

Validation:

1. Target file must be one of the known indexes (`github/issues-index.json`, `github/prs-index.json`, `github/discussions-index.json`, `detected-issues/index.json`).
2. `entry.id` must be present for `upsert` / `update`.
3. For `detected-issues`, `entry.id` must match `DI-NNNN`; for upserts, ID must be ≥ current highest + 1 (no reuse).
4. `entry.area` must be a known code.

### `=== INDEX-WRITE: <relative-path> ===` ... `=== END INDEX-WRITE ===`

Full overwrite of a non-incremental index file (`github/milestones.json`). Body is the complete JSON.

```
=== INDEX-WRITE: github/milestones.json ===
{"open":[...], "closed":[...]}
=== END INDEX-WRITE ===
```

### `=== COVERAGE-SUMMARY ===` ... `=== END COVERAGE-SUMMARY ===`

Optional. Aggregate coverage report across the agent's whole run. Body is JSON: `{"tier_1": {visited, total}, "tier_2": {visited, total}, "tier_3": {skipped}}`. Used by the gate when the step's gate threshold is computed across multiple artifacts (e.g. step 3 architecture-per-area aggregates across all per-area INDEX.md files).

```
=== COVERAGE-SUMMARY ===
{"tier_1":{"visited":42,"total":42},"tier_2":{"visited":118,"total":127},"tier_3":{"skipped":31}}
=== END COVERAGE-SUMMARY ===
```

### `=== UNCLASSIFIED-FILE: <path> ===` ... `=== END UNCLASSIFIED-FILE ===`

Optional. Reports a file the agent encountered that didn't classify into Tier 1/2/3 under `kb-areas.md`. Body is a one-line reason. The skill collects these into `audit-log.md` for the user to triage (extend `kb-areas.md` or `kb-coverage-tiers.md`).

```
=== UNCLASSIFIED-FILE: Source/LinqToDB/Experimental/NewThing.cs ===
not in any area's path patterns
=== END UNCLASSIFIED-FILE ===
```

### `=== AUDIT-NOTE ===` ... `=== END AUDIT-NOTE ===`

Optional. Free-text observation the agent wants to surface to the user without producing an artifact. Appended to `audit-log.md` verbatim. Use sparingly — most observations belong in artifacts.

## Encoding

- UTF-8, no BOM. Newlines are LF (the skill normalizes CRLF to LF on write).
- Fence headers are case-sensitive and must start at column 0.
- Bodies inside fences are literal — no escaping. The closing `=== END ... ===` line must also start at column 0.

## Failure handling

If the agent cannot complete the step (e.g. a file's content makes coverage impossible without user input), it emits the partial artifacts that *did* complete + an `=== AUDIT-NOTE ===` explaining what blocked. The skill records `partial` status and surfaces the audit note to the user. Re-running picks up from the last successful artifact via the cursor.

## Read-only enforcement

Indexer agents have only `Read`, `Grep`, `Glob`, and (where listed) `Bash` for `gh api` / `git log`. They have **no** `Edit` / `Write` / `NotebookEdit`. Attempts to bypass via shell redirect must fail at the permission layer; if an agent is observed writing to `.claude/knowledge-base/` directly, treat it as a contract bug and fix the agent.

## Versioning

`v1` is the current protocol version. Bumps go to `v2` etc. Skills accept the highest version they know; agents always emit a single version. Mixed-version envelopes within one run are not supported.
