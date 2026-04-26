# KB build steps

Source of truth for `/kb-build`. Each step is a savepoint key in `state/build-progress.json`. Status moves through `pending → in-progress → (done | partial)`. A `partial` step is re-entered on the next `/kb-build` run before the skill advances; a `done` step is skipped unless the user passes `--force <step>`.

## Step contract

For each step the skill:

1. Reads `state/build-progress.json`; if step status is `done`, advance to next.
2. Marks the step `in-progress`, sets `started_at`.
3. Spawns the owner agent (or runs the skill body directly for skill-owned steps) with the step's input.
4. Receives the agent's fenced output (see [`kb-protocol.md`](../agents/_shared/kb-protocol.md)).
5. Calls `kb-state.ps1 apply-fences` to validate frontmatter, parse coverage blocks, write artifacts, merge index patches.
6. Runs the gate: artifact-existence check + frontmatter validity + coverage thresholds from [`kb-coverage-tiers.md`](kb-coverage-tiers.md).
7. On gate pass: status → `done`, `finished_at` set, append summary to `audit-log.md`.
8. On gate fail: status → `partial`, append failure reasons to `gate_failures[]` and `audit-log.md`. Skill stops at this boundary; user resumes next session.

The skill never silently advances past a `partial` step.

## Step list

### Step 0 — bootstrap

**Owner**: `/kb-build` itself (no agent).

**Output**:
- `.claude/knowledge-base/` skeleton dirs (`state/`, `architecture/`, `conventions/`, `history/by-year/`, `history/decisions/`, `github/wiki/`, `detected-issues/items/`, `areas/`).
- `.claude/knowledge-base/README.md` with a 1-page overview + "How to read" + "How to refresh".
- `.claude/knowledge-base/glossary.md` stub (frontmatter + `# Glossary` heading + `_(populated by step 12)_`).
- `state/build-progress.json` initialized with all step entries `pending`.
- `state/cursors.json` initialized with all sources at zero (`sha: null`, `updated_at: "1970-01-01T00:00:00Z"`).
- `state/audit-log.md` with a `## <ISO> — kb-build started` entry.

**Gate**: every listed file/dir exists and parses.

### Step 1 — area-registry

**Owner**: `/kb-build` itself, with user confirmation.

**Output**: `.claude/docs/kb-areas.md` populated with the actual area table (replacing the bootstrap proposal).

**Procedure**:
1. Run a directory scan of `Source/`, `Tests/`, `Build/`, `.github/`, `.claude/`.
2. Diff against the bootstrap table in `kb-areas.md`. List dirs without a matching area.
3. Propose new rows to the user; ask for confirmation/edits.
4. Apply user's edits to `kb-areas.md`.

**Gate**: every top-level dir under `Source/` and `Tests/` matches at least one area's path patterns or is in the "Excluded paths" section.

### Step 2 — architecture-overview

**Owner**: `kb-architect`.

**Input**: pinned file list (`Source/LinqToDB/IDataContext.cs`, `DataConnection.cs`, top-level `*.csproj`, `Source/LinqToDB/SqlQuery/SqlStatement.cs`, `Source/LinqToDB/Linq/Builder/ExpressionBuilder.cs`).

**Output**:
- `architecture/overview.md` — 1-page repo map: directory tree + paragraph per top-level area.
- `architecture/query-pipeline.md` — LINQ → SQL pipeline narrative.
- `architecture/public-api.md` — surface map: every namespace, its purpose, and stability commitment.

**Gate**: Tier-1 100% on the pinned file list, Tier-2 ≥90% on `Source/LinqToDB/*.cs` (root).

### Step 3 — architecture-per-area

**Owner**: `kb-architect`, one invocation per area.

**Input**: area code; the agent reads `kb-areas.md` for the file list.

**Output**: `areas/<area>/INDEX.md` for each area, plus per-area architecture docs as needed (`architecture/sql-ast.md`, `architecture/expression-translator.md`, `architecture/providers.md`, `architecture/conventions.md`).

**Gate**: per-area Tier-1 100%, Tier-2 ≥90%.

This step is the largest. The skill processes areas in `kb-areas.md` order and checkpoints after each area; an interrupted run picks up at the first incomplete area.

### Step 4 — conventions

**Owner**: `kb-architect` (specialized pass).

**Output**:
- `conventions/naming.md`
- `conventions/column-alignment.md`
- `conventions/nullable-handling.md`
- `conventions/public-api-discipline.md`
- `conventions/legacy-patterns.md` — explicit catalog of patterns the codebase has migrated away from, with the modern replacement.

**Gate**: each file has frontmatter + body + at least 3 concrete examples with file:line citations.

### Step 5 — history-by-year

**Owner**: `kb-historian`.

**Input**: year range (first commit year through current).

**Output**: `history/by-year/<YYYY>.md` for each year — top commits, releases, themes.

**Gate**: every year covered (no gaps); each file ≥ 200 words.

### Step 6 — history-decisions

**Owner**: `kb-historian`.

**Input**: full commit history; agent uses heuristics (commit-msg keywords `BREAKING`, `design:`, `decision:`, large merges with > 50 files changed) to detect decisions.

**Output**: `history/decisions/<slug>.md` — one per decision.

**Gate**: every commit/PR matching the heuristic visited; each decision has a `Why`, `Decision`, `Consequences` section.

### Step 7 — github-indexes

**Owner**: `kb-github-curator`.

**Input**: cursors at zero (initial run) → fetch all-time.

**Output**:
- `github/issues-index.json`
- `github/prs-index.json`
- `github/discussions-index.json`
- `github/milestones.json`

**Gate**: cursor advanced to `now`; index size matches `gh api search` count.

### Step 8 — github-themes

**Owner**: `kb-github-curator` (theme-extraction pass).

**Input**: `github/issues-index.json` + `prs-index.json` + `discussions-index.json` already populated; area registry.

**Output**: per-area `areas/<area>/issues.md` and `areas/<area>/decisions.md` aggregating themes from GH content. Decision-flavored items also dropped into `history/decisions/`.

**Gate**: every area in `kb-areas.md` has a non-empty `issues.md` (or an explicit "no issues for this area" note).

### Step 9 — wiki-mirror

**Owner**: `kb-github-curator`.

**Output**: `github/wiki/<article-slug>.md` — full mirror of `linq2db.wiki`.

**Gate**: every wiki article present; cursor advanced to wiki HEAD SHA.

### Step 10 — detected-issues

**Owner**: `kb-issue-detector`.

**Input**: pattern catalog from [`kb-issue-categories.md`](kb-issue-categories.md), Tier-1 + Tier-2 file list per area, `github/issues-index.json` for cross-reference.

**Output**:
- `detected-issues/index.json`
- `detected-issues/items/<id>.md` per item.

**Gate**: every Tier-1 + Tier-2 file scanned for every pattern; cross-reference against open GH issues completed.

### Step 11 — area-rollup

**Owner**: `kb-architect` (cross-link pass).

**Input**: all of step 8, step 10, and area registry.

**Output**: `areas/<area>/tech-debt.md` and `areas/<area>/patterns.md` for each area, populated from cross-source data.

**Gate**: every area has all four sub-files (`INDEX.md`, `issues.md`, `decisions.md`, `tech-debt.md`, `patterns.md`).

### Step 12 — glossary

**Owner**: `kb-architect` (glossary pass).

**Input**: full KB content (greps for capitalized terms, repeated abbreviations).

**Output**: `glossary.md` populated with domain terms; cross-link annotations inserted into other KB files (`<term>` → `[<term>](../glossary.md#<anchor>)`).

**Gate**: every domain term used in ≥ 3 KB files appears in glossary.

### Step 13 — validation

**Owner**: `/kb-build` itself.

**Procedure**:
1. Verify every step in `build-progress.json` is `done`.
2. Random-sample K=10 KB files; spawn `kb-research` with a "validate citations" prompt; aggregate confidence.
3. Append summary to `audit-log.md`.
4. Print final `/kb-status` summary.

**Gate**: every gate passed; sample audit succeeds.

## Step ordering invariants

- Step 1 (area-registry) is a hard prerequisite for steps 3, 8, 10, 11.
- Step 7 (GH indexes) is a hard prerequisite for steps 8 and 10 (issue cross-reference).
- Step 10 (detected-issues) is a hard prerequisite for step 11 (`tech-debt.md` aggregates from it).
- Steps 5–6 (history) are independent of code/GH; can run anytime after step 0.

## Resume rules

- A `partial` step is re-entered with the same input; the agent uses item-cursors in `state/cursors.json` to skip already-emitted artifacts.
- A `done` step is skipped unless the user passes `--force <step-name>`.
- Re-running step 1 (area-registry) after the table has been edited regenerates per-area `INDEX.md` files only when the area's row changed.

## Force / scoping flags

- `/kb-build --force <step-name>` — re-run a `done` step from scratch.
- `/kb-build --area <area-code>` — when the current step is area-scoped (3, 8, 10, 11), restrict to one area.
- `/kb-build --since <YYYY-MM-DD>` — for step 5/6 (history) and step 7 (GH), override the cursor to a specific date.
