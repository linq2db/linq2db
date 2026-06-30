# Knowledge base architecture

Reference for the persistent knowledge base under `.agents/knowledge-base/`. Auto-imported by every `kb-*` skill and every KB indexer agent. Skills do not restate this file — they reference it.

## Directory layout

```
.agents/knowledge-base/
  README.md                              # what's here, how to read, how to (re)build
  glossary.md                            # domain terminology
  state/
    build-progress.json                  # step ledger
    cursors.json                         # all source cursors in one file
    audit-log.md                         # append-only refresh + gate-failure trail
  architecture/                          # global subsystem docs
    overview.md
    query-pipeline.md
    sql-ast.md
    expression-translator.md
    providers.md
    public-api.md
    ...
  conventions/                           # cross-area code-style + patterns
    naming.md
    column-alignment.md
    nullable-handling.md
    public-api-discipline.md
    legacy-patterns.md
  history/
    by-year/<YYYY>.md                    # one MD per year
    decisions/<slug>.md                  # ADR-equivalent records
  github/
    issues-index.json                    # thin index, one entry per issue
    prs-index.json
    discussions-index.json
    milestones.json
    wiki/<article-slug>.md               # full mirror
  detected-issues/
    index.json                           # queryable
    items/<id>.md                        # full description per item
  areas/
    <area>/
      INDEX.md                           # area overview + Tier file lists
      issues.md                          # extracted GH-issue themes for this area
      decisions.md                       # decision links scoped to area
      tech-debt.md                       # roll-up of detected-issues for area
      patterns.md                        # idiomatic + legacy patterns observed here
```

`.build/.agents/kb-*` (gitignored) holds transient fetch caches: raw `gh api` JSON, the cloned wiki repo, per-batch diff dumps. KB scripts write there freely; nothing from `.build/` is canonical.

## Frontmatter contract

Every MD file under `architecture/`, `conventions/`, `history/decisions/`, and `areas/<area>/` carries:

```yaml
---
area: <code>           # area code from kb-areas.md, or GLOBAL for cross-cutting docs
kind: architecture | convention | decision | area-index | issues | tech-debt | patterns
sources: [code | git | gh-issues | gh-prs | gh-discussions | wiki]   # which sources fed this file
confidence: high | medium | low
last_verified: 2026-04-25                # ISO date
last_verified_sha: <commit-sha>          # repo HEAD when this file was last verified
coverage_tier_1: <visited>/<total>       # omit on files that don't have Tier-classified inputs (e.g. decisions)
coverage_tier_2: <visited>/<total>
---
```

`history/by-year/<YYYY>.md` uses a simpler frontmatter (`area: GLOBAL`, `kind: history-year`, `sources: [git]`, `confidence`, `last_verified`, `last_verified_sha`) — no coverage block.

`github/wiki/*.md` is mirrored verbatim from the wiki repo and carries no frontmatter (it's not authored knowledge — it's a mirror).

## Coverage block

Every file with `coverage_tier_*` in frontmatter ends with a collapsible coverage section:

```markdown
<details><summary>Coverage</summary>

- Tier 1 (visited / total): 12 / 12 ✓
  - Source/LinqToDB/SqlQuery/SqlAst.cs
  - Source/LinqToDB/SqlProvider/ISqlBuilder.cs
  - ...
- Tier 2 (visited / total): 87 / 92 (94.6%) ✓
  - skipped: BasicSqlBuilder.OldOverload (deprecated near-duplicate)
  - skipped: ...
- Tier 3 (skipped, logged): 14
</details>
```

Tier definitions and gate thresholds live in [`kb-coverage-tiers.md`](kb-coverage-tiers.md). The block is the single source of truth for *what was actually visited* — a reader who suspects a file was missed reads the block.

## Index files (JSON)

Three index files are queryable artifacts:

- `github/issues-index.json` — array of `{id, title, state, labels[], area, linked_files[], summary, updated_at}`. One entry per issue.
- `github/prs-index.json` — same shape, plus `merged: bool`, `mergedAt`, `headRef`, `baseRef`.
- `github/discussions-index.json` — `{id, title, category, state, labels[], area, summary, updated_at}`.
- `detected-issues/index.json` — see [`kb-issue-categories.md`](kb-issue-categories.md) for the entry shape and lifecycle.

`github/milestones.json` is a single object: `{open: [{title, due_on, scope_summary}], closed: [...]}`.

Indexers update entries via the `INDEX-PATCH` fence (see [`kb-protocol.md`](../agents/_shared/kb-protocol.md)); the skill's `apply-fences` operation merges them into the on-disk index.

## State files

`state/build-progress.json`:

```json
{
  "schema": 1,
  "started_at": "2026-04-25T12:00:00Z",
  "current_step": 3,
  "steps": [
    {"id": 0, "name": "bootstrap", "status": "done", "started_at": "...", "finished_at": "...", "gate_failures": []},
    {"id": 1, "name": "area-registry", "status": "done", ...},
    {"id": 2, "name": "architecture-overview", "status": "partial", "gate_failures": ["Tier-1 88% on overview.md"]},
    ...
  ]
}
```

`state/cursors.json`:

```json
{
  "schema": 1,
  "code":          {"sha": "abc1234", "verified_at": "2026-04-25T12:00:00Z"},
  "commits":       {"sha": "abc1234", "year_done_through": 2025},
  "issues":        {"updated_at": "2026-04-20T08:30:00Z"},
  "prs":           {"updated_at": "2026-04-20T08:30:00Z"},
  "discussions":   {"updated_at": "2026-04-20T08:30:00Z"},
  "wiki":          {"sha": "fedcba9"}
}
```

`state/audit-log.md` is plain markdown — append-only; each entry is a `## <ISO-timestamp> — <event>` heading with a few bullets underneath. Events: `kb-build step done`, `kb-build gate failure`, `kb-refresh summary`, `kb-refresh confidence demotion`, `kb-refresh manual abort`.

## Pointers

- Build steps and gates: [`kb-build-steps.md`](kb-build-steps.md)
- Refresh + cursors: [`kb-refresh-cursors.md`](kb-refresh-cursors.md)
- Coverage tiers: [`kb-coverage-tiers.md`](kb-coverage-tiers.md)
- Area registry: [`kb-areas.md`](kb-areas.md)
- Detected-issue taxonomy: [`kb-issue-categories.md`](kb-issue-categories.md)
- Selection grammar (used by `/kb-issues`): [`kb-selection-grammar.md`](kb-selection-grammar.md)
- Fenced agent protocol: [`../agents/_shared/kb-protocol.md`](../agents/_shared/kb-protocol.md)

## Proposed enhancement: hybrid retrieval for `kb-ask` / `kb-research` (design note — not yet built)

**Status: proposed, not implemented.** Today `/kb-ask` answers by handing `kb-research` a curated doc shortlist that the agent reads; retrieval is effectively keyword / `Grep` plus human-curated shortlists over the markdown corpus and the JSON indexes. That underperforms on the queries this KB sees most — exact-token lookups: issue / PR numbers, SQL-builder type names (`BasicSqlOptimizer`), capability-flag names (`IsCrossJoinSyntaxRequired`), member names — where a lexical hit and a semantic match diverge.

The proposed shape (borrowed from production-RAG practice) is **hybrid retrieval**: run a lexical search (BM25 / full-text over the corpus) and a semantic search (embeddings over chunked docs) in parallel, fuse the two ranked lists with **Reciprocal Rank Fusion** (`score = Σ 1/(k + rankᵢ)`, k ≈ 60 — training-free, scale-agnostic, no score calibration needed between the two), then optionally re-rank the top ~20 candidates with a cross-encoder before they enter the answer context. Lexical covers exact tokens; semantic covers paraphrase.

This is a **build, not a doc edit** — it needs an embedding store, a chunking pass over `architecture/` / `conventions/` / `areas/` / the GH indexes, and changes to how `kb-research` is fed. Open questions to settle before committing: where embeddings live (the KB is checked-in markdown; an embedding index is a new artifact — gitignored under `.build/.agents/kb-*`, or a committed binary), which embedding + reranker models, and whether the corpus is even large enough to beat the current shortlist + grep approach. **Measure first:** on a small corpus, grep + good shortlists can win, and an agent working a local-disk repo tends to de-prioritize RAG/MCP retrieval tooling anyway. Treat this section as the spec to evaluate, not an approved change.
