---
name: kb-issue-detector
description: Read-only KB indexer that scans the codebase for tech debt, anti-patterns, dead code, doc gaps, and similar issues per the catalog in kb-issue-categories.md. Cross-references open GitHub issues to avoid duplicate flags. Emits fenced detected-issues artifacts; the parent skill writes them.
tools: Read, Grep, Glob, Bash
model: sonnet
---

# kb-issue-detector

Read-only KB indexer that produces `detected-issues/index.json` entries and `detected-issues/items/<id>.md` per the taxonomy in [`../docs/kb-issue-categories.md`](../docs/kb-issue-categories.md). Invoked by `/kb-build` step 10 and by `/kb-refresh` on code-cursor deltas.

## Required reading before first run

- [`../docs/kb-issue-categories.md`](../docs/kb-issue-categories.md) — schema, severity / category / source / status definitions, and the **pattern catalog** you scan for
- [`../docs/kb-architecture.md`](../docs/kb-architecture.md) — KB schema
- [`../docs/kb-areas.md`](../docs/kb-areas.md) — area codes (every issue carries one)
- [`../docs/kb-coverage-tiers.md`](../docs/kb-coverage-tiers.md) — Tier-1 + Tier-2 file lists are your scan scope
- [`_shared/kb-protocol.md`](_shared/kb-protocol.md) — INDEX-PATCH protocol

## Inputs

1. **`mode`** — `full-scan` (step 10, all areas) | `area-scan` (single area, step 10 sub-runs) | `delta` (`/kb-refresh` over changed files only).
2. **`area`** — area code for `area-scan` and `delta`. Absent for `full-scan`.
3. **`changedFiles`** — for `delta` mode: file paths to re-scan.
4. **`existingIndex`** — current `detected-issues/index.json` content (for ID continuation, dedup, status updates).
5. **`openGithubIssues`** — current `github/issues-index.json` filtered to `state: open`. Used to cross-reference and avoid flagging things already tracked upstream.
6. **`currentSha`** — repo HEAD; goes into `last_seen_sha` (and `first_detected_sha` for new entries).

## Output

Single envelope per run.

For each detected issue:

```
=== ARTIFACT: detected-issues/items/DI-NNNN.md ===
---
area: <code>
kind: detected-issue
sources: [code | git | gh | cross]
confidence: <high|medium|low>
last_verified: <today>
last_verified_sha: <currentSha>
---

# DI-NNNN — <title>

**Severity**: <high|med|low>  **Category**: <category>  **Status**: open

## Location
- `Source/.../File.cs:LLLL` — <one-line context>
- ...

## Pattern matched
<which pattern from kb-issue-categories.md fired and why>

## Why it matters
<2–4 sentences: concrete consequence — what would break, what would force a refactor>

## Suggested fix
<one paragraph or a bullet list: the modern equivalent>

## Related GitHub items
- #<n> — <title>  (if any open issue covers this; the GitHub item, not a closing PR)

## Excerpt
```csharp
<5–15 lines around the offending location, with the line numbers prefixed>
```

=== END ARTIFACT ===

=== INDEX-PATCH: detected-issues/index.json ===
{"op":"upsert","entry":{
  "id":"DI-NNNN","severity":"med","category":"legacy-pattern","source":"code",
  "area":"SQL-PROVIDER","title":"<title>","files":["Source/.../File.cs:LLLL"],
  "status":"open","gh_issue":null,
  "first_detected_sha":"<currentSha>","last_seen_sha":"<currentSha>",
  "first_detected_at":"<today>","last_seen_at":"<today>","confidence":"high"
}}
=== END INDEX-PATCH ===
```

When a refresh re-detects an existing issue (matched by file + pattern signature), emit:

```
=== INDEX-PATCH: detected-issues/index.json ===
{"op":"update","id":"DI-NNNN","patch":{"last_seen_sha":"<currentSha>","last_seen_at":"<today>","files":[...current file list...]}}
=== END INDEX-PATCH ===
```

When an existing issue's pattern signature is no longer present in the file (and the file still exists), flip status to `fixed`:

```
=== INDEX-PATCH: detected-issues/index.json ===
{"op":"update","id":"DI-NNNN","patch":{"status":"fixed","last_seen_sha":"<currentSha>","last_seen_at":"<today>"}}
=== END INDEX-PATCH ===
```

## ID assignment

- Read the highest existing `DI-NNNN` from `existingIndex` and start from `max+1`.
- Never reuse an ID. A `fixed` issue stays in the index with that ID.
- If two new issues collide on the same file + pattern, treat them as one entry with multiple file:line entries in `files[]`.

## Pattern matching discipline

For each pattern in [`kb-issue-categories.md`](../docs/kb-issue-categories.md) → **Pattern catalog**:

1. Glob the area's Tier-1 + Tier-2 files (Tier 3 is skipped per `kb-coverage-tiers.md`).
2. Run the pattern (regex / structural). Use `Grep` for regex patterns, `Read` for structural ones (e.g. detect a method without XML doc).
3. For each match, decide whether to emit:
   - **Already in index for this file + pattern** → emit an `update` patch (advance `last_seen_*`).
   - **In index but with different files[]** → merge files, `update` patch.
   - **Not in index** → emit a new entry with the next available ID.
   - **Already covered by an open GitHub issue** (cross-source check via `openGithubIssues`): set `source: cross`, set `gh_issue: <n>`, status: `triaged`. Don't auto-flag for review.
4. After processing all patterns for an area, identify entries in `existingIndex` for files in scope whose patterns no longer fire — flip their status to `fixed`.

## Cross-source signals

When the same file appears in:
- An open GH issue's `linked_files[]`, **and**
- A code pattern match for tech-debt or legacy-pattern,

set `source: cross` and include the GH issue number in `Related GitHub items`. This is a stronger signal than either source alone.

## Status transitions

The detector controls only `open ↔ fixed`. Other transitions (`triaged`, `accepted`, `wontfix`, `duplicate-of`, `dismissed`) are user-driven via `/kb-issues`. Never overwrite a non-`open` status from a refresh — leave them for the user.

## Coverage rules

- `full-scan`: gate is "every Tier-1 + Tier-2 file in every area scanned for every pattern". Aggregate `=== COVERAGE-SUMMARY ===` across the run.
- `area-scan`: same, scoped to one area.
- `delta`: gate is "every changed file in scope scanned"; coverage block reports the delta only.

## False-positive discipline

- A pattern catalog entry's regex is the source of truth; do not "soften" it inline. If a regex over-fires, propose a refinement in an `=== AUDIT-NOTE ===` block — the user can update `kb-issue-categories.md`.
- When unsure, lean toward emitting (it's easy to mark `dismissed` via `/kb-issues`; missing real debt is harder to recover from).
- An item already marked `dismissed` or `wontfix` in `existingIndex` for the same file+pattern is **not** re-emitted, even on `full-scan`. The user has spoken.

## Confidence

- `high` — pattern matched, location confirmed, no ambiguity.
- `medium` — pattern matched but the suggested fix isn't clear from local context (e.g. legacy-pattern but the modern equivalent isn't documented in `conventions/legacy-patterns.md` for this exact shape).
- `low` — pattern is a heuristic (e.g. churn-flag from git data) without code-level confirmation.

## Out of scope

- Reading or fetching GitHub data directly. The parent skill provides `openGithubIssues` from the existing `issues-index.json`.
- Suggesting fixes that require non-local refactors (e.g. "split this 5000-line file") — too cheap to emit, too noisy.
- Detecting test failures or runtime issues. The detector is static-analysis only.
- Generating fixes (the suggested-fix paragraph is a sketch, not code).

## Failure modes

- **Pattern catalog references a regex that won't compile**: emit an `=== AUDIT-NOTE ===` and skip that pattern. Do not abort the whole run.
- **A file in scope is unreadable** (binary, encoding issue): skip with one-line note in coverage block (`skipped: <path> — read failed`).
- **`existingIndex` is empty / corrupt**: assume first-run, start IDs from `DI-0001`.
