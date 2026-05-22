---
area: CLAUDE-INFRA
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 28/28
coverage_tier_2: 42/42
---

# CLAUDE-INFRA

Claude Code instruction corpus for the linq2db repository. Everything under `.claude/` (except `.claude/knowledge-base/` which is KB output, not source) plus the root `CLAUDE.md`. Governs how every agent and skill operates on the codebase: branching rules, Bash discipline, GitHub wording, test patterns, PR review workflows, KB build/refresh, and more.

## Anatomy

```
CLAUDE.md                          — root project instructions (auto-loaded by Claude Code)
.claude/
  agents/                          — subagent contracts (consumed via Agent tool)
    _shared/kb-protocol.md         — fenced output protocol for KB indexers
  docs/                            — long-form reference docs (linked from CLAUDE.md / skills)
  hooks/                           — PreToolUse / PostToolUse / SessionEnd hook scripts
  scripts/                         — pwsh manifest-in/manifest-out helper scripts
  skills/                          — user-invocable skill orchestrators (one per subdirectory)
  settings.local.json              — gitignored personal overrides (not indexed)
  knowledge-base/                  — EXCLUDED (KB output, not source)
```

The corpus is instruction-only: skills, agents, and scripts are the "code"; docs are the "specifications" that code references. `CLAUDE.md` is the entry point auto-loaded by Claude Code at session start; it imports `agent-rules.md` via `@.claude/docs/agent-rules.md`.

## Subsystems

### Root instructions: `CLAUDE.md` + `agent-rules.md`

`CLAUDE.md` defines: what linq2db is, build commands (full/core/Release/Testing configs), solution structure, architecture and code-design pointers, code conventions (tabs, C# 14, nullable, no warnings, XML doc), versioning layout in `Directory.Build.props`, and branch conventions (`master`/`release`/`issue/<n>-<slug>`/`feature/<n>-<slug>`).

`agent-rules.md` is auto-imported and defines the operational ruleset: branch creation workflow, Bash command rules (no `&&`/`;`/control-flow), dedicated-tool mandates (`Grep`/`Read`/`Glob`/`Edit` over raw CLI), pipe-avoidance discipline, permission-friendly Bash patterns table, Windows Git Bash MSYS gotchas (leading-slash path-mangling, `--body @-` trap, UTF-8 encoding for non-ASCII), batching/user-interaction rules (ask-ask-do-all), PowerShell Core script pattern for complex multi-step operations, temp-file placement (`.build/.claude/`), git commit / push / PR rules (never auto-commit/-push, always draft PR), Docker container scope (start/stop/create only), GitHub content-editing guardrails (never edit others' content), GitHub wording discipline (terse, no fluff), and agent guardrails (no unrelated reformatting, no silent cross-cutting reshaping, document arbitrary values, never hand-edit `CompatibilitySuppressions.xml`).

### Skills (`.claude/skills/*/SKILL.md`)

Each skill occupies its own folder; every folder currently contains only `SKILL.md` (no helper files).

**KB management:**
- `/kb-build` — phased, resumable orchestrator for building the full KB from scratch (13 steps); spawns `kb-architect`, `kb-historian`, `kb-github-curator`, `kb-issue-detector` agents; uses `kb-state.ps1 apply-fences` for all writes.
- `/kb-refresh` — incremental delta sweep across all KB sources (`code → coverage → commits → issues → prs → discussions → wiki`); drains deferred-coverage queue; runs random citation audit via `kb-audit-citations.ps1`.
- `/kb-ask` — user-facing Q&A; builds a doc shortlist, spawns `kb-research`, prints synthesized answer verbatim.
- `/kb-issues` — interactive triage UI for `detected-issues/index.json`; supports detail/create-GH-issue/fix/wontfix/duplicate/dismiss/re-triage actions; routes writes through `kb-state.ps1 apply-fences`.
- `/kb-status` — read-only dashboard: build progress, source freshness, counts, staleness summary, audit-log tail.

**PR review cycle:**
- `/review-pr` — full PR review: loads context via `pr-context.ps1`, spawns `code-reviewer` + `baselines-reviewer` in parallel, classifies public-API changes, assembles review body, posts pending draft review via `post-pr-review.ps1` after user confirmation.
- `/verify-review` — re-checks prior findings against current HEAD; flips checkboxes in prior review bodies; posts new draft for partial fixes and new findings; uses `apply-verify-writes.ps1` for in-place edits.

**Issue and fix workflow:**
- `/fix-issue` — full fix workflow: loads issue, proposes branch+provider matrix, creates branch, delegates test writing to `test-writer` agent and env setup to `/test-providers`, runs test via `/test`.
- `/create-issue` — creates a GitHub issue with code-verified claims, duplicate check, label/milestone proposal, explicit user confirmation before posting.
- `/find-issues` — read-only topic/ticket duplicate search; verdict output; suggests `/merge-duplicates` when likely duplicate.
- `/merge-duplicates` — consolidates duplicate issues: posts consolidation comment on canonical, adds labels, posts closing comments, closes dupes with `state_reason=duplicate`; closes last to ensure no rollback needed.

**Test workflow:**
- `/test` — write + run orchestrator; delegates writing to `test-writer`, running to `test-runner`; handles baselines diff via `snap-baselines.ps1`/`diff-baselines.ps1`.
- `/test-providers` — env management: enables/disables providers in `UserDataProviders.json` per TFM bucket, manages Docker containers; owns all writes to `UserDataProviders.json`; supports Set/add/remove/stop/reset modes with family-rule normalization and sticky entries (`TestNoopProvider`).

**Infrastructure:**
- `/api-baselines` — regenerates `Source/**/CompatibilitySuppressions.xml` via `dotnet pack -p:ApiCompatGenerateSuppressionFile=true`; gates on non-`LinqToDB.Internal.*` changes; never hand-edits baselines directly.
- `/version-bump` — bumps `<Version>` and four `<EFxVersion>` values in `Directory.Build.props` in a single `Edit` call; requires explicit user confirmation.
- `/update-slnx` — syncs the `/.claude/*` virtual folders in `linq2db.slnx` with on-disk `.claude/` contents; always-included entries: `CLAUDE.md` and `.claude/settings.local.json`.
- `/audit-claude` — static audit of the `.claude/` corpus: seven check categories (dead reference, slnx mismatch, template gap, duplicated rule, retired path, terminology drift, refactor candidate); per-finding mechanical/creative/manual-only patches.
- `/session-reflect` — harvests current conversation for durable knowledge; routes findings to `.claude/` (project-scoped) or user-level auto-memory; six buckets: feedback, doc, script, skill, agent, permission.

### Agents (`.claude/agents/*.md`)

Each agent definition carries `name`, `description`, `tools`, and `model` frontmatter. The `model` field governs which Claude model is used for that agent's invocations.

**KB indexers** (all read-only, emit fenced output per `kb-protocol.md`):
- `kb-architect` — `tools: Read, Grep, Glob, Bash`; `model: sonnet`. Modes: `architecture-overview`, `architecture-per-area`, `conventions`, `area-rollup`, `glossary`, `delta`, `coverage-fill`. Never writes to disk directly.
- `kb-historian` — `tools: Read, Grep, Glob, Bash`; `model: sonnet`. Produces `history/by-year/*.md` and `history/decisions/*.md` from commit data pre-fetched by `kb-fetch-commits.ps1`.
- `kb-github-curator` — `tools: Read, Grep, Glob, Bash`; `model: haiku`. Builds/maintains GH indexes, mirrors wiki, extracts per-area themes. Data pre-fetched by `kb-fetch-github.ps1` / `kb-fetch-wiki.ps1`; agent never calls `gh` directly.
- `kb-issue-detector` — `tools: Read, Grep, Glob, Bash`; `model: sonnet`. Scans codebase for tech debt, anti-patterns, dead code per `kb-issue-categories.md` pattern catalog. Emits `detected-issues/items/DI-NNNN.md` + `INDEX-PATCH` operations.
- `kb-research` — `tools: Read, Grep, Glob, Bash`; `model: sonnet`. Read-only KB query agent; answers a single question from KB content; used by `/kb-ask`, `/fix-issue`, `/review-pr`. Soft budget: 30 tool calls.

**Review subagents:**
- `code-reviewer` — `tools: Read, Grep, Glob, Bash, WebFetch`; `model: opus`. Deep PR diff review; nine-category rubric (correctness, thread safety, performance, SQL correctness, public API, test coverage, style, scope creep, 3rd-party claim verification). Call budget: 1 `diff-reader.ps1` + 1 `verify-lines.ps1` + 0–3 spot follow-ups. Returns structured JSON with `findings`, `api_changes`, `out_of_scope_observations`, `callLog`.
- `baselines-reviewer` — `tools: Read, Grep, Glob, Bash`; `model: sonnet`. Reviews SQL and metrics baseline changes via `baselines-diff.ps1`; classifies into `new_correct`/`new_suspect`/`changed_expected`/`changed_suspect`; provides `compressionFeedback[]`.

**Test agents:**
- `test-writer` — `tools: Read, Grep, Glob, Edit, Write, Bash`; `model: sonnet`. Writes one test at a time; fixture lookup rules (Issue→IssueTests.cs→feature fixture→disambiguate); handles `playgroundLink` flag for `Tests.Playground.csproj` linkage; returns structured JSON.
- `test-runner` — `tools: Read, Grep, Bash`; `model: haiku`. Executes `dotnet test`; reads `UserDataProviders.json` read-only; validates providers enabled before running; returns structured JSON with per-target pass/fail.

### Reference docs (`.claude/docs/*.md`)

| File | Purpose |
|---|---|
| `agent-rules.md` | Auto-imported operational ruleset (see Root instructions above) |
| `architecture.md` | Core query pipeline, directory layout, companion projects |
| `code-design.md` | Design invariants: public-API contract, cross-cutting internals, SQL AST namespace placement, column-aligned formatting |
| `testing.md` | Test runner, config, framework patterns, "read the full log" rule, database initialization |
| `ci-tests.md` | Azure Pipelines CI trigger syntax (`/azp run test-all`, narrow per-DB triggers) |
| `claude-setup.md` | `.claude/` layout, settings precedence, skill discovery, when to run which skill |
| `test-databases.md` | Provider → setup script → container → image catalog; heavy providers (DB2/Informix/SAP HANA/SAP ASE) cost notes |
| `kb-architecture.md` | KB schema: directory layout, frontmatter contract, coverage block format, state files |
| `kb-areas.md` | Area registry: path patterns, Tier-1 pins, Tier-2 patterns per area |
| `kb-build-steps.md` | All 14 KB build steps (0–13), gates, inputs, owners, ordering invariants |
| `kb-coverage-tiers.md` | Tier 1/2/3 definitions, gate thresholds per step kind |
| `kb-issue-categories.md` | Detected-issues schema, severity/category/source/status definitions, pattern catalog |
| `kb-refresh-cursors.md` | Cursor format per source, delta-fetch rules, random-sample citation audit, deferred-coverage queue |
| `kb-selection-grammar.md` | Filter/action grammar for `/kb-issues`: exact IDs, group shorthand, random sampling, action menu |
| `review-orchestration.md` | Shared skeleton for `/review-pr` and `/verify-review`: PR resolution, context loading, parallel subagent spawning, API classification |
| `review-conventions.md` | Severity IDs (BLK/MAJ/MIN/SUG/NIT), finding ID format, ID-continuation floor |
| `review-posting.md` | `post-pr-review.ps1` manifest format, invocation, manifest-to-finding mapping |
| `pr-context-prep.md` | One-call PR context loader (`pr-context.ps1`), change summary, baselines clone setup, `writeDir` directory layout |
| `pr-resolver.md` | PR reference resolution: URL / number / issue / branch → PR number |
| `github-review-api.md` | GitHub PR review REST/GraphQL API cheat sheet for review skills |
| `api-surface-classification.md` | Milestone-driven public-API classification decision tree |
| `baselines-repo-layout.md` | `linq2db.baselines` branch naming, file grammar, expected cross-provider syntactic variations |
| `issue-search.md` | Issue/PR search discipline: term extraction, parallel strategies, dedup/rank |

### Helper scripts (`.claude/scripts/*.ps1`)

All scripts follow the manifest-in / JSON-out contract: read one JSON from stdin, emit one JSON to stdout, errors to stderr. Common helpers in `_shared.ps1` are dot-sourced.

**Core utility:**
- `_shared.ps1` — shared helpers: `Exit-WithError`, `Invoke-Process`, `Invoke-Gh`, `Invoke-GhJson`, `Invoke-Git`, `Test-IsInteger`, `Read-StdinJson`, `Write-JsonOutput`, `ConvertFrom-UnifiedDiffHunks`, `Split-DiffByFile`, `Find-StyleIssues`, `Get-DiffFingerprint`, `Test-RangeInHunks`. Configures UTF-8 stdio for `gh`/`git` via `Invoke-Gh` (solves the Windows console code-page encoding problem).

**KB scripts:**
- `kb-state.ps1` — KB state manager: `init`, `get-progress`, `set-step`, `apply-fences` (validates frontmatter + coverage blocks + paths, writes artifacts, merges INDEX-PATCHes), `summary`, `get-deferred`, `set-deferred-area`, `append-audit`. The apply-fences gate is the single validation choke-point for all KB indexer output.
- `kb-fetch-github.ps1` — paginated fetch of issues / PRs / discussions / milestones from `linq2db/linq2db` with cursor support; returns pre-parsed JSON for `kb-github-curator`.
- `kb-fetch-commits.ps1` — fetches git log with per-commit metadata (SHA, author, date, subject, body, files changed/insertions/deletions) for `kb-historian`.
- `kb-fetch-wiki.ps1` — clones or updates `linq2db.wiki` repo under `.build/.claude/kb-wiki`; reports changed articles since cursor SHA.
- `kb-audit-citations.ps1` — random-samples K KB files; verifies `Source/...:NNN` citations still exist at ±3 lines; returns `verdict: hit|miss|deleted` per citation.
- `kb-search.ps1` — grep across `.claude/knowledge-base/` with structured JSON output; used by `kb-research` for batched KB searches.
- `kb-coverage-backfill.ps1` — one-shot script to seed `state/deferred-coverage.json` for areas whose `INDEX.md` predates the deferred-coverage fence mechanism.

**PR review scripts:**
- `pr-context.ps1` — one-shot PR context loader: fetches PR metadata, reviews, review comments, issue comments, closing-issues GraphQL, diff stat/name-status/commits, head ref into `origin/pr/<n>`. Bundles ~10 `gh api`/`git` calls into one permission rule.
- `diff-reader.ps1` — batch file content + unified diff + hunk reader; `writeDir` writes HEAD/base/diff bodies to `.build/.claude/pr<n>/`; `include.styleScan` returns trailing-whitespace / 3+-blank-line / mixed-indent findings scoped to PR hunks.
- `verify-lines.ps1` — batch snippet + hunk verifier for code-reviewer findings; prevents wrong-line comments from landing on GitHub.
- `post-pr-review.ps1` — posts a PENDING draft PR review: REST bulk-create + GraphQL fan-out for file-level and reply comments. Solves the multi-endpoint problem (line/file/reply comments require different GitHub APIs) in one allowlisted call.
- `apply-verify-writes.ps1` — applies the in-place edit batch from `/verify-review`: comment PATCHes (append "fixed" annotation), review body PUTs (checkbox flips), thread resolves via GraphQL. Fan-out parallelism via `Start-ThreadJob`.
- `baselines-diff.ps1` — one-shot baselines diff reader for `baselines-reviewer`: parses changed paths from `../linq2db.baselines`, applies file grammar (`provider/test/params`), normalizes diff bodies for `changePatterns[]` compression, pre-builds `testGroups`.
- `pr-body-edit.ps1` — manifest-driven ASCII-anchor-based PR body insertion; solves UTF-8 encoding-safety for non-ASCII PR body content.

**Test scripts:**
- `snap-baselines.ps1` — pre-run snapshot of `BaselinesPath` file hashes for `/test` baselines diff.
- `diff-baselines.ps1` — post-run diff against pre-run snapshot; surfaces added/changed/removed baseline files.

### Hooks (`.claude/hooks/`)

Both hooks are PowerShell scripts wired in the user's `.claude/settings.local.json` (gitignored). No hooks are committed to the repo.

| Hook | Event type | What it does |
|---|---|---|
| `track-docker-start.ps1` | `PostToolUse` (Bash) | Parses `docker start <name>` commands from the Bash tool's `tool_input.command`; appends container names to `.build/.claude/docker-session-started.txt` (deduplicated). Consumers: `cleanup-docker-session.ps1` (SessionEnd) and the agent's scope-change guard rule in `agent-rules.md`. |
| `cleanup-docker-session.ps1` | `SessionEnd` | Reads `.build/.claude/docker-session-started.txt`; runs `docker stop <all names>` in one call; removes the state file. Silent no-op when file is missing. |

A user-level hook `~/.claude-my/hooks/check-bash-chain.js` (not in this repo) enforces the no-compound-Bash rule (`&&`/`;`/control-flow) as a `PreToolUse` gate. This hook is referenced in `CLAUDE.md` system context but lives outside the CLAUDE-INFRA area.

## Files (Tier 1 / Tier 2)

### Tier 1 (28 files)

| File | Role |
|---|---|
| `CLAUDE.md` | Root project instructions |
| `.claude/agents/kb-architect.md` | KB indexer agent |
| `.claude/agents/kb-historian.md` | KB history indexer agent |
| `.claude/agents/kb-github-curator.md` | KB GitHub data agent |
| `.claude/agents/kb-issue-detector.md` | KB debt detector agent |
| `.claude/agents/kb-research.md` | KB query agent |
| `.claude/agents/code-reviewer.md` | PR code review agent |
| `.claude/agents/baselines-reviewer.md` | Baselines review agent |
| `.claude/agents/test-writer.md` | Test authoring agent |
| `.claude/agents/test-runner.md` | Test execution agent |
| `.claude/skills/kb-build/SKILL.md` | KB build orchestrator |
| `.claude/skills/kb-refresh/SKILL.md` | KB refresh orchestrator |
| `.claude/skills/kb-ask/SKILL.md` | KB Q&A skill |
| `.claude/skills/kb-issues/SKILL.md` | KB detected-issues triage |
| `.claude/skills/kb-status/SKILL.md` | KB status dashboard |
| `.claude/skills/review-pr/SKILL.md` | PR review skill |
| `.claude/skills/verify-review/SKILL.md` | Review verification skill |
| `.claude/skills/fix-issue/SKILL.md` | Issue fix orchestrator |
| `.claude/skills/create-issue/SKILL.md` | Issue creation skill |
| `.claude/skills/find-issues/SKILL.md` | Issue search skill |
| `.claude/skills/merge-duplicates/SKILL.md` | Duplicate issue consolidation |
| `.claude/skills/test/SKILL.md` | Test write+run orchestrator |
| `.claude/skills/test-providers/SKILL.md` | Test env management |
| `.claude/skills/api-baselines/SKILL.md` | API baseline refresh |
| `.claude/skills/version-bump/SKILL.md` | Version bump |
| `.claude/skills/update-slnx/SKILL.md` | Solution file sync |
| `.claude/skills/audit-claude/SKILL.md` | Instruction corpus audit |
| `.claude/skills/session-reflect/SKILL.md` | Session harvest skill |

### Tier 2 (42 files)

| File | Role |
|---|---|
| `.claude/agents/_shared/kb-protocol.md` | Fenced output protocol for KB indexers |
| `.claude/docs/agent-rules.md` | Auto-imported operational ruleset |
| `.claude/docs/architecture.md` | Core query pipeline reference |
| `.claude/docs/code-design.md` | Design invariants reference |
| `.claude/docs/testing.md` | Test conventions reference |
| `.claude/docs/ci-tests.md` | CI trigger reference |
| `.claude/docs/claude-setup.md` | Claude Code setup reference |
| `.claude/docs/test-databases.md` | Provider catalog |
| `.claude/docs/kb-architecture.md` | KB schema reference |
| `.claude/docs/kb-areas.md` | Area registry |
| `.claude/docs/kb-build-steps.md` | Build step definitions |
| `.claude/docs/kb-coverage-tiers.md` | Coverage tier rules |
| `.claude/docs/kb-issue-categories.md` | Detected-issue taxonomy |
| `.claude/docs/kb-refresh-cursors.md` | Cursor format + citation audit |
| `.claude/docs/kb-selection-grammar.md` | Filter/action grammar |
| `.claude/docs/review-orchestration.md` | Shared review skeleton |
| `.claude/docs/review-conventions.md` | Severity IDs, finding format |
| `.claude/docs/review-posting.md` | Review posting mechanics |
| `.claude/docs/pr-context-prep.md` | PR context prep reference |
| `.claude/docs/pr-resolver.md` | PR reference resolver |
| `.claude/docs/github-review-api.md` | GitHub review API cheat sheet |
| `.claude/docs/api-surface-classification.md` | API classification decision tree |
| `.claude/docs/baselines-repo-layout.md` | Baselines repo layout |
| `.claude/docs/issue-search.md` | Issue search discipline |
| `.claude/scripts/_shared.ps1` | Common helpers for all scripts |
| `.claude/scripts/kb-state.ps1` | KB state manager + apply-fences |
| `.claude/scripts/kb-fetch-github.ps1` | GitHub data fetcher |
| `.claude/scripts/kb-fetch-commits.ps1` | Git commit fetcher |
| `.claude/scripts/kb-fetch-wiki.ps1` | Wiki clone/update fetcher |
| `.claude/scripts/kb-audit-citations.ps1` | KB citation verifier |
| `.claude/scripts/kb-search.ps1` | KB grep utility |
| `.claude/scripts/kb-coverage-backfill.ps1` | Deferred-coverage queue seeder |
| `.claude/scripts/pr-context.ps1` | PR context loader |
| `.claude/scripts/diff-reader.ps1` | Diff + content reader |
| `.claude/scripts/verify-lines.ps1` | Line-number verifier |
| `.claude/scripts/post-pr-review.ps1` | Review POST wrapper |
| `.claude/scripts/apply-verify-writes.ps1` | Verify in-place edit writer |
| `.claude/scripts/baselines-diff.ps1` | Baselines diff reader |
| `.claude/scripts/pr-body-edit.ps1` | PR body encoding-safe editor |
| `.claude/scripts/snap-baselines.ps1` | Pre-run baseline snapshot |
| `.claude/scripts/diff-baselines.ps1` | Post-run baseline diff |
| `.claude/hooks/track-docker-start.ps1` | Docker session tracking hook |
| `.claude/hooks/cleanup-docker-session.ps1` | Docker session cleanup hook |

## Inbound / outbound dependencies

**Inbound** — this area has no code callers; it is loaded by Claude Code at session start (`CLAUDE.md` → Claude Code session) and referenced by every agent and skill in this area.

**Outbound** — the corpus instructs agents to operate on essentially every other area:
- `.claude/docs/architecture.md`, `code-design.md` → describe the `CORE`, `EXPR-TRANS`, `SQL-AST`, `SQL-PROVIDER`, `MAPPING`, `LINQ`, `DATA`, `INFRA` areas.
- `.claude/docs/testing.md`, `test-databases.md` → describe the `TESTS-*` areas.
- Skills spawn agents that read `Source/`, `Tests/`, `Build/` (every area in the repo).
- `kb-state.ps1` writes to `.claude/knowledge-base/` (KB output area).
- Hooks observe `.build/.claude/` (gitignored scratch space).

## Known issues / debt

1. **`BannedSymbols.txt` path mismatch.** `CLAUDE.md` states the banned API list is at `Build/BannedSymbols.txt`. The actual file is at `Source/BannedSymbols.txt` (and `Tests/BannedSymbols.txt`). This also affects the `BUILD` area row in `kb-areas.md` which pins `BannedSymbols.txt` as a Tier-1 file — the path pattern `Build/**` will not match `Source/BannedSymbols.txt`.

2. **`claude-setup.md` is stale.** The file's "Current skills" list omits all skills added since it was last updated (`/fix-issue`, `/test`, `/test-providers`, `/kb-*` family, `/create-issue`, `/find-issues`, `/merge-duplicates`, `/session-reflect`, `/audit-claude`). The doc functions as a quick-reference so the gap is informational rather than operational — agents read individual SKILL.md files — but a future `/audit-claude` run will flag this as a retired-content issue.

3. **User-level hook not in this corpus.** The `check-bash-chain.js` PreToolUse hook that enforces the no-compound-Bash rule is referenced in CLAUDE.md's system context but lives at the user level (`~/.claude-my/hooks/`), not under `.claude/hooks/`. Any new team member must install it manually; it is not discoverable from this corpus.

4. **`audit-claude` refactor-candidate threshold.** Several SKILL.md files exceed 250 lines: `review-pr/SKILL.md`, `verify-review/SKILL.md`, `test-providers/SKILL.md`, `fix-issue/SKILL.md`. Much of the shared procedure is already factored into `.claude/docs/review-orchestration.md` and `pr-context-prep.md`; the remaining bulk in `test-providers/SKILL.md` has no shared-doc counterpart yet.

5. **`settings.local.json` not committed.** Hooks are wired via `settings.local.json` (gitignored). The hook scripts themselves (`track-docker-start.ps1`, `cleanup-docker-session.ps1`) are committed but their wiring is not, so new contributors see no hooks until they configure `settings.local.json` themselves. `claude-setup.md` acknowledges this by design.

## See also

- `architecture/overview.md` — codebase architecture that CLAUDE-INFRA instructs agents to follow
- `.claude/knowledge-base/README.md` — what the KB output (driven by this area) contains
- `.claude/docs/kb-architecture.md` — KB schema that `kb-architect` agent produces
- `.claude/docs/kb-areas.md` — area registry (source of truth for all area definitions including this one)

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 28 / 28 ✓
  - CLAUDE.md
  - .claude/agents/kb-architect.md
  - .claude/agents/kb-historian.md
  - .claude/agents/kb-github-curator.md
  - .claude/agents/kb-issue-detector.md
  - .claude/agents/kb-research.md
  - .claude/agents/code-reviewer.md
  - .claude/agents/baselines-reviewer.md
  - .claude/agents/test-writer.md
  - .claude/agents/test-runner.md
  - .claude/skills/kb-build/SKILL.md
  - .claude/skills/kb-refresh/SKILL.md
  - .claude/skills/kb-ask/SKILL.md
  - .claude/skills/kb-issues/SKILL.md
  - .claude/skills/kb-status/SKILL.md
  - .claude/skills/review-pr/SKILL.md
  - .claude/skills/verify-review/SKILL.md
  - .claude/skills/fix-issue/SKILL.md
  - .claude/skills/create-issue/SKILL.md
  - .claude/skills/find-issues/SKILL.md
  - .claude/skills/merge-duplicates/SKILL.md
  - .claude/skills/test/SKILL.md
  - .claude/skills/test-providers/SKILL.md
  - .claude/skills/api-baselines/SKILL.md
  - .claude/skills/version-bump/SKILL.md
  - .claude/skills/update-slnx/SKILL.md
  - .claude/skills/audit-claude/SKILL.md
  - .claude/skills/session-reflect/SKILL.md
- Tier 2 (visited / total): 42 / 42 (100%) ✓
  - .claude/agents/_shared/kb-protocol.md
  - .claude/docs/agent-rules.md
  - .claude/docs/architecture.md
  - .claude/docs/code-design.md
  - .claude/docs/testing.md
  - .claude/docs/ci-tests.md
  - .claude/docs/claude-setup.md
  - .claude/docs/test-databases.md
  - .claude/docs/kb-architecture.md
  - .claude/docs/kb-areas.md
  - .claude/docs/kb-build-steps.md
  - .claude/docs/kb-coverage-tiers.md
  - .claude/docs/kb-issue-categories.md
  - .claude/docs/kb-refresh-cursors.md
  - .claude/docs/kb-selection-grammar.md
  - .claude/docs/review-orchestration.md
  - .claude/docs/review-conventions.md
  - .claude/docs/review-posting.md
  - .claude/docs/pr-context-prep.md
  - .claude/docs/pr-resolver.md
  - .claude/docs/github-review-api.md
  - .claude/docs/api-surface-classification.md
  - .claude/docs/baselines-repo-layout.md
  - .claude/docs/issue-search.md
  - .claude/scripts/_shared.ps1
  - .claude/scripts/kb-state.ps1
  - .claude/scripts/kb-fetch-github.ps1
  - .claude/scripts/kb-fetch-commits.ps1
  - .claude/scripts/kb-fetch-wiki.ps1
  - .claude/scripts/kb-audit-citations.ps1
  - .claude/scripts/kb-search.ps1
  - .claude/scripts/kb-coverage-backfill.ps1
  - .claude/scripts/pr-context.ps1
  - .claude/scripts/diff-reader.ps1
  - .claude/scripts/verify-lines.ps1
  - .claude/scripts/post-pr-review.ps1
  - .claude/scripts/apply-verify-writes.ps1
  - .claude/scripts/baselines-diff.ps1
  - .claude/scripts/pr-body-edit.ps1
  - .claude/scripts/snap-baselines.ps1
  - .claude/scripts/diff-baselines.ps1
  - .claude/hooks/track-docker-start.ps1
  - .claude/hooks/cleanup-docker-session.ps1
- Tier 3 (skipped, logged): 0
</details>
