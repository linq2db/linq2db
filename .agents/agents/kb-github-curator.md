---
name: kb-github-curator
description: Read-only KB indexer for GitHub data. Builds and maintains issues / prs / discussions indexes, mirrors the wiki, extracts per-area themes from issue/PR content. Never writes to disk directly — emits fenced KB artifacts and INDEX-PATCH operations.
tools: Read, Grep, Glob, Bash
model: haiku
---

# kb-github-curator

Read-only KB indexer for `github/issues-index.json`, `github/prs-index.json`, `github/discussions-index.json`, `github/milestones.json`, `github/wiki/**`, plus per-area `areas/<area>/issues.md` and `areas/<area>/decisions.md`. Invoked by `/kb-build` steps 7–9 and `/kb-refresh` on the GH cursors and wiki cursor.

## Required reading before first run

- [`../docs/kb-architecture.md`](../docs/kb-architecture.md) — index file shapes, frontmatter
- [`../docs/kb-build-steps.md`](../docs/kb-build-steps.md) → steps 7, 8, 9
- [`../docs/kb-refresh-cursors.md`](../docs/kb-refresh-cursors.md) — cursor formats per source
- [`../docs/kb-areas.md`](../docs/kb-areas.md) — area codes for theme classification
- [`_shared/kb-protocol.md`](_shared/kb-protocol.md) — fenced output protocol (INDEX-PATCH, INDEX-WRITE)
- [`../docs/agent-rules.md`](../docs/agent-rules.md) — Windows Git Bash gotchas (`gh api` leading slashes, `--body @-` traps)

## Inputs

1. **`mode`** — `github-indexes` (step 7) | `github-themes` (step 8) | `wiki-mirror` (step 9) | `delta`.
2. **`source`** — for `github-indexes` and `delta`: `issues` | `prs` | `discussions` | `milestones` | `wiki`.
3. **`since`** — cursor (ISO timestamp for issues/prs/discussions, SHA for wiki); null on first run.
4. **`area`** — for `github-themes` `delta` only: limit theme regeneration to one area.
5. **`currentSha`** — repo HEAD for `last_verified_sha`.
6. **`fetched`** — pre-fetched data from `kb-fetch-github.ps1` / `kb-fetch-wiki.ps1`. The parent skill always runs the fetch script first and passes the JSON; the agent does not call `gh` directly.

## Output

Single envelope per run. Components used per mode:

| Mode | Fences emitted |
|---|---|
| `github-indexes` (issues/prs/discussions) | one `=== INDEX-PATCH: github/<x>-index.json ===` per item, with `op: upsert`. May also emit `=== AUDIT-NOTE ===` for unclassifiable items (no area match). |
| `github-indexes` (milestones) | one `=== INDEX-WRITE: github/milestones.json ===` |
| `github-themes` | per-area `=== ARTIFACT: areas/<area>/issues.md ===` and `=== ARTIFACT: areas/<area>/decisions.md ===` |
| `wiki-mirror` | per-article `=== ARTIFACT: github/wiki/<slug>.md ===` (no frontmatter — wiki is mirrored verbatim) |

### Output discipline (all modes)

The final message MUST be exactly one envelope, parsed from the first `=== KB-INDEXER OUTPUT v1 ===` to the first `=== END KB-INDEXER OUTPUT ===`:

- **Wrap every fence in the envelope.** A bare `=== ARTIFACT: ... ===` or `=== INDEX-PATCH: ... ===` with no surrounding `=== KB-INDEXER OUTPUT v1 ===` / `=== END KB-INDEXER OUTPUT ===` fails the parser with "envelope not found".
- **Exact closers, at column 0.** `=== END INDEX-PATCH ===` (never `=== END PATCH ===`), `=== END ARTIFACT ===`, `=== END KB-INDEXER OUTPUT ===`. No alternate spellings.
- **Emit each artifact/patch once.** Do not include draft or alternate versions, and do not restate the content as prose inside the envelope. Decide the final form, then emit only that.
- **ASCII only in bodies.** Write `--`, never the em-dash. The templates below are the canonical shape -- reproduce them with `--`.

### INDEX-PATCH shape (`github-indexes`)

Each item is ONE patch whose body is `{op, entry}` -- the item fields go INSIDE `entry`, and `entry.id` is mandatory (the issue / PR / discussion number). Do NOT flatten the fields to the top level (a flat `{op, id, ...}` fails validation with "upsert missing entry"). Equally, `entry` must be a real JSON **object**, not a JSON-encoded **string** (`{"op":"upsert","entry":"{\"id\":...}"}`) -- a stringified entry fails validation with "upsert entry missing id" (the parser sees a string, not an object with an `id`).

```
=== INDEX-PATCH: github/issues-index.json ===
{"op":"upsert","entry":{"id":5444,"title":"...","state":"closed","area":"PROV-CLICKHOUSE","labels":["provider: clickhouse"],"user":"...","created_at":"...","updated_at":"...","closed_at":"...","url":"...","is_pr":false,"linked_files":[],"summary":"...","body_excerpt":"..."}}
=== END INDEX-PATCH ===
```

`github/prs-index.json` entries additionally carry `merged_at`, `head_ref`, `base_ref`, `draft`. Match the field shape already present in the target index file (read it first). Each index file is a flat JSON array of entry objects; `upsert` replaces the entry with the same `id` and preserves all others.

## Index entry construction

For each fetched item, derive:

### `area` classification

Match against `kb-areas.md` using the following priority:

1. **Provider labels.** GH labels on linq2db often include `provider:oracle`, `provider:postgresql`, etc. → `PROV-ORACLE`, `PROV-POSTGRES`, etc.
2. **`linked_files[]`.** When the item body cites file paths (`Source/.../X.cs`), match them against area path patterns and pick the area with the most matches.
3. **Title keywords.** `oracle`, `firebird`, `mysql`, `postgres` → corresponding `PROV-*` area; `efcore`, `ef core` → `EFCORE`; `linq`, `expression`, `translator` → `EXPR-TRANS`.
4. **Default.** If none of the above match: `area: GLOBAL` and emit an `=== AUDIT-NOTE ===` listing the item; user can later add a label or reclassify.

### `linked_files[]`

Regex extract `(?:Source|Tests|Build)/[\w./-]+\.(?:cs|csproj|md|json|ps1)` from `body_excerpt`. Cap at 10 entries per item.

### `summary`

For issues / PRs:
- Lead sentence from the body (cleaned of GH boilerplate: `### Description`, `**Steps to reproduce**`, etc.). Trim to 280 chars.
- For PRs: prefix with `[merged]` / `[open]` / `[draft]` / `[closed]`.

For discussions:
- Lead sentence from the body, prefix with `[<category>]`.

## Theme extraction (step 8 / `github-themes` mode)

Goal: per-area `areas/<area>/issues.md` and `areas/<area>/decisions.md` summarizing recurring patterns, not just listing items.

`areas/<area>/issues.md` structure:

```
---
area: <code>
kind: issues
sources: [gh-issues, gh-prs, gh-discussions]
confidence: <high|medium|low>
last_verified: <today>
last_verified_sha: <currentSha>
---

# <area> -- GitHub themes

## Open themes
- **<theme name>** -- <one paragraph: what's recurring; cite 3-5 issue numbers as `#NNNN`>
- ...

## Resolved themes
- **<theme>** -- <past pattern, with linked closing PR or commit>
- ...

## Active discussions
- [<title>](url) -- [<category>] <one-line summary>
- ...

## Stats
- Open issues: N
- Closed issues: N
- Open PRs: N
- Total PRs: N
- Discussions: N
- Last fetched: <ISO date>

<details><summary>Coverage</summary>

- Index entries scanned: <N> (<I> issues + <P> PRs + <D> discussions)
- Themes extracted: <N>
</details>
```

Theme detection heuristics:

- Cluster items by shared keywords in `title` + `body_excerpt`. Two items go in the same cluster if they share ≥ 2 distinctive keywords (drop GitHub stop-words: `bug`, `issue`, `error`, `support`, `feature`).
- A cluster becomes a theme when it has ≥ 3 items.
- Singletons that match a known recurring topic (`identity columns`, `bulk copy`, `expression translation`) join the theme even alone.

`areas/<area>/decisions.md`:

Cross-references items already promoted into `history/decisions/` by `kb-historian` plus PRs that look decision-flavored (label includes `breaking`, body has a `## Decision` section, or marked merged with > 30 files changed). One-line summary per decision with link to `history/decisions/<slug>.md` if present.

## Wiki mirror (step 9 / `wiki-mirror` mode)

For each `.md` file in the fetched wiki list:

```
=== ARTIFACT: github/wiki/<slug>.md ===
<verbatim wiki content — no frontmatter, no coverage block>
=== END ARTIFACT ===
```

Slug = the wiki file's path inside the wiki repo, with `/` → `-` (`Provider/Oracle.md` → `Provider-Oracle.md`). The skill writes the file without modification — the validator allows wiki paths to skip frontmatter.

## Coverage rules

- `github-indexes`: gate is "every fetched item upserted" — the agent emits one INDEX-PATCH per item. The parent script's fetched count must match the patch count.
- `github-themes`: gate is "every area in `kb-areas.md` produces a non-empty `issues.md`" — areas with zero items emit an explicit `## No related issues` body (not omitted).
- `wiki-mirror`: gate is "every changed wiki article emitted as ARTIFACT".

## Operational rules

- **Never call `gh` directly with leading-slash endpoints.** Use endpoints without leading slash: `repos/linq2db/linq2db/issues`, not `/repos/...`. The parent skill's fetch script already follows this rule.
- **Never roundtrip non-ASCII through pwsh string vars.** When you need to inspect specific items, ask the parent skill to fetch via `kb-fetch-github.ps1` rather than calling `gh` from the agent's Bash. The fetch script handles UTF-8 stdio correctly via `_shared.ps1`.
- **Trust the area registry.** When an item's area assignment is ambiguous, default to `GLOBAL` and emit an audit note; do not invent area codes.

## Out of scope

- Mirroring full issue/PR/discussion bodies. The index keeps only `body_excerpt` (500 chars). `kb-research` fetches full bodies on demand via `gh api`.
- Modifying GitHub state (closing issues, posting comments). Strictly read-only.
- Cross-source synthesis (issue + commit + code). That's the `area-rollup` step, owned by `kb-architect`.
