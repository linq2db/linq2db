---
name: kb-build
description: Build the persistent knowledge base under .claude/knowledge-base/ from scratch. Phased and resumable — each step is sized to a single session and gates on coverage. Re-running picks up at the first incomplete step. Spawns kb-architect / kb-historian / kb-github-curator / kb-issue-detector as needed; never invents content directly.
---

# /kb-build

User-triggered orchestrator that produces the linq2db knowledge base.

## What this skill does

Walks the build steps defined in [`../../docs/kb-build-steps.md`](../../docs/kb-build-steps.md), one at a time:

1. Reads `state/build-progress.json` to find the first non-`done` step.
2. Re-enters `partial` steps before advancing.
3. Spawns the step's owner agent with the right inputs.
4. Validates the agent's fenced output via `kb-state.ps1 apply-fences`, applies all writes.
5. Runs the gate (Tier coverage + artifact existence + frontmatter validity).
6. On gate pass: marks step `done`, advances. On gate fail: marks `partial`, stops at this boundary.

The user can stop at any step boundary and resume later — `/kb-build` is idempotent.

## Shared reference material

- [`../../docs/kb-architecture.md`](../../docs/kb-architecture.md) — KB schema, frontmatter, indexes
- [`../../docs/kb-build-steps.md`](../../docs/kb-build-steps.md) — full step list, gates, inputs (this is the source of truth — do not duplicate here)
- [`../../docs/kb-areas.md`](../../docs/kb-areas.md) — area registry
- [`../../docs/kb-coverage-tiers.md`](../../docs/kb-coverage-tiers.md) — coverage rules
- [`../../docs/kb-issue-categories.md`](../../docs/kb-issue-categories.md) — detected-issue taxonomy (used by step 10)
- [`../../docs/kb-refresh-cursors.md`](../../docs/kb-refresh-cursors.md) — cursor formats
- [`../../agents/_shared/kb-protocol.md`](../../agents/_shared/kb-protocol.md) — fenced output contract

## When to run

Only when the user explicitly invokes `/kb-build`. Typical prompts:
- `/kb-build` — start or resume from the first incomplete step.
- `/kb-build --force <step-name>` — re-run a `done` step from scratch.
- `/kb-build --area <code>` — when the current step is area-scoped (3, 8, 10, 11), restrict to one area.
- `/kb-build --since <YYYY-MM-DD>` — for steps 5/6/7, override cursor to a specific date (initial sliced runs).

Subsequent updates after the build is complete go through `/kb-refresh`.

## Steps

### 1. Initialize state

Run `kb-state.ps1 init`. Idempotent: creates skeleton dirs, `state/build-progress.json`, `state/cursors.json`, `state/audit-log.md` if missing; otherwise no-op.

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "init"}
EOF
```

### 2. Determine the next step

Read current progress:

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "get-progress"}
EOF
```

Find the **first step where status ≠ `done`** (in id order). That's the target. If `--force <name>` was passed, target that step and treat its status as `pending`. If all steps are `done` and no `--force`, print a one-line "KB build is complete; run /kb-refresh for incremental updates" and exit.

### 3. Capture currentSha

```bash
git rev-parse HEAD
```

Cache the SHA — you'll pass it to every agent invocation as `currentSha`, and to `apply-fences` for `last_verified_sha` validation.

### 4. Mark step in-progress

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "set-step", "step": "<name>", "status": "in-progress"}
EOF
```

### 5. Run the step

Step-specific procedures. Each step's gate rules and inputs are in [`../../docs/kb-build-steps.md`](../../docs/kb-build-steps.md) — consult it before running.

#### Step 0 — bootstrap (skill-owned)

The skill itself creates the KB skeleton:

1. Create dirs: `.claude/knowledge-base/{architecture,conventions,history/by-year,history/decisions,github/wiki,detected-issues/items,areas}`. Use `Bash` `mkdir -p`.
2. Write `.claude/knowledge-base/README.md` (1-page overview — see template below).
3. Write `.claude/knowledge-base/glossary.md` stub:
   ```
   ---
   area: GLOBAL
   kind: glossary
   sources: []
   confidence: low
   last_verified: <today>
   last_verified_sha: <currentSha>
   ---

   # Glossary

   _(populated by /kb-build step 12)_
   ```
4. Mark step `done` and advance.

#### Step 1 — area-registry (skill + user)

1. Glob top-level dirs under `Source/`, `Tests/`, `Build/`, `.github/`, `.claude/`.
2. Read `.claude/docs/kb-areas.md` (its bootstrap proposal table).
3. Diff: dirs not covered by any area → list them.
4. Print the bootstrap proposal table to the user; ask them to confirm or edit.
5. **Wait for user response** (`y` to accept, free-text edits to apply, or "edit yourself" to drop into `Edit` on `kb-areas.md`).
6. After user confirms, the file is the source of truth — mark step `done`.

The bootstrap proposal in `kb-areas.md` is intentionally already filled in; for typical first runs, the user confirms with `y` and step 1 completes in one turn.

#### Steps 2, 3, 4, 11, 12 — kb-architect

Spawn the `kb-architect` agent via the `Agent` tool with `subagent_type: "kb-architect"`.

Per-step inputs:

| Step | mode | extra |
|---|---|---|
| 2 | `architecture-overview` | `pinnedFiles` from `kb-areas.md` Tier-1 cross-area + canonical pipeline files |
| 3 | `architecture-per-area` | one invocation per area; pass `area` |
| 4 | `conventions` | (no area) |
| 11 | `area-rollup` | one per area; pass `area` |
| 12 | `glossary` | (no area) |

Capture the agent's stdout, write to `.build/.claude/kb-build-step<n>.txt`, then:

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "apply-fences", "agentOutputFile": ".build/.claude/kb-build-step<n>.txt", "currentSha": "<sha>"}
EOF
```

Inspect the result. If `gateFailures` is empty, mark step `done`. Otherwise mark `partial`, append failures to `audit-log.md`, surface to user, and stop.

For step 3 (per-area) and step 11 (per-area roll-up), checkpoint *after each area*: spawn agent, apply fences, mark intermediate progress in `audit-log.md`. If interrupted mid-step, the next run reads which areas already have artifacts on disk and skips them (the agent receives `existingArtifacts` to support this).

#### Steps 5, 6 — kb-historian

For step 5 (history-by-year):

1. Determine year range: `git log --reverse --format=%aI -n 1` for first commit, current year for last.
2. For each year, fetch commits via `kb-fetch-commits.ps1` with `year: <YYYY>`. Pass the result to `kb-historian` with `mode: "history-by-year"`, `yearRange: [year, year]`.
3. Apply fences. Checkpoint after each year.

For step 6 (history-decisions):

1. Fetch full history: `kb-fetch-commits.ps1` with `since: null`.
2. Spawn `kb-historian` with `mode: "history-decisions"` and the full commit list.
3. Apply fences.

Update `cursors.json.commits` after each year's batch is applied.

#### Steps 7, 8, 9 — kb-github-curator

For step 7 (github-indexes):

For each source in `[issues, prs, discussions, milestones]`:
1. `kb-fetch-github.ps1` with `source: <x>`, cursor from `cursors.json` (or null on first run).
2. Spawn `kb-github-curator` with `mode: "github-indexes"`, `source: <x>`, the fetched JSON.
3. Apply fences.
4. Update `cursors.json.<source>` with `next_cursor` from the fetch result.

If the fetch script returns `status: "rate-limited"`, append to `audit-log.md`, mark step `partial`, exit.

For step 8 (github-themes):

For each area in `kb-areas.md`:
1. Spawn `kb-github-curator` with `mode: "github-themes"`, `area: <code>`, plus the index files already on disk.
2. Apply fences.

For step 9 (wiki-mirror):

1. `kb-fetch-wiki.ps1` with `since: null` on first run, otherwise current cursor.
2. Spawn `kb-github-curator` with `mode: "wiki-mirror"` and the changed-file list.
3. Apply fences.
4. Update `cursors.json.wiki.sha`.

#### Step 10 — kb-issue-detector

For each area in `kb-areas.md`:
1. Spawn `kb-issue-detector` with `mode: "area-scan"`, `area: <code>`, plus `existingIndex` (current `detected-issues/index.json`) and `openGithubIssues` (filter from `github/issues-index.json`).
2. Apply fences.
3. Checkpoint per area in `audit-log.md`.

After all areas: aggregate the COVERAGE-SUMMARY blocks; mark step `done` only if every Tier-1 + Tier-2 file in every area was reported as scanned.

#### Step 13 — validation (skill-owned)

1. Read `state/build-progress.json` — verify every step is `done`.
2. Spawn `kb-research` with a sample question per area (10 random KB files, ask "summarize the main claims of this file in 2 sentences"). Aggregate confidence.
3. Append a final `## kb-build complete` entry to `audit-log.md`.
4. Print final `/kb-status` summary.

### 6. Mark step done or partial

After `apply-fences`, if `gateFailures` is empty:

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "set-step", "step": "<name>", "status": "done"}
EOF
```

Otherwise:

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "set-step", "step": "<name>", "status": "partial", "gate_failures": [<failure messages>]}
EOF
```

### 7. Loop or stop

After each step:

- If `done` and there are more steps, loop back to step 2 (find next).
- If `partial`, surface the failures to the user, suggest a fix path, and stop. Do not advance past a `partial` step.
- If the user has been asked a question (step 1 area-registry), stop and wait for the answer; resume after it lands.

## README template (step 0)

```
# linq2db Knowledge Base

This directory is generated by `/kb-build` and updated by `/kb-refresh`. It captures
linq2db's architecture, conventions, history, and detected tech debt in a form agents
can consume cheaply.

## Reading order

1. `glossary.md` — domain terminology
2. `architecture/overview.md` — repo map and query pipeline
3. `areas/<area>/INDEX.md` — drill into a specific subsystem
4. `detected-issues/index.json` — known tech debt (browse via `/kb-issues`)

## Refreshing

- `/kb-refresh` — pull deltas (commits, GH items, wiki) since last cursor.
- `/kb-status` — show what's on disk and how fresh it is.
- `/kb-issues` — query / triage detected issues.
- `/kb-ask <question>` — KB-grounded Q&A.

## Conventions

Every file under `architecture/`, `conventions/`, `history/decisions/`, and `areas/<area>/`
carries YAML frontmatter with `confidence`, `last_verified`, and `last_verified_sha` so
readers can see how stale a doc is.

Coverage blocks at the end of each file list which Tier-1 / Tier-2 files were visited
when the doc was generated. See `.claude/docs/kb-coverage-tiers.md`.
```

## Do not

- Run more than one step per turn unless the user opts in. Step boundaries are deliberate pause points.
- Bypass `kb-state.ps1 apply-fences` — agents emit fenced output, the skill does not write artifacts directly.
- Modify any file outside `.claude/knowledge-base/`, `.claude/docs/kb-areas.md` (step 1 only), or `.build/.claude/`.
- Re-fetch GitHub content the cursor says is fresh. Trust cursors.
- Force-promote a step to `done` when its gate failed; the user resolves the failure first.
