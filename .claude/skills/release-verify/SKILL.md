---
name: release-verify
description: Final verification phase of release prep. Runs **one** Release build (or `/profile-analyzers` if analyzer packages were bumped this release) to validate that the cumulative state from all earlier release-prep tasks (deps + PublicAPI + milestone-check + test-matrix + release-notes + ad-hoc) compiles cleanly. Reactively walks any new analyzer-rule errors with the user (fix or disable per rule). Refreshes API baselines via `/api-baselines`. Commits the consequent fixes (rule disables, baseline updates, code patches) as a single "release prep verification" commit on the prep PR. Invoked by `/release` step 6 after every other release-prep task is `[x]` or `[-]`.
---

# /release-verify

## What this skill is (and isn't)

**Is:** the single verification gate at the end of release prep. Owns the **only** Release build of the release-prep cycle (every prior sub-skill is forbidden from running its own verification build). Combines four things into one pass so the build+fix loop happens once, not per-task:

1. Release build of `linq2db.slnx` (or profile-analyzers, see step 1).
2. Reactive analyzer-rule catch-up — for any new analyzer error the build surfaces, ask user fix-or-disable per rule (the equivalent of step 4a in the new `/release-deps` for analyzer rules that escaped the predictive audit).
3. (Conditional) `/profile-analyzers` perf check if analyzer packages were bumped this release.
4. `/api-baselines` refresh — regenerate `CompatibilitySuppressions.xml` files.

All consequent edits (`.editorconfig` rule disables, `CompatibilitySuppressions.xml` regenerations, code fixes) batch into a single follow-up commit on the prep PR.

**Isn't:**

- Not a plain `dotnet build`. It's an orchestrated build → react → re-build → audit → baseline-refresh chain.
- Not a CI replacement. CI test-all still runs the full provider matrix on every commit.
- Not for ad-hoc verification mid-flow. It's gated to fire once, at orchestrator step 6, after every other prep task is closed.

## When to run

- Invoked by `/release` step 6 once tasks 0-5 + ad-hoc 6.x are all `[x]` or `[-]`.
- Manually only if the user explicitly asks "verify the release prep" or similar — and only when the prep branch is in a state to merge (otherwise it's premature).

## Required reading

- [`.claude/skills/api-baselines/SKILL.md`](../api-baselines/SKILL.md) — invoked as a sub-step. Read its policy on `LinqToDB.Internal.*` vs other API surface.
- [`.claude/skills/profile-analyzers/SKILL.md`](../profile-analyzers/SKILL.md) — invoked conditionally.
- [`.claude/docs/release/nuget-package-notes.md`](../../docs/release/nuget-package-notes.md) — analyzer-package rules (consulted for the reactive rule walk).
- [`.claude/docs/agent-rules.md`](../../docs/agent-rules.md) → **Git commit rules**, **Push to remote rules**.

## Procedure

### 1. Pick the verification build mode

Read the deps state (`release-state.ps1 -Action load`). If `state.deps.applied[]` includes any analyzer package — id matches `*Analyzer`, `*Analyzers`, `BannedApi*`, `PublicApi*`, `AsyncFixer`, `Lindhart.Analyser.*`, or sits in the `Build: Analyzers and Tools` `<ItemGroup>` of `Directory.Packages.props` — the verification build is `/profile-analyzers` (which is itself a Release rebuild with `-p:ReportAnalyzer=true -v:detailed`, so it covers both the build verification and the analyzer perf check).

Otherwise, the verification build is plain `dotnet build linq2db.slnx -c Release`.

Tell the user which mode and the wall-clock estimate (10-25 min for profile-analyzers; 3-8 min for plain). Wait for explicit go-ahead — never auto-launch a build inside `/release-verify`.

### 2. Run the build (loop until clean)

Run the chosen build. If it fails, classify the errors:

#### 2a. Analyzer-rule errors (`error MAxxxx`, `error CAxxxx`, `error NUnitxxxx`, etc.)

Group by rule code. For each distinct rule, ask the user: **fix the errors**, **disable the rule** (set `dotnet_diagnostic.<id>.severity = none` in `.editorconfig`), or **set as suggestion** (no build break, surface in IDE).

When disabling, queue a `.editorconfig` insert in numerical position under the appropriate analyzer family section (Meziantou under `###### Meziantou.Analyzers`, etc.) with a short reasoning suffix.

When fixing, walk the user through the affected sites. The fix may itself require multiple iterations.

Apply the queued `.editorconfig` edits + any code fixes in one batch, then **re-run the build**. Loop until clean.

This step is the reactive backstop for analyzer rules that escaped the predictive audit in `/release-deps` step 4a (e.g. rules that fire on patterns the changelog didn't enumerate).

#### 2b. Non-analyzer compile errors (`error CSxxxx`)

Common surprises:
- Direct API deprecations on test-helper types from analyzer packages (e.g. NUnit's `TestDelegate` obsoletion in 4.6 — caught only at compile time).
- Cross-package compile interactions (dropped overloads, removed extensions).
- Source-tree edits from a prior release-prep task (PublicAPI, test-matrix) that didn't compile-check.

Walk the user through each. Apply minimal fixes. Re-run the build. Loop until clean.

#### 2c. Other failures (NuGet restore, file lock, disk full)

Stop and surface to user. Do not improvise — disk-full / locked-file failures need the user's environment intervention.

### 3. (Conditional) Profile analyzers — if mode = profile-analyzers

If step 1 chose profile-analyzers, the build run already produced the analyzer-perf log. Parse it:

```
pwsh -NoProfile -File .claude/scripts/analyzer-profile-report.ps1 -LogPath <log-path-from-step-2> -Top 10
```

Show the three rankings to the user. They decide whether to disable any newly-expensive rules (queue another `.editorconfig` edit) or accept the perf hit. Per `/profile-analyzers` Don'ts, only propose disabling rules that are both expensive AND not pulling their weight.

If the user disables anything, re-run the build verification (step 2) once more to confirm clean.

### 4. Refresh API baselines

Invoke `Skill('api-baselines')`. That skill:

- Deletes existing `CompatibilitySuppressions.xml` files under `Source/`.
- Re-runs `dotnet pack -p:ApiCompatGenerateSuppressionFile=true` to regenerate them.
- Reviews the diff and flags any non-`LinqToDB.Internal.*` API changes for explicit user approval.

The user reviews the regenerated baselines per `/api-baselines` policy. Any approved changes accumulate on the prep branch's working tree.

### 5. Consolidated release-prep commit

This is the **single** commit of the release-prep cycle. Earlier tasks (deps / PublicAPI / milestone-check / test-matrix / release-notes / ad-hoc) leave their changes uncommitted on the worktree; this step stages all of them plus anything produced in steps 2-4 of this skill, and commits them together.

Stage all changes (audit `Tests/Tests.Playground/` for accidental scratch — see `agent-rules.md` → **Git commit rules** — and `.claude/` for accidental curation diffs that must NOT land on `release-prep/<ver>`). Commit message structure:

```
Release <ver>: prep

Dependencies (per /release-deps):
- <Package> <old> -> <new>
- ...
- Skipped: <ids or "none">; Blocked by policy: <ids or "none">

PublicAPI reconciliation (per /release-publicapi):
- Promoted Unshipped -> Shipped across <N> projects.

Test-matrix follow-ups (per /release-test-matrix):
- <list of issues caught + fixes, or "all green">

Release-notes (per /release-notes-validate):
- <gaps closed + intentional omissions, or "complete coverage">

Ad-hoc tasks: <list>

Final verification (per /release-verify):
- analyzer rules disabled: <list>
- analyzer rules enabled (post-bump): <list>
- code fixes for new errors: <list>
- API baselines regenerated: <count> files
- profile-analyzers report: <link to .build/.claude/...>  (only if step 3 ran)
- iSeries drift check: <verdict, link to .build/.claude/...>
```

Per `agent-rules.md` → **Git commit rules**, only commit on explicit user request. The user reviews the full message + the proposed staged file list before the commit lands.

If multiple thematic commits are preferred (e.g. user wants deps separate from verification fixes), split into:
- `Release <ver>: dependency updates` — only the deps changes.
- `Release <ver>: prep verification` — the .editorconfig / baseline / code fixes from steps 2-4 of this skill.

Default is one consolidated commit; ask the user if they want a split.

### 6. Push + open PR + trigger CI

On explicit user confirmation:
1. Push the prep branch (creating it on the remote on first push).
2. If no prep PR exists yet, open one per [`branch-and-pr.md`](../../docs/release/branch-and-pr.md) (`Release prep <ver>`, draft, milestone, `--assignee @me`, body = checklist auto-synced from `release-state.ps1`).
3. Trigger `/azp run test-all` via `.claude/scripts/azp-run.ps1`. This is the first and only CI trigger for the release-prep cycle.

### 7. Hand back to `/release` orchestrator

The orchestrator marks task 6 `[x]` and proceeds to step 5 (prep-merge gate). The prep PR is now in CI's hands.

## Don'ts

- Do **not** run multiple verification builds. The whole point of this skill is one build per release prep — if step 2 needs to re-build after fixes, that's expected; if step 3 (profile-analyzers) needed a re-build after disables, that's expected; otherwise no extra builds.
- Do **not** disable analyzer rules without explicit user approval per rule.
- Do **not** push without explicit user request (per `agent-rules.md` → **Push to remote rules**).
- Do **not** commit `TreatWarningsAsErrors=false` or any other temporary unblock flag.
- Do **not** dispatch `/release-verify` mid-prep just to "see if it works". Trust the predictive audits in earlier prep tasks; the orchestrator gates this skill to fire once at the end.

## Related

- [`/release`](../release/SKILL.md) — orchestrator, dispatches this skill at step 6.
- [`/release-deps`](../release-deps/SKILL.md) — deps phase. Step 4 (predictive audits) is meant to make `/release-verify`'s reactive walk in step 2a a no-op for the common case.
- [`/profile-analyzers`](../profile-analyzers/SKILL.md), [`/api-baselines`](../api-baselines/SKILL.md) — invoked as sub-steps.
